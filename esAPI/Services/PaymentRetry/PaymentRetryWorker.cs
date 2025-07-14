using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using esAPI.Clients;
using esAPI.Data;
using esAPI.DTOs;
using esAPI.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Services.PaymentRetry;

public class PaymentRetryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<PaymentRetryWorker> _logger;

    public PaymentRetryWorker(IServiceProvider serviceProvider, IAmazonSQS sqsClient, IConfiguration config, ILogger<PaymentRetryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _sqsClient = sqsClient;
        _logger = logger;
        _queueUrl = config["SqsQueues:BankPaymentRetryQueueUrl"]!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Retry Worker is starting, listening to queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 20
            };

            var response = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);

            if (response?.Messages != null)
            {
                foreach (var message in response.Messages)
                {
                    using var scope = _serviceProvider.CreateScope();
                    bool success = await ProcessRetryMessageAsync(scope.ServiceProvider, message.Body);
                    if (success)
                    {
                        await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                    }
                }
            }
        }
    }

    private async Task<bool> ProcessRetryMessageAsync(IServiceProvider sp, string messageBody)
    {
        var paymentEvent = JsonSerializer.Deserialize<PaymentRetryEvent>(messageBody);
        if (paymentEvent == null)
        {
            _logger.LogError("Could not deserialize payment retry message. Deleting poison pill message. Body: {Body}", messageBody);
            return true;
        }

        _logger.LogInformation("Retrying payment for local order {OrderId}.", paymentEvent.LocalOrderId);

        var bankClient = sp.GetRequiredService<ICommercialBankClient>();
        var dbContext = sp.GetRequiredService<AppDbContext>();

        try
        {

            var order = await dbContext.MaterialOrders
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.OrderId == paymentEvent.LocalOrderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found in the database. The order may have been cancelled or deleted. Deleting retry message.", paymentEvent.LocalOrderId);
                return true;
            }

            if (order.OrderStatus?.Status != "PAYMENT_FAILED" && order.OrderStatus?.Status != "PENDING")
            {
                _logger.LogInformation("Order {OrderId} is no longer in a state requiring payment (current status: '{Status}'). Deleting retry message.",
                    paymentEvent.LocalOrderId, order.OrderStatus?.Status);
                return true;
            }

            _logger.LogInformation("Order {OrderId} is valid for payment retry. Attempting to pay supplier '{Supplier}'.", paymentEvent.LocalOrderId, paymentEvent.RecipientName);

            await bankClient.MakePaymentAsync(
                paymentEvent.RecipientBankAccount,
                "commercial-bank",
                paymentEvent.Amount,
                paymentEvent.Reference
            );

            var acceptedStatus = await dbContext.OrderStatuses.FirstAsync(s => s.Status == "ACCEPTED");

            order.OrderStatusId = acceptedStatus.StatusId;
            await dbContext.SaveChangesAsync();

            return true;
        }
        catch (ApiCallFailedException ex)
        {
            _logger.LogError(ex, "Payment retry for order {OrderId} FAILED again. The bank API returned a failure. The message will be retried later.", paymentEvent.LocalOrderId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An unexpected exception occurred while retrying payment for order {OrderId}. The message will be retried later.", paymentEvent.LocalOrderId);
            return false;
        }
    }
}