using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Clients;

namespace esAPI.Services
{
    public interface ISimulationDayService
    {
        Task<bool> ExecuteDayAsync(int dayNumber);
    }

    public class SimulationDayService : ISimulationDayService
    {
        private readonly AppDbContext _context;
        private readonly IBankService _bankService;
        private readonly ICommercialBankClient _bankClient;
        private readonly IRecyclerApiClient _recyclerClient;
        private readonly IElectronicsService _electronicsService;
        private readonly IMaterialOrderingService _materialOrderingService;
        private readonly IMachineManagementService _machineManagementService;
        private readonly ILogger<SimulationDayService> _logger;

        public SimulationDayService(
            AppDbContext context,
            IBankService bankService,
            ICommercialBankClient bankClient,
            IRecyclerApiClient recyclerClient,
            IElectronicsService electronicsService,
            IMaterialOrderingService materialOrderingService,
            IMachineManagementService machineManagementService,
            ILogger<SimulationDayService> logger)
        {
            _context = context;
            _bankService = bankService;
            _bankClient = bankClient;
            _recyclerClient = recyclerClient;
            _electronicsService = electronicsService;
            _materialOrderingService = materialOrderingService;
            _machineManagementService = machineManagementService;
            _logger = logger;
        }

        public async Task<bool> ExecuteDayAsync(int dayNumber)
        {
            _logger.LogInformation("📊 Running simulation logic for Day {DayNumber}", dayNumber);

            // 1. Query bank and store our balance
            await HandleBankingOperationsAsync(dayNumber);

            // 2. Query recycler for materials (for logging purposes)
            await LogRecyclerMaterialsAsync();

            // 3. Get our own stock levels
            var (copperStock, siliconStock) = await GetCurrentStockLevelsAsync();

            // 4. Ensure we have working machines
            await _machineManagementService.EnsureMachinesAvailableAsync();

            // 5. Order materials if stock is low
            await OrderMaterialsIfNeededAsync(copperStock, siliconStock, dayNumber);

            // 6. Produce electronics at end of day
            await ProduceElectronicsAsync();

            _logger.LogInformation("✅ Simulation day {DayNumber} completed successfully", dayNumber);
            return true;
        }

        private async Task HandleBankingOperationsAsync(int dayNumber)
        {
            _logger.LogInformation("🏦 Querying bank balance for day {DayNumber}", dayNumber);
            try
            {
                var balance = await _bankService.GetAndStoreBalance(dayNumber);
                _logger.LogInformation("✅ Bank balance stored for day {DayNumber}: {Balance}", dayNumber, balance);

                // Check if we need a loan (not on day 1, that's handled in startup)
                if (dayNumber > 1 && balance <= 10000m)
                {
                    await RequestEmergencyLoanAsync(dayNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bank balance retrieval failed for day {DayNumber}, but simulation will continue", dayNumber);
            }
        }

        private async Task RequestEmergencyLoanAsync(int dayNumber)
        {
            _logger.LogInformation("🏦 Bank balance is low (<= 10,000). Attempting to request a loan...");
            const decimal loanAmount = 20000000m; // 20 million
            
            try
            {
                string? loanSuccess = await _bankClient.RequestLoanAsync(loanAmount);
                if (loanSuccess == null)
                {
                    _logger.LogWarning("❌ Failed to request loan for day {DayNumber}. Will retry next day if still low.", dayNumber);
                }
                else
                {
                    _logger.LogInformation("✅ Loan requested successfully for day {DayNumber}: {LoanNumber}", dayNumber, loanSuccess);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while requesting loan at start of day {DayNumber}", dayNumber);
            }
        }

        private async Task LogRecyclerMaterialsAsync()
        {
            try
            {
                var recyclerMaterials = await _recyclerClient.GetAvailableMaterialsAsync();
                foreach (var mat in recyclerMaterials)
                {
                    _logger.LogInformation($"[Recycler] {mat.MaterialName}: AvailableQuantity={mat.AvailableQuantity}, PricePerKg={mat.PricePerKg}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Recycler] Failed to query recycler materials");
            }
        }

        private async Task<(int copperStock, int siliconStock)> GetCurrentStockLevelsAsync()
        {
            try
            {
                var ownSupplies = await _context.CurrentSupplies.ToListAsync();
                int ownCopper = ownSupplies.FirstOrDefault(s => s.MaterialName.ToLower() == "copper")?.AvailableSupply ?? 0;
                int ownSilicon = ownSupplies.FirstOrDefault(s => s.MaterialName.ToLower() == "silicon")?.AvailableSupply ?? 0;
                
                _logger.LogInformation($"[Stock] Our Copper: {ownCopper}, Our Silicon: {ownSilicon}");
                return (ownCopper, ownSilicon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Stock] Error querying current stock levels");
                return (0, 0);
            }
        }

        private async Task OrderMaterialsIfNeededAsync(int copperStock, int siliconStock, int dayNumber)
        {
            var materialsToCheck = new[]
            {
                ("copper", copperStock),
                ("silicon", siliconStock)
            };

            foreach (var (materialName, stock) in materialsToCheck)
            {
                try
                {
                    await _materialOrderingService.OrderMaterialIfLowStockAsync(materialName, stock, dayNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Order] Error processing material order for {materialName}");
                }
            }
        }

        private async Task ProduceElectronicsAsync()
        {
            try
            {
                var result = await _electronicsService.ProduceElectronicsAsync();
                _logger.LogInformation($"[Production] Produced {result.ElectronicsCreated} electronics. Materials used: {string.Join(", ", result.MaterialsUsed.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Production] Error producing electronics at end of day");
            }
        }
    }
}