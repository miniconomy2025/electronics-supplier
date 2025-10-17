using System.Collections.Generic;
using System.Threading.Tasks;
using esAPI.DTOs;

namespace esAPI.Interfaces
{
    public interface IThohMachineApiClient
    {
        Task<List<ThohMachineInfo>> GetAvailableMachinesAsync();
        Task<ThohMachinePurchaseResponse?> PurchaseMachineAsync(ThohMachinePurchaseRequest request);
    }
}


