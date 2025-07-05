using System.Threading.Tasks;
using esAPI.Dtos.ElectronicsDtos;

namespace esAPI.Services
{
    public interface IElectronicsService
    {
        Task<ElectronicsDetailsDto?> GetElectronicsDetailsAsync();
        Task<ProducedElectronicsResultDto> ProduceElectronicsAsync(int? machineId, string? notes);
    }
} 