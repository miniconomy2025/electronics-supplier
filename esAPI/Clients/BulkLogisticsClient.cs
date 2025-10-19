using esAPI.Clients;
using esAPI.DTOs;
using esAPI.Interfaces;

namespace esAPI.Clients;

public class BulkLogisticsClient : BaseClient, IBulkLogisticsClient
{
    private const string ClientName = "bulk-logistics";

    public BulkLogisticsClient(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, ClientName) { }

    public async Task<LogisticsPickupResponse?> ArrangePickupAsync(LogisticsPickupRequest request)
    {
        return await PostAsync<LogisticsPickupRequest, LogisticsPickupResponse>("/api/pickup-request", request);
    }

    public async Task<LogisticsPickupDetailsResponse?> GetPickupRequestAsync(int pickupRequestId)
    {
        return await GetAsync<LogisticsPickupDetailsResponse>($"/api/pickup-request/{pickupRequestId}");
    }

    public async Task<List<LogisticsPickupDetailsResponse>> GetCompanyPickupRequestsAsync(string companyName)
    {
        var response = await GetAsync<LogisticsPickupDetailsResponse[]>($"/api/pickup-request/company/{companyName}");
        return response?.ToList() ?? new List<LogisticsPickupDetailsResponse>();
    }
}