using esAPI.DTOs;
using esAPI.Exceptions;
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
        try
        {
            var recyclerResponse = await GetAsync<RecyclerApiResponseDto>("/materials");

            if (recyclerResponse.Materials == null)
            {
                throw new ApiResponseParseException("Recycler API response was successful but the 'materials' array was missing.");
            }

            return [.. recyclerResponse.Materials.Select(recyclerDto =>
                new SupplierMaterialInfo
                {
                    MaterialName = recyclerDto.Name,
                    AvailableQuantity = recyclerDto.AvailableQuantityInKg,
                    PricePerKg = recyclerDto.Price
                })];
        }
        catch (Exception ex) when (ex is not ApiClientException and not ApiResponseParseException)
        {
            throw; // Re-throw to allow the calling service to handle it.
        }
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