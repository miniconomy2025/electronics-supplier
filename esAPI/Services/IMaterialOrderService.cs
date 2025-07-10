using System.Collections.Generic;
using System.Threading.Tasks;
using esAPI.DTOs.MaterialOrder;

namespace esAPI.Services
{
    public interface IMaterialOrderService
    {
        Task<IEnumerable<MaterialOrderResponse>> GetAllMaterialOrdersAsync();
        Task<MaterialOrderResponse?> GetMaterialOrderByIdAsync(int orderId);
        Task<MaterialOrderResponse> CreateMaterialOrderAsync(CreateMaterialOrderRequest request);
        Task<bool> UpdateMaterialOrderAsync(int orderId, UpdateMaterialOrderRequest request);
    }
} 