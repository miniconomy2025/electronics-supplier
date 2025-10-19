using esAPI.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace esAPI.Interfaces;

public interface
ISupplierApiClient
{
    Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync();
    Task<SupplierOrderResponse?> PlaceOrderAsync(SupplierOrderRequest request);

}
