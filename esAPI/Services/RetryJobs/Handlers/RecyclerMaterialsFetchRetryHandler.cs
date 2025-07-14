using esAPI.Clients;
using Microsoft.Extensions.Logging;

namespace esAPI.Services
{
    public class RecyclerMaterialsFetchRetryHandler : IRetryHandler<RecyclerMaterialsFetchRetryJob>
    {
        private readonly RecyclerApiClient _recyclerClient;
        private readonly ILogger<RecyclerMaterialsFetchRetryHandler> _logger;
        private readonly RetryQueuePublisher _retryQueuePublisher;

        private const int MaxRetries = 5;

        public RecyclerMaterialsFetchRetryHandler(
            RecyclerApiClient recyclerClient,
            ILogger<RecyclerMaterialsFetchRetryHandler> logger,
            RetryQueuePublisher retryQueuePublisher)
        {
            _recyclerClient = recyclerClient;
            _logger = logger;
            _retryQueuePublisher = retryQueuePublisher;
        }

        public async Task<bool> HandleAsync(RecyclerMaterialsFetchRetryJob job, CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔄 Retry attempt {Attempt} for fetching recycler materials", job.RetryAttempt);

            try
            {
                var materials = await _recyclerClient.GetAvailableMaterialsAsync();

                if (materials != null && materials.Any())
                {
                    _logger.LogInformation("✅ Successfully fetched recycler materials on retry.");
                    // You can optionally trigger some event or store the results somewhere
                    return true; // success, remove from queue
                }
                else
                {
                    _logger.LogWarning("❌ Fetching recycler materials returned empty.");

                    if (job.RetryAttempt >= MaxRetries)
                    {
                        _logger.LogError("❌ Max retries reached for fetching recycler materials. Giving up.");
                        return true; // Remove from queue or move to dead letter queue
                    }

                    job.RetryAttempt++;
                    await _retryQueuePublisher.PublishAsync(job);
                    return true; // Remove current message, because re-queued manually
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception during fetching recycler materials retry.");

                if (job.RetryAttempt >= MaxRetries)
                {
                    _logger.LogError("❌ Max retries reached for fetching recycler materials. Giving up.");
                    return true;
                }

                job.RetryAttempt++;
                await _retryQueuePublisher.PublishAsync(job);
                return true;
            }
        }
    }
}
