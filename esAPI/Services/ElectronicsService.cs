using System.Threading.Tasks;
using esAPI.Dtos.ElectronicsDtos;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Services
{
    public class ElectronicsService : IElectronicsService
    {
        private readonly AppDbContext _context;

        public ElectronicsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ElectronicsDetailsDto?> GetElectronicsDetailsAsync()
        {
            return await _context.Set<ElectronicsDetailsDto>()
                .FromSqlRaw("SELECT * FROM available_electronics_stock")
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<ProducedElectronicsResultDto> ProduceElectronicsAsync(int? machineId, string? notes)
        {
            var result = await _context.Database.SqlQueryRaw<ProduceElectronicsResult>(
                "CALL produce_electronics()")
                .ToListAsync();
            var dto = new ProducedElectronicsResultDto();
            if (result.Count > 0)
            {
                dto.ElectronicsCreated = result[0].ElectronicsCreated;
                dto.MaterialsUsed["copper"] = result[0].CopperUsed;
                dto.MaterialsUsed["silicone"] = result[0].SiliconeUsed;
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