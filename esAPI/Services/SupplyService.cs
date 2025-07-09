using esAPI.Data;
using esAPI.DTOs.Supply;
using esAPI.Models;
using esAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Services
{
    public class SupplyService(AppDbContext context, ISimulationStateService stateService) : ISupplyService
    {
        private readonly AppDbContext _context = context;
        private readonly ISimulationStateService _stateService = stateService;

        public async Task<IEnumerable<SupplyDto>> GetAllSuppliesAsync()
        {
            return await _context.MaterialSupplies
                .Include(s => s.Material)
                .Select(s => new SupplyDto
                {
                    SupplyId = s.SupplyId,
                    ReceivedAt = s.ReceivedAt,
                    ProcessedAt = s.ProcessedAt,
                    MaterialId = s.MaterialId,
                    MaterialName = s.Material != null ? s.Material.MaterialName : null
                })
                .ToListAsync();
        }

        public async Task<SupplyDto?> GetSupplyByIdAsync(int id)
        {
            var supply = await _context.MaterialSupplies
                .Include(s => s.Material)
                .FirstOrDefaultAsync(s => s.SupplyId == id);
            if (supply == null)
                return null;
            return new SupplyDto
            {
                SupplyId = supply.SupplyId,
                MaterialId = supply.MaterialId,
                ReceivedAt = supply.ReceivedAt,
                ProcessedAt = supply.ProcessedAt,
                MaterialName = supply.Material?.MaterialName
            };
        }

        public async Task<SupplyDto> CreateSupplyAsync(CreateSupplyDto dto)
        {
            var materialExists = await _context.Materials.AnyAsync(m => m.MaterialId == dto.MaterialId);
            if (!materialExists)
                throw new KeyNotFoundException($"Material with ID {dto.MaterialId} does not exist.");
            var supply = new MaterialSupply
            {
                MaterialId = dto.MaterialId,
                ReceivedAt = dto.ReceivedAt != 0 ? dto.ReceivedAt : _stateService.GetCurrentSimulationTime(3),
                ProcessedAt = dto.ProcessedAt ?? null
            };
            _context.MaterialSupplies.Add(supply);
            await _context.SaveChangesAsync();
            return await GetSupplyByIdAsync(supply.SupplyId) ?? throw new System.Exception("Failed to create supply.");
        }

        public async Task<bool> DeleteSupplyByIdAsync(int id)
        {
            var supply = await _context.MaterialSupplies.FindAsync(id);
            if (supply == null)
                return false;
            _context.MaterialSupplies.Remove(supply);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}