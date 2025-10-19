using esAPI.Interfaces;

namespace esAPI.Services;

public class MaterialSourcingService : IMaterialSourcingService
{
    private readonly IEnumerable<ISupplierApiClient> _supplierClients;


    public MaterialSourcingService(IEnumerable<ISupplierApiClient> supplierClients)
    {
        _supplierClients = supplierClients;
    }

    public async Task<SourcedSupplier?> FindBestSupplierAsync(string materialName)
    {

        var quotes = new List<SourcedSupplier>();

        foreach (var client in _supplierClients)
        {
            var materials = await client.GetAvailableMaterialsAsync();
            var materialInfo = materials?.FirstOrDefault(m => m.MaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase));
            if (materialInfo != null && materialInfo.AvailableQuantity > 0)
            {
                // Use client type name as supplier name by default
                var supplierName = client.GetType().Name.Replace("ApiClient", string.Empty);
                quotes.Add(new SourcedSupplier(supplierName, client, materialInfo));
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