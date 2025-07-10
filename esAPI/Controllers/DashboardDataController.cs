using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using esAPI.Data;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardDataController(AppDbContext context)
        {
            _context = context;
        }

        // Example endpoint: Get machine statuses count
        [HttpGet("machines-status")]
        public async Task<IActionResult> GetMachinesStatus()
        {
            var data = await _context.Machines
                .Where(m => m.RemovedAt == null)
                .GroupBy(m => m.MachineStatusId)
                .Select(g => new
                {
                    Status = _context.MachineStatuses
                            .Where(ms => ms.StatusId == g.Key)
                            .Select(ms => ms.Status)
                            .FirstOrDefault(),
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("current-supply")]
        public async Task<IActionResult> GetCurrentSupply()
        {
            var result = await _context.MaterialSupplies
                .Where(s => s.ProcessedAt == null)
                .GroupBy(s => new { s.MaterialId, s.Material!.MaterialName })
                .Select(g => new
                {
                    MaterialId = g.Key.MaterialId,
                    MaterialName = g.Key.MaterialName,
                    AvailableSupply = g.Count()
                })
                .ToListAsync();

            return Ok(result);
        }


        // Add more endpoints for other data as needed
    }
}
