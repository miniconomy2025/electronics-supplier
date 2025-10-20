namespace esAPI.Interfaces.Services
{
    public interface IMachineAcquisitionService
    {
        Task<bool> CheckTHOHForMachines();
        Task<(int? orderId, int quantity)> PurchaseMachineViaBank();
        Task QueryOrderDetailsFromTHOH();
        Task PlaceBulkLogisticsPickup(int thohOrderId, int quantity);
    }
}
