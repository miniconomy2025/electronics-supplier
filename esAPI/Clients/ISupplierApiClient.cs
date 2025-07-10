using esAPI.DTOs;

namespace esAPI.Clients;

public interface
ISupplierApiClient
{
    Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync();
    Task<SupplierOrderResponse?> PlaceOrderAsync(SupplierOrderRequest request);

}

public interface IBulkLogisticsClient
{
    Task<LogisticsPickupResponse?> ArrangePickupAsync(LogisticsPickupRequest request);
}