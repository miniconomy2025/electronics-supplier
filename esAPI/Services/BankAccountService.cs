using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Clients;
using esAPI.Interfaces;

namespace esAPI.Services
{
    public class BankAccountService(AppDbContext db, ICommercialBankClient bankClient, ILogger<BankAccountService> logger)
    {
        private readonly AppDbContext _db = db;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly ILogger<BankAccountService> _logger = logger;

        public async Task SetupBankAccount(CancellationToken cancellationToken = default)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyId == 1, cancellationToken);
            if (company == null)
            {
                _logger.LogError("Electronics Supplier company (ID=1) not found in database.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(company.BankAccountNumber))
            {
                _logger.LogInformation($"Bank account already exists: {company.BankAccountNumber}");
                return;
            }

            _logger.LogInformation("No bank account found. Creating new account with Commercial Bank...");
            var accountNumber = await _bankClient.CreateAccountAsync();
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                _logger.LogError("Failed to create bank account with Commercial Bank API.");
                return;
            }

            company.BankAccountNumber = accountNumber;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Bank account created and saved: {accountNumber}");
        }
    }
}