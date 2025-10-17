using esAPI.DTOs;

namespace esAPI.Interfaces;

public interface
ISupplierApiClient
{
    Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync();
    Task<SupplierOrderResponse?> PlaceOrderAsync(SupplierOrderRequest request);

}

// moved to IBulkLogisticsClient.cs