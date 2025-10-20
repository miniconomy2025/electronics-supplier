using esAPI.Clients;
using esAPI.Interfaces;

namespace esAPI.Services;

public class MaterialSourcingService : IMaterialSourcingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IThohApiClient _thohClient;
    private readonly IRecyclerApiClient _recyclerClient;


    public MaterialSourcingService(IHttpClientFactory httpClientFactory, IThohApiClient thohClient, IRecyclerApiClient recyclerClient)
    {
        _httpClientFactory = httpClientFactory;
        _thohClient = thohClient;
        _recyclerClient = recyclerClient;
    }

    public async Task<SourcedSupplier?> FindBestSupplierAsync(string materialName)
    {
        var quotes = new List<SourcedSupplier>();
        // Query THOH
        var thohMaterials = await _thohClient.GetAvailableMaterialsAsync();
        var thohMaterial = thohMaterials?.FirstOrDefault(m => m.MaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase));
        if (thohMaterial != null && thohMaterial.AvailableQuantity > 0)
        {
            quotes.Add(new SourcedSupplier("thoh", (object)_thohClient, thohMaterial));
        }
        // Query Recycler
        var recyclerMaterials = await _recyclerClient.GetAvailableMaterialsAsync();
        var recyclerMaterial = recyclerMaterials?.FirstOrDefault(m => m.MaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase));
        if (recyclerMaterial != null && recyclerMaterial.AvailableQuantity > 0)
        {
            quotes.Add(new SourcedSupplier("recycler", (object)_recyclerClient, recyclerMaterial));
        }
        if (!quotes.Any())
        {
            return null;
        }
        var bestQuote = quotes.OrderBy(q => q.MaterialDetails.PricePerKg).First();
        return bestQuote;
    }
}