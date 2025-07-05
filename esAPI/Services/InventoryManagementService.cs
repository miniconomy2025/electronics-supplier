using System.Data;
using System.Text.Json;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace esAPI.Services;

public class InventoryManagementService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryManagementService> _logger;

    // Define low-stock thresholds and reorder amounts
    private readonly Dictionary<string, int> _lowStockThresholds = new()
    {
        { "Copper", 10 },
        { "Silicon", 10 }
    };

    private readonly Dictionary<string, int> _reorderAmounts = new()
    {
        { "Copper", 50 },
        { "Silicon", 50 }
    };

    public InventoryManagementService(IServiceProvider serviceProvider, ILogger<InventoryManagementService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inventory Management Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckInventoryAndReorderAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the inventory management service.");
            }

            // Wait for 1 minute before checking again
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckInventoryAndReorderAsync()
    {
        _logger.LogInformation("Checking inventory levels...");

        // We need to create a new scope to resolve scoped services like DbContext
        // inside a singleton background service. This is a critical pattern.
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get current stock levels for materials we care about
        var currentStock = await dbContext.Materials
            .Where(m => _lowStockThresholds.ContainsKey(m.MaterialName))
            .Select(m => new
            {
                m.MaterialId,
                m.MaterialName,
                Quantity = m.MaterialOrderItems.SelectMany(i => i.Order!.Items)
                    .Where(supply => supply.Order!.ReceivedAt != null && supply.Order.Items.All(item => item.Order!.ReceivedAt == null))
                    .Count()
            })
            .ToListAsync();
        
        // Let's get supplierId for "THoH"
        var thohSupplier = await dbContext.MaterialSuppliers
            .FirstOrDefaultAsync(s => s.SupplierName == "THoH");

        if (thohSupplier == null)
        {
            _logger.LogWarning("Cannot reorder: Supplier 'THoH' not found in database.");
            return;
        }

        var itemsToOrder = new List<object>();
        foreach (var stockItem in currentStock)
        {
            if (stockItem.Quantity < _lowStockThresholds[stockItem.MaterialName])
            {
                _logger.LogInformation("Low stock for {Material}: {Quantity} units. Threshold is {Threshold}. Queueing reorder.",
                    stockItem.MaterialName, stockItem.Quantity, _lowStockThresholds[stockItem.MaterialName]);

                itemsToOrder.Add(new { material_id = stockItem.MaterialId, amount = _reorderAmounts[stockItem.MaterialName] });
            }
        }

        if (itemsToOrder.Any())
        {
            await PlaceReorderAsync(dbContext, thohSupplier.SupplierId, itemsToOrder);
        }
        else
        {
            _logger.LogInformation("Inventory levels are sufficient. No reorder needed.");
        }
    }

    private async Task PlaceReorderAsync(DbContext context, int supplierId, List<object> items)
    {
        _logger.LogInformation("Placing automatic reorder to supplier ID {SupplierId} for {ItemCount} item types.", supplierId, items.Count);
        
        var itemsJson = JsonSerializer.Serialize(items);
        var createdOrderIdParam = new NpgsqlParameter("p_created_order_id", DbType.Int32)
        {
            Direction = ParameterDirection.InputOutput,
            Value = DBNull.Value
        };

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "CALL create_material_order_with_items(@p_supplier_id, @p_items::jsonb, @p_created_order_id)",
                new NpgsqlParameter("p_supplier_id", supplierId),
                new NpgsqlParameter("p_items", itemsJson),
                createdOrderIdParam
            );
            
            _logger.LogInformation("Successfully placed automatic reorder. New Order ID: {OrderId}", createdOrderIdParam.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place automatic reorder.");
        }
    }
}