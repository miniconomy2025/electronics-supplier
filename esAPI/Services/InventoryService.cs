using System.Text.Json;
using esAPI.DTOs;

namespace esAPI.Services
{
    public interface IInventoryService
    {
        Task<InventorySummaryDto> GetAndStoreInventory();
    }

    public class InventoryService(IHttpClientFactory httpClientFactory) : IInventoryService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        public Task<InventorySummaryDto> GetAndStoreInventory()
        {
            return GetAndStoreInventoryImpl();
        }

        private async Task<InventorySummaryDto> GetAndStoreInventoryImpl()
        {
            var client = _httpClientFactory.CreateClient(); // Default client, since it's our own API
            var response = await client.GetAsync("/inventory");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var summary = JsonSerializer.Deserialize<InventorySummaryDto>(content);
            return summary!;
        }
    }
}