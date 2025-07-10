using esAPI.DTOs;

namespace esAPI.Interfaces;

public interface ISupplierApiClient
{
    Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync();
}