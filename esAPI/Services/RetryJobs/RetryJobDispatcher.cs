using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace esAPI.Services
{
    public class RetryJobDispatcher : BackgroundService
    {
        private readonly IAmazonSQS _sqs;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RetryJobDispatcher> _logger;
        private readonly Dictionary<string, Type> _jobTypes = new()
        {
            { "BankAccountRetry", typeof(BankAccountRetryJob) },
            { "BankBalanceRetry", typeof(BankBalanceRetryJob) }
            // add more here
        };

        private readonly string _queueUrl;
        

        public RetryJobDispatcher(IAmazonSQS sqs, IServiceProvider serviceProvider, ILogger<RetryJobDispatcher> logger, IConfiguration config)
        {
            _sqs = sqs;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _queueUrl = config["Retry:QueueUrl"] ?? throw new ArgumentNullException("Retry:QueueUrl not configured");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MaxNumberOfMessages = 10,
                        WaitTimeSeconds = 20
                    }, stoppingToken);

                    if (response.Messages == null || response.Messages.Count == 0)
                    {
                        continue;
                    }

                    foreach (var msg in response.Messages)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(msg.Body))
                            {
                                _logger.LogWarning("Received empty message body. Skipping.");
                                continue;
                            }

                            var root = JsonSerializer.Deserialize<JsonElement>(msg.Body);

                            if (!root.TryGetProperty("JobType", out var jobTypeProperty))
                            {
                                _logger.LogWarning("Message does not contain 'JobType' property. Skipping.");
                                continue;
                            }

                            var typeKey = jobTypeProperty.GetString();
                            if (string.IsNullOrEmpty(typeKey))
                            {
                                _logger.LogWarning("JobType is null or empty. Skipping.");
                                continue;
                            }

                            if (!_jobTypes.TryGetValue(typeKey, out var jobType))
                            {
                                _logger.LogWarning("Unknown JobType: {Type}", typeKey);
                                continue;
                            }

                            var job = (IRetryJob?)JsonSerializer.Deserialize(msg.Body, jobType);
                            if (job == null)
                            {
                                _logger.LogWarning("Failed to deserialize job of type {TypeKey}. Skipping.", typeKey);
                                continue;
                            }

                            using var scope = _serviceProvider.CreateScope();
                            var handlerType = typeof(IRetryHandler<>).MakeGenericType(jobType);
                            var handler = scope.ServiceProvider.GetService(handlerType);
                            if (handler == null)
                            {
                                _logger.LogWarning("Handler not found for job type {TypeKey}. Skipping.", typeKey);
                                continue;
                            }

                            var method = handlerType.GetMethod("HandleAsync");
                            if (method == null)
                            {
                                _logger.LogWarning("HandleAsync method not found on handler for {TypeKey}. Skipping.", typeKey);
                                continue;
                            }

                            var handled = await (Task<bool>)method.Invoke(handler, new object[] { job, stoppingToken })!;
                            if (handled)
                            {
                                await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                                _logger.LogInformation("✅ Retry job {JobType} completed and deleted", typeKey);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Failed to process retry job");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in RetryJobDispatcher main loop");
                    // Wait before retrying to avoid tight error loops
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}