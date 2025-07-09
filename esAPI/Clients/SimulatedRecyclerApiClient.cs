using System.Text.Json;
using esAPI.DTOs;
using esAPI.Interfaces;

namespace esAPI.Clients;

public class SimulatedRecyclerApiClient(ILogger<SimulatedRecyclerApiClient> logger) : ISupplierApiClient
{
    private readonly ILogger<SimulatedRecyclerApiClient> _logger = logger;

    public Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync()
    {
        _logger.LogInformation("SIMULATION: Calling Recycler's materials endpoint...");

        // 1. Simulate the raw JSON response from the Recycler.
        // The Recycler has cheap Copper (ID 1) but only 20kg of it. It has no Silicon.
        var jsonResponse = """
        {
          "materials": [
            {
              "id": 1,
              "name": "copper",
              "available_quantity_in_kg": 20,
              "price": 9.75
            }
          ]
        }
        """;

        // 2. Deserialize the JSON into our specific DTOs.
        var recyclerData = JsonSerializer.Deserialize<RecyclerApiResponseDto>(jsonResponse);

        // 3. Transform the supplier-specific DTO into our standard internal model.
        var standardizedInfo = recyclerData?.Materials
            .Select(m => new SupplierMaterialInfo
            {
                MaterialId = m.Id,
                AvailableStock = m.AvailableQuantityInKg,
                Price = m.Price
            })
            .ToList() ?? [];

        return Task.FromResult(standardizedInfo);
    }

}