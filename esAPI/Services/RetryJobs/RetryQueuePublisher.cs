using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using esAPI.Interfaces;

namespace esAPI.Services
{
    public class RetryQueuePublisher
    {
        private readonly IAmazonSQS _sqs;
        private readonly ILogger<RetryQueuePublisher> _logger;
        private readonly ISimulationStateService _stateService;
        private readonly string _queueUrl;

        public RetryQueuePublisher(IAmazonSQS sqs, ILogger<RetryQueuePublisher> logger, IConfiguration config, ISimulationStateService stateService)
        {
            _sqs = sqs;
            _logger = logger;
            _stateService = stateService;
            _queueUrl = config?["Retry:QueueUrl"] ?? throw new ArgumentNullException(nameof(config), "Configuration is null or Retry:QueueUrl not configured");
        }

        public async Task PublishAsync(IRetryJob job)
        {
            // Only publish retry jobs when simulation is running
            if (!_stateService.IsRunning)
            {
                _logger.LogDebug("🔄 Simulation not running, skipping retry job publication for {JobType}", job.JobType);
                return;
            }

            var json = JsonSerializer.Serialize(job);
            var request = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = json,
                MessageGroupId = "default", // or something meaningful per job type
                MessageDeduplicationId = Guid.NewGuid().ToString()
            };
            var response = await _sqs.SendMessageAsync(request);
            _logger.LogInformation("✅ Published retry job {JobType} with MessageId: {MessageId}", job.JobType, response.MessageId);
        }
    }
}
