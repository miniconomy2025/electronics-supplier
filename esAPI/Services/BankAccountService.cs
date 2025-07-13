using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Clients;

namespace esAPI.Services
{

    public interface IBankAccountService
    {

        Task<string?> SetupBankAccount(CancellationToken cancellationToken = default);
    }

    public class BankAccountService(AppDbContext db, ICommercialBankClient bankClient, ILogger<BankAccountService> logger)
    {
        private readonly AppDbContext _db = db;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly ILogger<BankAccountService> _logger = logger;

        private const int OurCompanyId = 1;

        public async Task<string?> SetupBankAccount(CancellationToken cancellationToken = default)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyId == 1, cancellationToken);
            if (company == null)
            {
                _logger.LogError("Electronics Supplier company (ID=1) not found in database.");
                throw new InvalidOperationException($"Company with ID {OurCompanyId} not found.");
            }

            if (!string.IsNullOrWhiteSpace(company.BankAccountNumber))
            {
                _logger.LogInformation($"Bank account already exists: {company.BankAccountNumber}");
                return company.BankAccountNumber; ;
            }

            try
            {
                var accountNumber = await _bankClient.CreateAccountAsync();
                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    _logger.LogError("Failed to create bank account: The bank API did not return an account number.");
                    return null;
                }

                company.BankAccountNumber = accountNumber;
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully created and saved new bank account: {AccountNumber}", accountNumber);
                return accountNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred during bank account setup.");
                return null;
            }


        }
    }
}