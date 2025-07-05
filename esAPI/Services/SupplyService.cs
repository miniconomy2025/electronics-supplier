using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using esAPI.Data;
using esAPI.DTOs.Supply;
using esAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Services
{
    public class SupplyService : ISupplyService
    {
        private readonly AppDbContext _context;
        public SupplyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SupplyDto>> GetAllSuppliesAsync()
        {
            return await _context.Supplies
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
            var supply = await _context.Supplies
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
                ReceivedAt = dto.ReceivedAt,
                ProcessedAt = dto.ProcessedAt ?? 0
            };
            _context.Supplies.Add(supply);
            await _context.SaveChangesAsync();
            return await GetSupplyByIdAsync(supply.SupplyId) ?? throw new System.Exception("Failed to create supply.");
        }

        public async Task<bool> DeleteSupplyByIdAsync(int id)
        {
            var supply = await _context.Supplies.FindAsync(id);
            if (supply == null)
                return false;
            _context.Supplies.Remove(supply);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 