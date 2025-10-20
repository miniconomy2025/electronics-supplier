using esAPI.DTOs;
using esAPI.DTOs.Thoh;

namespace esAPI.Interfaces
{
    /// <summary>
    /// Interface for THOH API client operations
    /// </summary>
    public interface IThohApiClient
    {
        /// <summary>
        /// Gets the electronics machine information from THOH
        /// </summary>
        /// <returns>Electronics machine DTO or null if not found</returns>
        Task<ThohMachineDto?> GetElectronicsMachineAsync();

        /// <summary>
        /// Gets all available machines from THOH
        /// </summary>
        /// <returns>List of available machines</returns>
        Task<List<ThohMachineDto>> GetAvailableMachinesAsync();

        /// <summary>
        /// Gets available raw materials from THOH
        /// </summary>
        /// <returns>List of available materials with pricing</returns>
        Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync();

        /// <summary>
        /// Places a material order with THOH
        /// </summary>
        /// <param name="request">Order request details</param>
        /// <returns>Order response with order ID and payment details, or null if failed</returns>
        Task<SupplierOrderResponse?> PlaceOrderAsync(SupplierOrderRequest request);
    }
}
