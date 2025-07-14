using System.Text.Json.Serialization;
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
    public class ThohApiClient
    {
        private readonly HttpClient _client;
        public ThohApiClient(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new System.Uri("https://thoh-api.projects.bbdgrad.com");
        }

        public async Task<ThohMachineDto?> GetElectronicsMachineAsync()
        {
            var response = await _client.GetFromJsonAsync<ThohMachinesResponse>("/machines");
            return response?.Machines?.FirstOrDefault(m => m.MachineName == "electronics_machine");
        }

        public async Task<List<ThohMachineDto>> GetAvailableMachinesAsync()
        {
            var response = await _client.GetFromJsonAsync<ThohMachinesResponse>("/machines");
            return response?.Machines ?? new List<ThohMachineDto>();
        }

        public async Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync()
        {
            var response = await _client.GetFromJsonAsync<ThohMachinesResponse>("/machines");
            var result = new List<SupplierMaterialInfo>();
            if (response?.Machines == null) return result;
            foreach (var machine in response.Machines)
            {
                if (machine.InputRatio != null)
                {
                    foreach (var input in machine.InputRatio)
                    {
                        // Use machine price as price per unit for now (or set to 0 if not applicable)
                        result.Add(new SupplierMaterialInfo
                        {
                            MaterialName = input.Key,
                            AvailableQuantity = machine.Quantity, // or another field if more appropriate
                            PricePerKg = machine.Price // This may need to be refined if price per kg is available elsewhere
                        });
                    }
                }
            }
            return result;
        }
    }
}