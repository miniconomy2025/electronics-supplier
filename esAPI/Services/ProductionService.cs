using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace esAPI.Services
{
    public class ProductionService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductionService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(int electronicsCreated, Dictionary<string, int> materialsUsed)> ProduceElectronics()
        {
            var client = _httpClientFactory.CreateClient(); // Default client, since it's our own API
            var response = await client.PostAsync("/electronics", null);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            int created = root.GetProperty("electronicsCreated").GetInt32();
            var materialsUsed = new Dictionary<string, int>();
            if (root.TryGetProperty("materialsUsed", out var matProp) && matProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in matProp.EnumerateObject())
                {
                    materialsUsed[prop.Name] = prop.Value.GetInt32();
                }
            }
            return (created, materialsUsed);
        }
    }
} 