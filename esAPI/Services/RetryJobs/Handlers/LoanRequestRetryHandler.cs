using esAPI.Clients;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace esAPI.Services
{
    public class LoanRequestRetryHandler : IRetryHandler<LoanRequestRetryJob>
    {
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<LoanRequestRetryHandler> _logger;

        public LoanRequestRetryHandler(ICommercialBankClient bankClient, ILogger<LoanRequestRetryHandler> logger)
        {
            _bankClient = bankClient;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(LoanRequestRetryJob job, CancellationToken token)
        {
            _logger.LogInformation("🔄 Processing LoanRequestRetryJob attempt {Attempt} for amount {Amount}", job.RetryAttempt, job.Amount);

            try
            {
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
