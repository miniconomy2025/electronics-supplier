using esAPI.Clients;
using esAPI.Interfaces;

namespace esAPI.Services;

public class MaterialSourcingService : IMaterialSourcingService
{
    private readonly IEnumerable<ISupplierApiClient> _supplierClients;
    private readonly ILogger<MaterialSourcingService> _logger;


    public MaterialSourcingService(
        IEnumerable<ISupplierApiClient> supplierClients,
        ILogger<MaterialSourcingService> logger)
    {
        _supplierClients = supplierClients;
        _logger = logger;
    }

    public async Task<SourcedSupplier?> FindBestSupplierAsync(string materialName)
    {

        var quotes = new List<SourcedSupplier>();

        foreach (var client in _supplierClients)
        {

            try
            {
                var materials = await client.GetAvailableMaterialsAsync();
                var materialInfo = materials?.FirstOrDefault(m => m.MaterialName.Equals(materialName, StringComparison.OrdinalIgnoreCase));

                if (materialInfo != null && materialInfo.AvailableQuantity > 0)
                {
                    var clientName = client.GetType().Name.Replace("ApiClient", "").ToLowerInvariant();
                    quotes.Add(new SourcedSupplier(clientName, client, materialInfo));
                }
            }
            catch (Exception ex)
            {
                var clientName = client.GetType().Name;
                _logger.LogError(ex, "Failed to get material data from supplier client '{ClientName}'.", clientName);
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