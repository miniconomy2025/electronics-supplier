using System.Data;
using esAPI.Configuration;
using esAPI.Data;
using esAPI.Models;
using esAPI.Interfaces.Services;
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
    private const int ProductionDaysToCover = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inventory Management Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckProductionNeedsAndTriggerAcquisitionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the inventory management service loop.");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckProductionNeedsAndTriggerAcquisitionAsync()
    {
        _logger.LogInformation("Checking production needs against effective inventory...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var acquisitionService = scope.ServiceProvider.GetRequiredService<IMaterialAcquisitionService>();

        var dailyNeeds = await dbContext.DailyMaterialConsumption.ToListAsync();
        if (!dailyNeeds.Any())
        {
            _logger.LogWarning("Could not calculate daily material consumption. No operational machines or ratios defined?");
            return;
        }

        var effectiveStockLevels = await dbContext.EffectiveMaterialStock
            .ToDictionaryAsync(s => s.MaterialId, s => s.EffectiveQuantity);

        bool isAcquisitionNeeded = false;

        foreach (var materialNeed in dailyNeeds)
        {
            long requiredStock = materialNeed.TotalDailyConsumption * ProductionDaysToCover;
            effectiveStockLevels.TryGetValue(materialNeed.MaterialId, out long currentStock);

            if (currentStock < requiredStock)
            {
                _logger.LogWarning("DEFICIT DETECTED for {MaterialName}. Required: {Required}, Effective: {Current}. Triggering acquisition process.",
                    materialNeed.MaterialName, requiredStock, currentStock);
                isAcquisitionNeeded = true;

                break;
            }
            else
            {
                _logger.LogInformation("OK: Stock for {MaterialName} is sufficient. Required: {Required}, Effective: {Current}.",
                   materialNeed.MaterialName, requiredStock, currentStock);
            }
        }

        if (isAcquisitionNeeded)
        {
            _logger.LogInformation("All material stock levels are insufficient. Triggering ExecutePurchaseStrategyAsync");
            await acquisitionService.ExecutePurchaseStrategyAsync();
        }
        else
        {
            _logger.LogInformation("All material stock levels are sufficient. No acquisition needed at this time.");
        }
    }


}
