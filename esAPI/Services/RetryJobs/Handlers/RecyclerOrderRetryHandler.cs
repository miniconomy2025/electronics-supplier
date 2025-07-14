using esAPI.Clients;

namespace esAPI.Services
{
    public class RecyclerOrderRetryHandler : IRetryHandler<RecyclerOrderRetryJob>
{
    private readonly RecyclerApiClient _recyclerClient;
    private readonly ILogger<RecyclerOrderRetryHandler> _logger;
    private readonly RetryQueuePublisher _retryQueuePublisher;

    private const int MaxRetries = 5;

    public RecyclerOrderRetryHandler(RecyclerApiClient recyclerClient, ILogger<RecyclerOrderRetryHandler> logger, RetryQueuePublisher retryQueuePublisher)
    {
        _recyclerClient = recyclerClient;
        _logger = logger;
        _retryQueuePublisher = retryQueuePublisher;
    }

    public async Task<bool> HandleAsync(RecyclerOrderRetryJob job, CancellationToken token)
    {
        _logger.LogInformation("🔄 Retry attempt {Attempt} for recycler order: {MaterialName} x {Quantity}", job.RetryAttempt, job.MaterialName, job.QuantityInKg);

        try
        {
            var response = await _recyclerClient.PlaceRecyclerOrderAsync(job.MaterialName, job.QuantityInKg);
            if (response != null && response.IsSuccess)
            {
                _logger.LogInformation("✅ Successfully placed recycler order on retry.");
                return true; // success, remove message from queue
            }
            else
            {
                _logger.LogWarning("❌ Recycler order retry failed.");

                if (job.RetryAttempt >= MaxRetries)
                {
                    _logger.LogError("❌ Max retries reached for recycler order {MaterialName}. Giving up.", job.MaterialName);
                    return true; // remove from queue, or alternatively move to dead-letter queue
                }

                job.RetryAttempt++;
                await _retryQueuePublisher.PublishAsync(job); // re-queue for next retry
                return true; // remove current message, since we re-queued manually
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception during recycler order retry.");

            if (job.RetryAttempt >= MaxRetries)
            {
                _logger.LogError("❌ Max retries reached for recycler order {MaterialName}. Giving up.", job.MaterialName);
                return true;
            }

            job.RetryAttempt++;
            await _retryQueuePublisher.PublishAsync(job);
            return true;
        }
    }
}

}