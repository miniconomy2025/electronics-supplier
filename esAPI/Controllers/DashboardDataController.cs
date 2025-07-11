using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using esAPI.Data;

namespace esAPI.Controllers
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

        // 1. Machine statuses count
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

        // 2. Current material supply
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

        // 3. Electronics stock (from view)
        [HttpGet("electronics-stock")]
        public async Task<IActionResult> GetElectronicsStock()
        {
            var stock = await _context.Database
                .SqlQueryRaw<ElectronicsStockDto>("SELECT * FROM available_electronics_stock")
                .FirstOrDefaultAsync();

            return Ok(stock ?? new ElectronicsStockDto());
        }

        // 4. Total earnings (from view)
        [HttpGet("total-earnings")]
        public async Task<IActionResult> GetEarnings()
        {
            var total = await _context.Payments
                .Where(p => p.Status == "COMPLETED")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return Ok(new EarningsDto { TotalEarnings = total });
        }

        // 5. Inventory summary (from function)
        [HttpGet("inventory-summary")]
        public async Task<IActionResult> GetInventorySummary()
        {
            var result = await _context.Database
                .SqlQueryRaw<string>("SELECT get_inventory_summary()")
                .FirstOrDefaultAsync();

            // result is a JSON string, so just return it as content
            return Content(result ?? "{}", "application/json");
        }

        // --- DTOs for responses ---
        private class ElectronicsStockDto
        {
            public int availableStock { get; set; }
            public decimal pricePerUnit { get; set; }
        }

        private class EarningsDto
        {
            public decimal TotalEarnings { get; set; }
        }
    }
}
