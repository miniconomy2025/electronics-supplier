using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using esAPI.DTOs;

namespace esAPI.Services
{
    public class InventoryService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public InventoryService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<InventorySummaryDto> GetAndStoreInventory()
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