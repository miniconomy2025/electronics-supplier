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
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20
                }, stoppingToken);

                foreach (var msg in response.Messages)
                {
                    try
                    {
                        var root = JsonSerializer.Deserialize<JsonElement>(msg.Body);
                        var typeKey = root.GetProperty("JobType").GetString();

                        if (!_jobTypes.TryGetValue(typeKey!, out var jobType))
                        {
                            _logger.LogWarning("❌ Unknown JobType: {Type}", typeKey);
                            continue;
                        }

                        var job = (IRetryJob)JsonSerializer.Deserialize(msg.Body, jobType)!;

                        using var scope = _serviceProvider.CreateScope();
                        var handlerType = typeof(IRetryHandler<>).MakeGenericType(jobType);
                        var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                        var method = handlerType.GetMethod("HandleAsync");

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
        }
    }

}