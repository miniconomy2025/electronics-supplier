using esAPI.Clients;
using esAPI.DTOs;

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
}