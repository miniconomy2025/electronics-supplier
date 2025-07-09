using esAPI.DTOs.Electronics;

namespace esAPI.Services
{
    public interface IElectronicsService
    {
        Task<ElectronicsDetailsDto?> GetElectronicsDetailsAsync();
        Task<ProducedElectronicsResultDto> ProduceElectronicsAsync();
    }
} 