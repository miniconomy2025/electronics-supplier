using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using esAPI.Clients;
using esAPI.Data;
using esAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace esAPI.Services
{
    public class SimulationDayOrchestrator
    {
        private readonly ICommercialBankClient _bankClient;
        private readonly AppDbContext _db;
        private readonly HttpClient _client;
        private readonly ILogger<SimulationDayOrchestrator> _logger;

        public SimulationDayOrchestrator(
            ICommercialBankClient bankClient,
            AppDbContext db,
            IHttpClientFactory httpClientFactory,
            ILogger<SimulationDayOrchestrator> logger)
        {
            _bankClient = bankClient;
            _db = db;
            _client = httpClientFactory.CreateClient("commercial-bank");
            _logger = logger;
        }

        public class OrchestratorResult
        {
            public bool Success { get; set; }
            public string? AccountNumber { get; set; }
            public string? Error { get; set; }
        }

        public async Task<OrchestratorResult> OrchestrateAsync()
        {
            _logger.LogInformation("Starting simulation orchestration: creating bank account and setting notification URL.");
            var accountResult = await CreateBankAccountAsync();
            if (!accountResult.Success && accountResult.Error != "accountAlreadyExists")
            {
                _logger.LogError("Failed to create bank account: {Error}", accountResult.Error);
                return accountResult;
            }

            _logger.LogInformation("Simulation orchestration completed successfully. AccountNumber: {AccountNumber}", accountResult.AccountNumber);
            return new OrchestratorResult
            {
                Success = true,
                AccountNumber = accountResult.AccountNumber,
                Error = accountResult.Error
            };
        }

        private async Task<OrchestratorResult> CreateBankAccountAsync()
        {
            _logger.LogInformation("Attempting to create a new bank account via Commercial Bank API.");
            var accountResponse = await _client.PostAsync("/account", null);
            if (accountResponse.StatusCode == HttpStatusCode.Created)
            {
                var content = await accountResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var accountNumber = doc.RootElement.GetProperty("account_number").GetString();
                _logger.LogInformation("Bank account created successfully. AccountNumber: {AccountNumber}", accountNumber);
                await StoreAccountNumberInDbAsync(accountNumber);
                return new OrchestratorResult { Success = true, AccountNumber = accountNumber };
            }
            else if (accountResponse.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Bank account already exists (409). Retrieving existing account details.");
                var existingAccountNumber = await _bankClient.GetAccountDetailsAsync();
                if (!string.IsNullOrEmpty(existingAccountNumber))
                {
                    _logger.LogInformation("Retrieved existing account number: {AccountNumber}", existingAccountNumber);
                    await StoreAccountNumberInDbAsync(existingAccountNumber);
                    return new OrchestratorResult { Success = false, Error = "accountAlreadyExists", AccountNumber = existingAccountNumber };
                }
                else
                {
                    _logger.LogError("Failed to retrieve existing account details.");
                    return new OrchestratorResult { Success = false, Error = "Failed to retrieve existing account details" };
                }
            }
            else
            {
                var content = await accountResponse.Content.ReadAsStringAsync();
                _logger.LogError("Unexpected response from Commercial Bank API: {StatusCode} {Content}", accountResponse.StatusCode, content);
                return new OrchestratorResult { Success = false, Error = content };
            }
        }

        private async Task StoreAccountNumberInDbAsync(string? accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
            {
                _logger.LogWarning("No account number provided to store in DB.");
                return;
            }
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyId == 1);
            if (company != null)
            {
                company.BankAccountNumber = accountNumber;
                await _db.SaveChangesAsync();
                _logger.LogInformation("Stored account number {AccountNumber} in database for CompanyId=1.", accountNumber);
            }
            else
            {
                _logger.LogWarning("Company with ID=1 not found. Could not store account number.");
            }
        }


    }
}