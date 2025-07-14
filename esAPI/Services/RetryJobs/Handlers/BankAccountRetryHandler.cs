using System.Text.Json;
using esAPI.Clients;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Services
{
    public class BankAccountRetryHandler : IRetryHandler<BankAccountRetryJob>
    {
        private readonly AppDbContext _db;
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<BankAccountRetryHandler> _logger;

        public BankAccountRetryHandler(AppDbContext db, ICommercialBankClient bankClient, ILogger<BankAccountRetryHandler> logger)
        {
            _db = db;
            _bankClient = bankClient;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(BankAccountRetryJob job, CancellationToken token)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyId == job.CompanyId, token);
            if (company == null)
            {
                _logger.LogError("❌ Company with ID {CompanyId} not found. Skipping retry.", job.CompanyId);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(company.BankAccountNumber))
            {
                _logger.LogInformation("🏦 Company {CompanyId} already has a bank account. Skipping creation.", job.CompanyId);
                return true;
            }

            var response = await _bankClient.CreateAccountAsync(new { notification_url = job.NotificationUrl });
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("❌ Failed to create bank account for Company {CompanyId}. Status: {StatusCode}", job.CompanyId, response.StatusCode);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            var accountNumber = parsed.GetProperty("account_number").GetString();

            company.BankAccountNumber = accountNumber;
            await _db.SaveChangesAsync(token);

            _logger.LogInformation("✅ Created bank account {AccountNumber} for Company {CompanyId}", accountNumber, job.CompanyId);
            return true;
        }
    }
}
