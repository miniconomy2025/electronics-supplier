using esAPI.Clients;

namespace FactoryApi.Clients;

public class SupplierApiClientFactory(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public ISupplierApiClient GetClient(string supplierName)
    {
        // Use the supplier's name to determine which client implementation to return.
        // This is more robust than relying on hard-coded IDs.
        return supplierName switch
        {
            "recycler" => _serviceProvider.GetRequiredService<SimulatedRecyclerApiClient>(),
            "thoh" => _serviceProvider.GetRequiredService<SimulatedThohApiClient>(),
            _ => throw new NotSupportedException($"No API client implementation found for supplier: {supplierName}")
        };
    }
}