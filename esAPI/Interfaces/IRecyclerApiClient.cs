using esAPI.DTOs;
using esAPI.Clients;
using Microsoft.Extensions.Logging;

namespace esAPI.Interfaces
{
    /// <summary>
    /// Interface for Recycler API client operations, extends the general supplier interface
    /// </summary>
    public interface IRecyclerApiClient : ISupplierApiClient
    {
        /// <summary>
        /// Places a specific recycler order with native recycler response format
        /// </summary>
        /// <param name="materialName">Name of the material to order</param>
        /// <param name="quantityInKg">Quantity in kilograms</param>
        /// <returns>Recycler-specific order response wrapper</returns>
        Task<RecyclerOrderResponseWrapper?> PlaceRecyclerOrderAsync(string materialName, int quantityInKg);

        /// <summary>
        /// Orders and pays for half the available stock of all materials
        /// </summary>
        /// <param name="bankClient">Bank client for payment processing</param>
        /// <param name="logger">Logger for operation tracking</param>
        /// <returns>List of actions taken</returns>
        Task<List<string>> OrderAndPayForHalfStockAsync(ICommercialBankClient bankClient, ILogger logger);

        /// <summary>
        /// Places orders for half stock without payment processing
        /// </summary>
        /// <returns>List of payment information for placed orders</returns>
        Task<List<RecyclerOrderPaymentInfo>> PlaceHalfStockOrdersAsync();
    }
}