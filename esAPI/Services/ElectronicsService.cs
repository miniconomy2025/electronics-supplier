using System.Threading.Tasks;
using esAPI.DTOs.Electronics;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;
using esAPI.Services;

namespace esAPI.Services
{
    public class ElectronicsService(AppDbContext context, SimulationStateService stateService) : IElectronicsService
    {
        private readonly AppDbContext _context = context;
        private readonly SimulationStateService _stateService = stateService;

        public async Task<ElectronicsDetailsDto?> GetElectronicsDetailsAsync()
        {
            var result = await _context.Database.SqlQueryRaw<ElectronicsDetailsDto>(
                "SELECT \"availableStock\" AS AvailableStock, \"pricePerUnit\" AS PricePerUnit FROM available_electronics_stock")
                .ToListAsync();
            
            return result.FirstOrDefault();
        }

        public async Task<ProducedElectronicsResultDto> ProduceElectronicsAsync()
        {
            // Get current simulation day
            var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
            if (sim == null)
                throw new InvalidOperationException("Simulation not running.");

            var result = await _context.Database.SqlQueryRaw<ProduceElectronicsResult>(
                "SELECT electronics_created AS ElectronicsCreated, copper_used AS CopperUsed, silicone_used AS SiliconeUsed FROM produce_electronics()")
                .ToListAsync();
            var dto = new ProducedElectronicsResultDto();
            if (result.Count > 0)
            {
                dto.ElectronicsCreated = result[0].ElectronicsCreated;
                dto.MaterialsUsed["copper"] = result[0].CopperUsed;
                dto.MaterialsUsed["silicone"] = result[0].SiliconeUsed;

                // Set produced_at for new electronics to the current simulation day
                var newElectronics = _context.Electronics
                    .OrderByDescending(e => e.ElectronicId)
                    .Take(dto.ElectronicsCreated)
                    .ToList();
                foreach (var e in newElectronics)
                {
                    e.ProducedAt = _stateService.GetCurrentSimulationTime(3);
                }
                await _context.SaveChangesAsync();
            }
            return dto;
        }

        private class ProduceElectronicsResult
        {
            public int ElectronicsCreated { get; set; }
            public int CopperUsed { get; set; }
            public int SiliconeUsed { get; set; }
        }
    }
}