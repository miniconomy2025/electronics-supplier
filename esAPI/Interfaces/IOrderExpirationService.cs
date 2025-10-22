namespace esAPI.Interfaces
{
    public interface IOrderExpirationService
    {
        Task<bool> ReserveElectronicsForOrderAsync(int orderId, int quantity);
        Task<int> FreeReservedElectronicsAsync(int orderId);
        Task<int> CheckAndExpireOrdersAsync();
        Task<int> GetAvailableElectronicsCountAsync();
        Task<int> GetReservedElectronicsCountAsync();
    }
}