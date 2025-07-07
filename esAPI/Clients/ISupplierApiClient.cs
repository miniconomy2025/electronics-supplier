using esAPI.DTOs;

namespace esAPI.Clients;

public interface ISupplierApiClient
{
    Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync();
}