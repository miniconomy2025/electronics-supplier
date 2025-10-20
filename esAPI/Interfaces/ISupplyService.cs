using esAPI.DTOs.Supply;

namespace esAPI.Interfaces
{
    public interface ISupplyService
    {
        Task<IEnumerable<SupplyDto>> GetAllSuppliesAsync();
        Task<SupplyDto?> GetSupplyByIdAsync(int id);
        Task<SupplyDto> CreateSupplyAsync(CreateSupplyDto dto);
        Task<bool> DeleteSupplyByIdAsync(int id);
    }
}
