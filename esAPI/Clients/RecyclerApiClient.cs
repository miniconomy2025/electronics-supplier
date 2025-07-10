using esAPI.DTOs;
using esAPI.Interfaces;

namespace esAPI.Clients;

public class RecyclerApiClient : BaseClient, ISupplierApiClient
{
    private const string ClientName = "recycler";
    private const string OurCompanyName = "electronics-supplier";

    public RecyclerApiClient(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, ClientName) { }

    public async Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync()
    {
        var recyclerResponse = await GetAsync<RecyclerApiResponseDto>("/materials");
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

        var recyclerRequest = new RecyclerOrderRequestDto
        {
            CompanyName = OurCompanyName,
            OrderItems = new List<RecyclerOrderItemDto>
            {
                new RecyclerOrderItemDto
                {
                    RawMaterialName = request.MaterialName,
                    QuantityInKg = request.WeightQuantity
                }
            }
        };

        return await PostAsync<RecyclerOrderRequestDto, SupplierOrderResponse>("/orders", recyclerRequest);
    }
}