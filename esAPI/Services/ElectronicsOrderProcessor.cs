using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using esAPI.Data;
using esAPI.DTOs.Orders;
using esAPI.Interfaces;
using esAPI.Models;
using esAPI.Services;

namespace esAPI.Services;



public class ElectronicsOrderProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<ElectronicsOrderProcessor> _logger;

    public ElectronicsOrderProcessor(IServiceProvider serviceProvider, IAmazonSQS sqsClient, IConfiguration config, ILogger<ElectronicsOrderProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _sqsClient = sqsClient;
        _logger = logger;
        _queueUrl = config["SqsQueues:NewElectronicsOrderQueueUrl"]!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Electronics Order Processor worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 1, // Process one order at a time for simplicity
                WaitTimeSeconds = 20
            };

            var response = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);

            if (response?.Messages != null)
            {
                foreach (var message in response.Messages)
                {
                    // Create a new scope for each message to get fresh service instances (like DbContext).
                    using var scope = _serviceProvider.CreateScope();
                    bool success = await ProcessOrderMessageAsync(scope.ServiceProvider, message.Body);
                    if (success)
                    {
                        // Delete the message from the queue only if processing was successful.
                        await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                    }
                }
            }


        }
    }

    private async Task<bool> ProcessOrderMessageAsync(IServiceProvider sp, string messageBody)
    {
        _logger.LogInformation("Processing new electronics order event.");

        // Resolve all necessary services from the provided scope.
        var dbContext = sp.GetRequiredService<AppDbContext>();
        var stateService = sp.GetRequiredService<ISimulationStateService>();
        var orderExpirationService = sp.GetRequiredService<OrderExpirationService>();

        try
        {
            var orderEvent = JsonSerializer.Deserialize<ElectronicsOrderReceivedEvent>(messageBody);
            if (orderEvent == null)
            {
                _logger.LogError("Could not deserialize message body into ElectronicsOrderReceivedEvent. Message will be deleted.");
                return true; // Return true to delete the poison pill message.
            }

            // --- ALL THE LOGIC FROM YOUR ORIGINAL CONTROLLER IS NOW HERE ---

            // 1. Validate customer
            var manufacturer = await dbContext.Companies.FindAsync(orderEvent.CustomerId);
            if (manufacturer == null)
            {
                _logger.LogError("Invalid order: Customer with ID {CustomerId} not found. Deleting message.", orderEvent.CustomerId);
                return true; // Invalid data, delete message.
            }

            // 2. Check available stock
            var availableStock = await orderExpirationService.GetAvailableElectronicsCountAsync();
            if (availableStock < orderEvent.Amount)
            {
                _logger.LogWarning("Order from {Customer} for {Amount} units cannot be fulfilled. Stock: {Stock}. This order will be retried later.",
                    manufacturer.CompanyName, orderEvent.Amount, availableStock);
                return false; // Return false to NOT delete the message. It will be retried.
            }

            // 3. Create and save the order as a transaction
            // Using a transaction ensures that the order creation and inventory reservation are atomic.
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var order = new esAPI.Models.ElectronicsOrder
            {
                ManufacturerId = manufacturer.CompanyId,
                RemainingAmount = orderEvent.Amount,
                OrderedAt = stateService.GetCurrentSimulationTime(3),
                OrderStatusId = 1, // PENDING
                TotalAmount = orderEvent.Amount
            };
            dbContext.ElectronicsOrders.Add(order);
            await dbContext.SaveChangesAsync(); // Save to get the order.OrderId

            // 4. Reserve inventory
            var reservationSuccess = await orderExpirationService.ReserveElectronicsForOrderAsync(order.OrderId, orderEvent.Amount);
            if (!reservationSuccess)
            {
                _logger.LogError("Failed to reserve electronics for order {OrderId}. Rolling back transaction.", order.OrderId);
                await transaction.RollbackAsync();
                return false; // Return false to retry the entire process later.
            }

            // 5. Commit the transaction
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully created and reserved inventory for new electronics order {OrderId}.", order.OrderId);
            // After this, another process would handle notifying the bank/customer about payment.
            return true; // Return true to delete the message from the queue.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing an electronics order message.");
            return false; // Don't delete the message, let it be retried after the visibility timeout.
        }
    }
}