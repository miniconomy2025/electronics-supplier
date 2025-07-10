using esAPI.DTOs;

namespace esAPI.Clients;

public class RecyclerApiClient : BaseClient, ISupplierApiClient
{
    private const string ClientName = "recycler";

    public RecyclerApiClient(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, ClientName) { }

    public async Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync()
    {
        var recyclerResponse = await GetAsync<RecyclerApiResponseDto>("/raw-materials");
        if (recyclerResponse == null || recyclerResponse.Materials == null)
        {
            return new List<SupplierMaterialInfo>();
        }

    
        var standardizedList = recyclerResponse.Materials.Select(recyclerMaterial =>
            new SupplierMaterialInfo
            {
                MaterialName  = recyclerMaterial.Name,
                AvailableQuantity  = recyclerMaterial.AvailableQuantityInKg,
                PricePerKg = recyclerMaterial.Price
            }).ToList();

        return standardizedList;
    }

    public async Task<SupplierOrderResponse?> PlaceOrderAsync(SupplierOrderRequest request)
    {
        return await PostAsync<SupplierOrderRequest, SupplierOrderResponse>("/raw-materials", request);
    }
}