using System.Linq;
using esAPI.Clients;
using esAPI.Data;

namespace esAPI.Services;

public class StartupCostCalculator : IStartupCostCalculator
{
    private readonly IThohMachineApiClient _machineSupplier;
    private readonly ISupplierApiClient _materialSupplier;

    private const int InitialProductionCyclesToStock = 2;

    private readonly HashSet<string> _ourCoreMaterials = new(StringComparer.OrdinalIgnoreCase)
    {
        "Copper",
        "Silicon"
    };

    public StartupCostCalculator(
        IThohMachineApiClient machineSupplier,
        ISupplierApiClient materialSupplier)
    {
        _machineSupplier = machineSupplier;
        _materialSupplier = materialSupplier;
    }

    public async Task<List<StartupPlan>> GenerateAllPossibleStartupPlansAsync()
    {
        var allStartupPlans = new List<StartupPlan>();

        var availableMachines = await _machineSupplier.GetAvailableMachinesAsync();
        if (!availableMachines.Any())
        {
            return allStartupPlans;
        }

        var supplierInventory = await _materialSupplier.GetAvailableMaterialsAsync();
        var supplierInventoryDict = supplierInventory.ToDictionary(m => m.MaterialName, m => m, StringComparer.OrdinalIgnoreCase);

        foreach (var machineInfo in availableMachines)
        {
            if (machineInfo.Price <= 0) continue;

            var requiredMaterials = ParseMaterialRatio(machineInfo.InputRatio);

            if (requiredMaterials == null)
            {
                continue;
            }

            if (machineInfo.Price <= 0) continue;

            decimal totalMaterialCost = 0;
            bool canFulfillAllMaterials = true;

            foreach (var (materialName, ratio) in requiredMaterials)
            {
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

    private Dictionary<string, int>? ParseMaterialRatio(MachineInputRatioDto ratioDto)
    {
        var requiredMaterials = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (ratioDto.Copper.HasValue && ratioDto.Copper > 0) requiredMaterials.Add("copper", ratioDto.Copper.Value);
        if (ratioDto.Silicon.HasValue && ratioDto.Silicon > 0) requiredMaterials.Add("silicon", ratioDto.Silicon.Value);


        if ((ratioDto.Plastic.HasValue && ratioDto.Plastic > 0) || (ratioDto.Gold.HasValue && ratioDto.Gold > 0))
        {
            return null;
        }

        foreach (var materialName in requiredMaterials.Keys)
        {
            if (!_ourCoreMaterials.Contains(materialName))
            {
                return null;
            }
        }

        return requiredMaterials;

    }
}