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

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var currentStock = await dbContext.CurrentSupplies
            .Where(s => _lowStockThresholds.ContainsKey(s.MaterialName))
            .ToListAsync();

        var thohSupplier = await dbContext.MaterialSuppliers
            .FirstOrDefaultAsync(s => s.SupplierName == "THoH");

        if (thohSupplier == null)
        {
            _logger.LogWarning("Cannot reorder: Supplier 'THoH' not found in database.");
            return;
        }

        
        foreach (var stockItem in currentStock)
        {
            if (stockItem.AvailableSupply < _lowStockThresholds[stockItem.MaterialName])
            {
                _logger.LogInformation("Low stock for {Material}: {Quantity} units. Threshold is {Threshold}. Placing reorder.",
                    stockItem.MaterialName, stockItem.AvailableSupply, _lowStockThresholds[stockItem.MaterialName]);

                await PlaceReorderAsync(dbContext,
                    thohSupplier.SupplierId,
                    stockItem.MaterialId,
                    _reorderAmounts[stockItem.MaterialName]
                );
            }
        }
    }

    private async Task PlaceReorderAsync(DbContext context, int supplierId, int materialId, int amount)
    {
        
        var createdOrderIdParam = new NpgsqlParameter("p_created_order_id", DbType.Int32)
        {
            Direction = ParameterDirection.InputOutput,
            Value = DBNull.Value
        };

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "CALL create_material_order(@p_supplier_id, @p_material_id, @p_remaining_amount, @p_created_order_id)",
                new NpgsqlParameter("p_supplier_id", supplierId),
                new NpgsqlParameter("p_material_id", materialId),
                new NpgsqlParameter("p_remaining_amount", amount),
                createdOrderIdParam
            );

            _logger.LogInformation("Successfully placed automatic reorder. New Order ID: {OrderId}", createdOrderIdParam.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place automatic reorder for material ID {MaterialId}.", materialId);
        }
    }
}