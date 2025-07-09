using System.Data;
using esAPI.Configuration;
using esAPI.Data;
using esAPI.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace esAPI.Services;

file class SupplierQuote
{
    public int SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int AvailableStock { get; init; }
}


public class InventoryManagementService(IServiceProvider serviceProvider, ILogger<InventoryManagementService> logger, IOptions<InventoryConfig> config) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<InventoryManagementService> _logger = logger;
    private readonly InventoryConfig _config = config.Value;

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
                _logger.LogError(ex, "An error occurred in the inventory management service loop.");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckInventoryAndReorderAsync()
    {

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clientFactory = scope.ServiceProvider.GetRequiredService<SupplierApiClientFactory>();

        // var sim = dbContext.Simulations.FirstOrDefault(s => s.IsRunning);
        // if (sim == null)
        //     throw new InvalidOperationException("Simulation not running.");

        var materialsToMonitor = _config.MonitoredMaterials.Select(m => m.Name).ToList();

        var effectiveStockLevels = await dbContext.EffectiveMaterialStock
            .Where(s => materialsToMonitor.Contains(s.MaterialName))
            .ToListAsync();

        var allSuppliers = await dbContext.Companies
            .Where(c => c.CompanyName == "thoh" || c.CompanyName == "recycler")
            .ToListAsync();

        if (!allSuppliers.Any())
        {
            _logger.LogWarning("Cannot reorder: No suppliers found in the companies table.");
            return;
        }



        foreach (var stockItem in effectiveStockLevels)
        {
            var materialConfig = _config.MonitoredMaterials.First(m => m.Name.Equals(stockItem.MaterialName, StringComparison.OrdinalIgnoreCase));

            if (stockItem.EffectiveQuantity >= materialConfig.LowStockThreshold)
            {
                continue;
            }

            int amountNeeded = materialConfig.ReorderAmount;

            var quotes = new List<SupplierQuote>();
            foreach (var supplier in allSuppliers)
            {
                try
                {
                    var client = clientFactory.GetClient(supplier.CompanyName);
                    var supplierInventory = await client.GetAvailableMaterialsAsync();
                    var materialInfo = supplierInventory.FirstOrDefault(m => m.MaterialId == stockItem.MaterialId);

                    if (materialInfo != null && materialInfo.AvailableStock > 0)
                    {
                        quotes.Add(new SupplierQuote
                        {
                            SupplierId = supplier.CompanyId,
                            SupplierName = supplier.CompanyName,
                            Price = materialInfo.Price,
                            AvailableStock = materialInfo.AvailableStock
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get inventory data from supplier '{SupplierName}'", supplier.CompanyName);
                }
            }

            if (!quotes.Any())
            {

                continue;
            }

            var sortedQuotes = quotes.OrderBy(q => q.Price).ToList();

            foreach (var quote in sortedQuotes)
            {
                if (amountNeeded <= 0) break;

                int amountToOrder = Math.Min(amountNeeded, quote.AvailableStock);

                await PlaceReorderAsync(dbContext, quote.SupplierId, stockItem.MaterialId, amountToOrder);

                amountNeeded -= amountToOrder;
            }

            if (amountNeeded > 0)
            {

                //do something here 

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
                "CALL create_material_order(@p_supplier_id, @p_material_id, @p_amount, @p_current_day, @p_created_order_id)",
                new NpgsqlParameter("p_supplier_id", supplierId),
                new NpgsqlParameter("p_material_id", materialId),
                new NpgsqlParameter("p_amount", amount),
                new NpgsqlParameter("p_current_day", 1),
                createdOrderIdParam
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place automatic reorder for material ID {MaterialId}.", materialId);
        }
    }
}