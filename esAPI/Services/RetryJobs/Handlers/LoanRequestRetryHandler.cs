using esAPI.Clients;
using esAPI.Data;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace esAPI.Services
{
    public class LoanRequestRetryHandler : IRetryHandler<LoanRequestRetryJob>
    {
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<LoanRequestRetryHandler> _logger;
        private readonly AppDbContext _db;

        public LoanRequestRetryHandler(AppDbContext db, ICommercialBankClient bankClient, ILogger<LoanRequestRetryHandler> logger)
        {
            _db = db;
            _bankClient = bankClient;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(LoanRequestRetryJob job, CancellationToken token)
        {
            _logger.LogInformation("🔄 Processing LoanRequestRetryJob attempt {Attempt} for amount {Amount}", job.RetryAttempt, job.Amount);

            try
            {
                   var company = _db.Companies.FirstOrDefault(c => c.CompanyId == 1);
                    if (company == null || string.IsNullOrWhiteSpace(company.BankAccountNumber))
                    {
                        _logger.LogWarning("🏦 Loan retry skipped — bank account not ready.");
                        return false; // Will be retried again
                    }

                var loanNumber = await _bankClient.RequestLoanAsync(job.Amount);

                if (loanNumber != null)
                {
                    _logger.LogInformation("✅ Loan request successful on retry. Loan number: {LoanNumber}", loanNumber);
                    return true; // Success, no more retries needed
                }
                else
                {
                    _logger.LogWarning("❌ Loan request failed on retry attempt {Attempt}", job.RetryAttempt);
                    return false; // Indicate failure so message stays in queue for retry
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception during loan request retry attempt {Attempt}", job.RetryAttempt);
                return false;
            }
        }
    }
}
