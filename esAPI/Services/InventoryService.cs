using System.Text.Json;
using esAPI.DTOs;
using esAPI.Interfaces;

namespace esAPI.Services
{
    public class InventoryService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IInventoryService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly string _selfBaseUrl = configuration.GetValue<string>("SelfApi:BaseUrl") ?? "http://localhost:5062";

        public Task<InventorySummaryDto> GetAndStoreInventory()
        {
            return GetAndStoreInventoryImpl();
        }

        private async Task<InventorySummaryDto> GetAndStoreInventoryImpl()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_selfBaseUrl);
            var response = await client.GetAsync("/inventory");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var summary = JsonSerializer.Deserialize<InventorySummaryDto>(content);
            return summary!;
        }
    }
}