
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using esAPI.DTOs;

namespace esAPI.Services.PaymentRetry;

public interface IPaymentRetryHandler
{
    Task QueuePaymentForRetryAsync(PaymentRetryEvent paymentEvent);
}

public class SqsPaymentRetryHandler : IPaymentRetryHandler
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<SqsPaymentRetryHandler> _logger;

    public SqsPaymentRetryHandler(IAmazonSQS sqsClient, IConfiguration config, ILogger<SqsPaymentRetryHandler> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _queueUrl = config["SqsQueues:BankPaymentRetryQueueUrl"]!;
        if (string.IsNullOrEmpty(_queueUrl))
        {
            throw new InvalidOperationException("Bank Payment Retry Queue URL is not configured.");
        }
    }

    public async Task QueuePaymentForRetryAsync(PaymentRetryEvent paymentEvent)
    {
        try
        {
            var messageRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = JsonSerializer.Serialize(paymentEvent)
            };
            await _sqsClient.SendMessageAsync(messageRequest);
            _logger.LogInformation("Successfully queued payment for local order {OrderId} for later retry.", paymentEvent.LocalOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment retry message to SQS for order {OrderId}.", paymentEvent.LocalOrderId);
        }
    }
}