using System.Threading.Tasks;
using esAPI.DTOs;

namespace esAPI.Interfaces
{
    public interface IBulkLogisticsClient
    {
        Task<LogisticsPickupResponse?> ArrangePickupAsync(LogisticsPickupRequest request);
    }
}


