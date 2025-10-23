using System.Text.Json.Serialization;
using System.Text.Json;
using esAPI.DTOs;
using esAPI.Interfaces;
using esAPI.DTOs.Thoh;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace esAPI.Clients
{
    public class ThohApiClient : IThohApiClient
    {
        private readonly HttpClient _client;
        public ThohApiClient(IHttpClientFactory factory)
        {
            _client = factory.CreateClient("thoh");
        }

        public async Task<ThohMachineDto?> GetElectronicsMachineAsync()
        {
            var response = await _client.GetFromJsonAsync<ThohMachinesResponse>("machines");
            return response?.Machines?.FirstOrDefault(m => m.MachineName == "electronics_machine");
        }

        public async Task<List<ThohMachineDto>> GetAvailableMachinesAsync()
        {
            var response = await _client.GetFromJsonAsync<ThohMachinesResponse>("machines");
            return response?.Machines ?? new List<ThohMachineDto>();
        }

        public async Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync()
        {
            try
            {
                var response = await _client.GetFromJsonAsync<List<ThohRawMaterialDto>>("raw-materials");
                var result = new List<SupplierMaterialInfo>();

                if (response == null) return result;

                foreach (var material in response)
                {
                    result.Add(new SupplierMaterialInfo
                    {
                        MaterialName = material.RawMaterialName,
                        AvailableQuantity = material.QuantityAvailable,
                        PricePerKg = material.PricePerKg
                    });
                }

                return result;
            }
            catch (Exception)
            {
                // Return empty list if API call fails
                return new List<SupplierMaterialInfo>();
            }
        }

        // Place a material order with THOH using their raw-materials endpoint
        public async Task<SupplierOrderResponse?> PlaceOrderAsync(SupplierOrderRequest request)
        {
            try
            {
                var orderRequest = new
                {
                    materialName = request.MaterialName,
                    weightQuantity = request.WeightQuantity
                };

                var response = await _client.PostAsJsonAsync("raw-materials", orderRequest);

                if (!response.IsSuccessStatusCode)
                {
                    return null; // Failed to place order, will fallback to Recycler
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var order = doc.RootElement;

                return new SupplierOrderResponse
                {
                    OrderId = order.GetProperty("orderId").GetInt32(),
                    Price = order.GetProperty("price").GetDecimal(),
                    BankAccount = order.GetProperty("bankAccount").GetString() ?? string.Empty
                };
            }
            catch (Exception)
            {
                // Return null if any error occurs, allowing fallback to Recycler
                return null;
            }
        }
    }
}
