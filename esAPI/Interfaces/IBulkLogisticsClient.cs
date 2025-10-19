using System.Threading.Tasks;
using esAPI.DTOs;

namespace esAPI.Interfaces
{
    public interface IBulkLogisticsClient
    {
        Task<LogisticsPickupResponse?> ArrangePickupAsync(LogisticsPickupRequest request);
        Task<LogisticsPickupDetailsResponse?> GetPickupRequestAsync(int pickupRequestId);
        Task<List<LogisticsPickupDetailsResponse>> GetCompanyPickupRequestsAsync(string companyName);
    }
}


