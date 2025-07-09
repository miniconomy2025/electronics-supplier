using esAPI.DTOs.Electronics;

namespace esAPI.Interfaces
{
    public interface IElectronicsService
    {
        Task<ElectronicsDetailsDto?> GetElectronicsDetailsAsync();
        Task<ProducedElectronicsResultDto> ProduceElectronicsAsync();
    }
} 