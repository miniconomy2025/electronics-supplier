using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace esAPI.Services
{
    public class RetryQueuePublisher
    {
        private readonly IAmazonSQS _sqs;
        private readonly ILogger<RetryQueuePublisher> _logger;
        private readonly string _queueUrl;

        public RetryQueuePublisher(IAmazonSQS sqs, ILogger<RetryQueuePublisher> logger, IConfiguration config)
        {
            _sqs = sqs;
            _logger = logger;
            _queueUrl = config["Retry:QueueUrl"] ?? throw new ArgumentNullException("Retry:QueueUrl not configured");
        }

        public async Task PublishAsync(IRetryJob job)
        {
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
