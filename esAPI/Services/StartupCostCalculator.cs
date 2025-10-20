using System.Linq;
using esAPI.Clients;
using esAPI.Data;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;
using esAPI.DTOs.Startup;

namespace esAPI.Services;

public class StartupCostCalculator : IStartupCostCalculator
{
    private readonly IThohApiClient _thohClient;
    private readonly ISupplierApiClient _materialSupplier;

    private const int InitialProductionCyclesToStock = 2;

    private readonly HashSet<string> _ourCoreMaterials = new(StringComparer.OrdinalIgnoreCase)
    {
        "Copper",
        "Silicon"
    };

    public StartupCostCalculator(
        IThohApiClient thohClient,
        ISupplierApiClient materialSupplier)
    {
        _thohClient = thohClient;
        _materialSupplier = materialSupplier;
    }

    public async Task<List<StartupPlan>> GenerateAllPossibleStartupPlansAsync()
    {
        var allStartupPlans = new List<StartupPlan>();

        var availableMachines = await _thohClient.GetAvailableMachinesAsync();
        if (!availableMachines.Any())
        {
            return allStartupPlans;
        }

        var supplierInventory = await _materialSupplier.GetAvailableMaterialsAsync();
        var supplierInventoryDict = supplierInventory.ToDictionary(m => m.MaterialName, m => m, StringComparer.OrdinalIgnoreCase);

        foreach (var machineInfo in availableMachines)
        {
            if (machineInfo.Price <= 0) continue;

            var requiredMaterials = machineInfo.InputRatio;
            if (requiredMaterials == null || requiredMaterials.Count == 0)
            {
                continue;
            }

            decimal totalMaterialCost = 0;
            bool canFulfillAllMaterials = true;

            foreach (var (materialName, ratio) in requiredMaterials)
            {
                if (!_ourCoreMaterials.Contains(materialName))
                {
                    canFulfillAllMaterials = false;
                    break;
                }
                if (!supplierInventoryDict.TryGetValue(materialName, out var materialInfoFromSupplier))
                {
                    canFulfillAllMaterials = false;
                    break;
                }
                int quantityToBuy = ratio * InitialProductionCyclesToStock;
                totalMaterialCost += materialInfoFromSupplier.PricePerKg * quantityToBuy;
            }

            if (canFulfillAllMaterials)
            {
                allStartupPlans.Add(new StartupPlan
                {
                    MachineName = machineInfo.MachineName,
                    MachineCost = machineInfo.Price,
                    MaterialsCost = totalMaterialCost
                });
            }
        }

        return allStartupPlans;
    }
}
