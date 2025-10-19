using System.Threading.Tasks;
using esAPI.DTOs;

namespace esAPI.Interfaces
{
    public interface IInventoryService
    {
        Task<InventorySummaryDto> GetAndStoreInventory();
    }
}


