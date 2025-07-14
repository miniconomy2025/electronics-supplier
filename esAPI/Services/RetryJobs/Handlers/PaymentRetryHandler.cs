using esAPI.Clients;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace esAPI.Services
{
    public class PaymentRetryHandler : IRetryHandler<PaymentRetryJob>
    {
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<PaymentRetryHandler> _logger;
        private readonly RetryQueuePublisher _retryQueuePublisher;

        private const int MaxRetries = 5;

        public PaymentRetryHandler(ICommercialBankClient bankClient, ILogger<PaymentRetryHandler> logger, RetryQueuePublisher retryQueuePublisher)
        {
            _bankClient = bankClient;
            _logger = logger;
            _retryQueuePublisher = retryQueuePublisher;
        }

        public async Task<bool> HandleAsync(PaymentRetryJob job, CancellationToken token)
        {
            _logger.LogInformation("🔄 Retry attempt {Attempt} for payment: {Amount} to {Account} at {Bank}", job.RetryAttempt, job.Amount, job.ToAccountNumber, job.ToBankName);

            try
            {
                await _bankClient.MakePaymentAsync(job.ToAccountNumber, job.ToBankName, job.Amount, job.Description);

                _logger.LogInformation("✅ Payment succeeded on retry.");
                return true; // success, remove message from queue
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Payment retry failed.");

                if (job.RetryAttempt >= MaxRetries)
                {
                    _logger.LogError("❌ Max retries reached for payment to {Account}. Giving up.", job.ToAccountNumber);
                    return true; // remove from queue or move to dead-letter queue
                }

                job.RetryAttempt++;
                await _retryQueuePublisher.PublishAsync(job); // re-queue for next retry

                // Remove current message since re-queued manually
                return true;
            }
        }
    }
}
