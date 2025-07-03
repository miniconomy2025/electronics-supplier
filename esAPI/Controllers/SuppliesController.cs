using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Models;
using esAPI.Dtos;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliesController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpPost]
        public async Task<IActionResult> CreateSupply([FromBody] CreateSupplyDto dto)
        {
            var materialExists = await _context.Materials.AnyAsync(m => m.MaterialId == dto.MaterialId);
            if (!materialExists)
                return NotFound($"Material with ID {dto.MaterialId} does not exist.");

            var supply = new Supply
            {
                MaterialId = dto.MaterialId,
                ReceivedAt = dto.ReceivedAt,
                ProcessedAt = dto.ProcessedAt
            };

            _context.Supplies.Add(supply);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSupplyById), new { id = supply.SupplyId }, supply);
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplyDto>>> GetAllSupplies()
        {
            var supplies = await _context.Supplies
                .Include(s => s.Material)
                .Select(s => new SupplyDto
                {
                    SupplyId = s.SupplyId,
                    ReceivedAt = s.ReceivedAt,
                    ProcessedAt = s.ProcessedAt,
                    MaterialId = s.MaterialId,
                    MaterialName = s.Material!.MaterialName
                })
                .ToListAsync();

            return Ok(supplies);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupplyDto>> GetSupplyById(int id)
        {
            var supply = await _context.Supplies
                .Include(s => s.Material)
                .FirstOrDefaultAsync(s => s.SupplyId == id);

            if (supply == null)
                return NotFound();

            var dto = new SupplyDto
            {
                SupplyId = supply.SupplyId,
                MaterialId = supply.MaterialId,
                ReceivedAt = supply.ReceivedAt,
                ProcessedAt = supply.ProcessedAt,
                MaterialName = supply.Material?.MaterialName
            };

            return Ok(dto);
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplyById(int id)
        {
            var supply = await _context.Supplies.FindAsync(id);
            if (supply == null)
            {
                return NotFound($"Supply with ID {id} was not found.");
            }

            _context.Supplies.Remove(supply);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
