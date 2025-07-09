using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using esAPI.DTOs;

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

            // storing financial state
            await StoreFinancialState(dayNumber);

            var inventory = await _inventoryService.GetAndStoreInventory();

            // checking our current machine inventory, if we need more machines, purchase through bank
            if (NeedToBuyMachine(inventory))
                await TryAcquireMachine();

            // logistics will then call our logistics endpoint when delivering it, this will then fill our machine table

            // checking our current material inventory, if we need more mats, purchase through bank
            if (NeedToRestockMaterials(inventory))
                await _materialAcquisitionService.PurchaseMaterialsViaBank();

            // logistics will then call our logistics endpoint when delivering it, this will then fill our supply table

            // Given that we have machines, and a good amount of stock, it will automatically create electronics when supplies are added
            await ProduceElectronics();

            _logger.LogInformation($"--- Simulation Day {dayNumber} End ---");
        }

        private async Task StoreFinancialState(int dayNumber)
        {
            var bankBalance = await _bankService.GetAndStoreBalance(dayNumber);
            decimal spendingCap = bankBalance * 0.8m;
            _logger.LogInformation($"Bank Balance: {bankBalance}, Spending Cap: {spendingCap}");
        }

        private bool NeedToBuyMachine(InventorySummaryDto inventory)
        {
            return inventory.Machines.InUse == 0;
        }

        private async Task TryAcquireMachine()
        {
            if (!await _machineAcquisitionService.CheckTHOHForMachines())
            {
                _logger.LogInformation("No machines available at THOH.");
                return;
            }

            var (orderId, quantity) = await _machineAcquisitionService.PurchaseMachineViaBank();
            await _machineAcquisitionService.QueryOrderDetailsFromTHOH();

            if (orderId.HasValue && quantity > 0)
            {
                await _machineAcquisitionService.PlaceBulkLogisticsPickup(orderId.Value, quantity);
                _logger.LogInformation($"Ordered {quantity} machines (Order ID: {orderId}).");
            }
        }

        private bool NeedToRestockMaterials(InventorySummaryDto inventory)
        {
            bool HasMaterial(string name) =>
                inventory.MaterialsInStock.Any(m => m.MaterialName.Equals(name, StringComparison.OrdinalIgnoreCase) && m.Quantity > 0);

            return !HasMaterial("copper") || !HasMaterial("silicon");
        }

        private async Task ProduceElectronics()
        {
            var (created, materialsUsed) = await _productionService.ProduceElectronics();
            var used = string.Join(", ", materialsUsed.Select(kv => $"{kv.Key}: {kv.Value}"));
            _logger.LogInformation($"Produced {created} electronics. Materials used: {used}");
        }

        // public async Task RunDayAsync(int dayNumber)
        // {
        //     _logger.LogInformation($"--- Simulation Day {dayNumber} Start ---");

        //     // 1. Check and store bank balance
        //     var bankBalance = await _bankService.GetAndStoreBalance(dayNumber);
        //     decimal spendingCap = bankBalance * 0.8m;
        //     _logger.LogInformation($"Bank Balance: {bankBalance}, Spending Cap: {spendingCap}");

        //     // 2. Check and store inventory
        //     var inventory = await _inventoryService.GetAndStoreInventory();
        //     var machinesInUse = inventory.Machines.InUse;

        //     // 3. If no working machines, try to buy one
        //     if (inventory.Machines.InUse == 0)
        //     {
        //         var machineAvailable = await _machineAcquisitionService.CheckTHOHForMachines();
        //         if (machineAvailable)
        //         {
        //             var (orderId, quantity) = await _machineAcquisitionService.PurchaseMachineViaBank();
        //             await _machineAcquisitionService.QueryOrderDetailsFromTHOH();
        //             if (orderId.HasValue && quantity > 0)
        //             {
        //                 await _machineAcquisitionService.PlaceBulkLogisticsPickup(orderId.Value, quantity);
        //             }
        //         }
        //     }

        //     // 4. Check raw materials
        //     bool hasCopper = inventory.MaterialsInStock.Any(m => m.MaterialName.ToLower() == "copper" && m.Quantity > 0);
        //     bool hasSilicon = inventory.MaterialsInStock.Any(m => m.MaterialName.ToLower() == "silicon" && m.Quantity > 0);
        //     if (!hasCopper || !hasSilicon)
        //     {
        //         await _materialAcquisitionService.PurchaseMaterialsViaBank();
        //         // await _materialAcquisitionService.PlaceBulkLogisticsPickup(); // No longer needed, handled inside service
        //     }

        //     // 5. Bulk logistics delivery is handled by /logistics endpoint

        //     // 6. Produce electronics
        //     var (created, materialsUsed) = await _productionService.ProduceElectronics();
        //     _logger.LogInformation($"Produced {created} electronics. Materials used: {string.Join(", ", materialsUsed.Select(kv => $"{kv.Key}: {kv.Value}"))}");

        //     _logger.LogInformation($"--- Simulation Day {dayNumber} End ---");
        // }
    }
} 