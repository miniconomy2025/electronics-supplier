using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace esAPI.Services
{
    public class SimulationDayOrchestrator(
        BankService bankService,
        IInventoryService inventoryService,
        IMachineAcquisitionService machineAcquisitionService,
        IMaterialAcquisitionService materialAcquisitionService,
        IProductionService productionService,
        ILogger<SimulationDayOrchestrator> logger)
    {
        private readonly BankService _bankService = bankService;
        private readonly IInventoryService _inventoryService = inventoryService;
        private readonly IMachineAcquisitionService _machineAcquisitionService = machineAcquisitionService;
        private readonly IMaterialAcquisitionService _materialAcquisitionService = materialAcquisitionService;
        private readonly IProductionService _productionService = productionService;
        private readonly ILogger<SimulationDayOrchestrator> _logger = logger;

        public async Task RunDayAsync(int dayNumber)
        {
            _logger.LogInformation($"--- Simulation Day {dayNumber} Start ---");

            // 1. Check and store bank balance
            await _bankService.GetAndStoreBalance(dayNumber);

            // 2. Check and store inventory
            var inventory = await _inventoryService.GetAndStoreInventory();

            // 3. If no working machines, try to buy one
            // TODO: Limit total amount of machines we own
            if (inventory.Machines.InUse == 0) // Total = Broken
            {
                var machineAvailable = await _machineAcquisitionService.CheckTHOHForMachines();
                if (machineAvailable)
                {
                    var (orderId, quantity) = await _machineAcquisitionService.PurchaseMachineViaBank();
                    await _machineAcquisitionService.QueryOrderDetailsFromTHOH();
                    if (orderId.HasValue && quantity > 0)
                    {
                        await _machineAcquisitionService.PlaceBulkLogisticsPickup(orderId.Value, quantity);
                    }
                }
            }

            // 4. Check raw materials
            bool hasCopper = inventory.MaterialsInStock.Any(m => m.MaterialName.Equals("copper", StringComparison.CurrentCultureIgnoreCase) && m.Quantity > 0);
            bool hasSilicon = inventory.MaterialsInStock.Any(m => m.MaterialName.Equals("silicon", StringComparison.CurrentCultureIgnoreCase) && m.Quantity > 0);
            if (!hasCopper || !hasSilicon)
            {
                await _materialAcquisitionService.PurchaseMaterialsViaBank();
            }

            // 5. Bulk logistics delivery is handled by /logistics endpoint

            // 6. Produce electronics
            var (created, materialsUsed) = await _productionService.ProduceElectronics();
            _logger.LogInformation($"Produced {created} electronics. Materials used: {string.Join(", ", materialsUsed.Select(kv => $"{kv.Key}: {kv.Value}"))}");

            _logger.LogInformation($"--- Simulation Day {dayNumber} End ---");
        }
    }
} 