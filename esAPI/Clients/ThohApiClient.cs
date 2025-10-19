using System.Text.Json.Serialization;
using esAPI.DTOs;
using esAPI.Interfaces;

namespace esAPI.Clients;

public class ThohApiClient(IHttpClientFactory httpClientFactory) : BaseClient(httpClientFactory, ClientName), ISupplierApiClient, IThohMachineApiClient
{
    private const string ClientName = "thoh";

    public async Task<List<ThohMachineInfo>> GetAvailableMachinesAsync()
    {
        var response = await GetAsync<ThohMachineListResponse>("/machines");
        
        return response?.Machines ?? new List<ThohMachineInfo>();
    }

    public async Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync()
    {
        var response = await GetAsync<List<ThohMaterialInfo>>("/raw-materials");
        if (response == null) return new List<SupplierMaterialInfo>();
        var standardizedList = response.Select(thohDto =>
           new SupplierMaterialInfo
           {
               MaterialName = thohDto.RawMaterialName,
               AvailableQuantity = thohDto.QuantityAvailable,
               PricePerKg = thohDto.PricePerKg
           }).ToList();

        return standardizedList;
    }

    public async Task<SupplierOrderResponse?> PlaceOrderAsync(SupplierOrderRequest request)
    {
        return await PostAsync<SupplierOrderRequest, SupplierOrderResponse>("/raw-materials", request);
    }

    public async Task<ThohMachinePurchaseResponse?> PurchaseMachineAsync(ThohMachinePurchaseRequest request)
    {
        return await PostAsync<ThohMachinePurchaseRequest, ThohMachinePurchaseResponse>("/machines", request);
    }
}