
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using esAPI.DTOs.Orders;

namespace esAPI.Services.ElectronicsSQS;

public interface IElectronicsOrderPublisher
{
    Task<bool> PublishOrderReceivedEventAsync(ElectronicsOrderReceivedEvent orderEvent);
}


public class SqsElectronicsOrderPublisher : IElectronicsOrderPublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<SqsElectronicsOrderPublisher> _logger;

    public SqsElectronicsOrderPublisher(IAmazonSQS sqsClient, IConfiguration config, ILogger<SqsElectronicsOrderPublisher> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _queueUrl = config["SqsQueues:NewElectronicsOrderQueueUrl"]!;
        if (string.IsNullOrEmpty(_queueUrl))
        {
            throw new InvalidOperationException("New Electronics Order Queue URL is not configured.");
        }
    }

    public async Task<bool> PublishOrderReceivedEventAsync(ElectronicsOrderReceivedEvent orderEvent)
    {
        try
        {
            var messageRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = JsonSerializer.Serialize(orderEvent),
                MessageGroupId = $"customer-{orderEvent.CustomerId}"
            };
            var response = await _sqsClient.SendMessageAsync(messageRequest);

            _logger.LogInformation("Successfully published new electronics order event to SQS. Message ID: {MessageId}", response.MessageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish electronics order event to SQS queue {QueueUrl}", _queueUrl);
            return false;
        }
    }
}