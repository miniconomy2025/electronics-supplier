using esAPI.DTOs;

namespace esAPI.Clients;

public class SimulatedThohApiClient : ISupplierApiClient
{
    public Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync()
    {
        // "The Hand" has a simpler, more direct supply.
        // It has plenty of both materials at a stable price.
        var materials = new List<SupplierMaterialInfo>
        {
            new() { MaterialId = 1, AvailableStock = 9999, Price = 10.50m }, // Copper
            new() { MaterialId = 2, AvailableStock = 9999, Price = 12.00m }  // Silicon
        };
        return Task.FromResult(materials);
    }
}