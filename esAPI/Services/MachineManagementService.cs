using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Clients;
using esAPI.Interfaces;

namespace esAPI.Services
{
    public interface IMachineManagementService
    {
        Task<bool> EnsureMachinesAvailableAsync();
    }

    public class MachineManagementService : IMachineManagementService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<MachineManagementService> _logger;

        public MachineManagementService(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            ICommercialBankClient bankClient,
            ILogger<MachineManagementService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _bankClient = bankClient;
            _logger = logger;
        }

        public async Task<bool> EnsureMachinesAvailableAsync()
        {
            try
            {
                int totalMachines = await _context.Machines.CountAsync(m => m.RemovedAt == null);
                int brokenMachines = await _context.Machines.CountAsync(m =>
                    m.MachineStatusId == (int)Models.Enums.Machine.Status.Broken && m.RemovedAt == null);

                // Order machines if we have zero or all are broken
                if (totalMachines == 0 || totalMachines == brokenMachines)
                {
                    _logger.LogInformation($"[Machine] No working machines available. Total: {totalMachines}, Broken: {brokenMachines}. Attempting to buy 2 new machines from THOH.");
                    return await PurchaseMachinesFromThohAsync(2);
                }

                _logger.LogInformation($"[Machine] Machines status OK. Total: {totalMachines}, Broken: {brokenMachines}, Working: {totalMachines - brokenMachines}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Machine] Error checking machine status or purchasing new machines");
                return false;
            }
        }

        private async Task<bool> PurchaseMachinesFromThohAsync(int quantity)
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation($"[Machine] THOH machine order attempt {attempt}/{maxRetries}");
                    var thohHttpClient = _httpClientFactory.CreateClient("thoh");
                    var machineOrderReq = new { machineName = "electronics_machine", quantity };

                    var response = await thohHttpClient.PostAsJsonAsync("/machines", machineOrderReq);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"[Machine] Failed to order machines from THOH attempt {attempt}. Status: {response.StatusCode}");
                        if (attempt == maxRetries)
                            return false;
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(content);

                    var orderId = doc.RootElement.GetProperty("orderId").GetInt32();
                    var totalPrice = doc.RootElement.GetProperty("totalPrice").GetDecimal();
                    var bankAccount = doc.RootElement.GetProperty("bankAccount").GetString();

                    _logger.LogInformation($"[Machine] Ordered {quantity} new machines from THOH. OrderId={orderId}, TotalPrice={totalPrice}, BankAccount={bankAccount}");

                    if (!string.IsNullOrEmpty(bankAccount) && totalPrice > 0)
                    {
                        try
                        {
                            await _bankClient.MakePaymentAsync(bankAccount, "thoh", totalPrice, $"Purchase {quantity} electronics_machine from THOH");
                            _logger.LogInformation($"[Machine] Payment sent to THOH for order {orderId}");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"[Machine] Error paying THOH for machine order {orderId}");
                            return false;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"[Machine] Invalid payment details for THOH machine order {orderId}");
                        return false;
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogWarning($"[Machine] Timeout on attempt {attempt}/{maxRetries} for THOH machine purchase");
                    if (attempt == maxRetries)
                    {
                        _logger.LogError("[Machine] All attempts failed due to timeout for THOH machine purchase");
                        return false;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt)); // Quick retry for simulation
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Machine] Exception during machine purchase from THOH attempt {attempt}");
                    if (attempt == maxRetries)
                        return false;
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt)); // Quick retry for simulation
                }
            }
            
            return false; // All retries failed
        }
    }
}
