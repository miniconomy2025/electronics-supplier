namespace esAPI.Interfaces.Services
{
    public interface IMaterialAcquisitionService
    {
        Task ExecutePurchaseStrategyAsync();
        // Task PurchaseMaterialsViaBank();
        // Task PlaceBulkLogisticsPickup(int orderId, string itemName, int quantity, string supplier);
    }
}
