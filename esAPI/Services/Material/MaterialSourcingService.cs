using esAPI.Clients;
using esAPI.Interfaces;

namespace esAPI.Services;

public class MaterialSourcingService : IMaterialSourcingService
{
    private readonly IHttpClientFactory _httpClientFactory;


    public MaterialSourcingService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SourcedSupplier?> FindBestSupplierAsync(string materialName)
    {

        var suppliersToQuery = new Dictionary<string, Func<ISupplierApiClient>>
        {

            { "thoh", () => new ThohApiClient(_httpClientFactory) },
            { "recycler", () => new RecyclerApiClient(_httpClientFactory) }
        };

        var quotes = new List<SourcedSupplier>();

        foreach (var (name, clientFactory) in suppliersToQuery)
        {
            var client = clientFactory();
            var materials = await client.GetAvailableMaterialsAsync();
            var materialInfo = materials?.FirstOrDefault(m => m.MaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase));

            if (materialInfo != null && materialInfo.AvailableQuantity > 0)
            {
                quotes.Add(new SourcedSupplier(name, client, materialInfo));
            }
        }

        if (!quotes.Any())
        {
            return null;
        }

        var bestQuote = quotes.OrderBy(q => q.MaterialDetails.PricePerKg).First();

        return bestQuote;
    }
}