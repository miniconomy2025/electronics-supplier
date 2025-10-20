using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using esAPI.Data;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Machine statuses count
        [HttpGet("machines/status")]
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
        [HttpGet("supplies/current")]
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
        [HttpGet("electronics/stock")]
        public async Task<IActionResult> GetElectronicsStock()
        {
            var stock = await _context.Database
                .SqlQueryRaw<ElectronicsStockDto>("SELECT * FROM available_electronics_stock")
                .FirstOrDefaultAsync();

            return Ok(stock ?? new ElectronicsStockDto());
        }

        // 4. Total earnings (from view)
        [HttpGet("earnings/total")]
        public async Task<IActionResult> GetEarnings()
        {
            var total = await _context.Payments
                .Where(p => p.Status == "COMPLETED")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return Ok(new EarningsDto { TotalEarnings = total });
        }

        // 5. Inventory summary (from function)
        [HttpGet("inventory/summary")]
        public async Task<IActionResult> GetInventorySummary()
        {
            await using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "SELECT get_inventory_summary()";
            await _context.Database.OpenConnectionAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var json = reader.GetString(0);
                return Content(json, "application/json");
            }

            return NotFound();
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await (
                from o in _context.ElectronicsOrders
                join c in _context.Companies on o.ManufacturerId equals c.CompanyId
                join s in _context.OrderStatuses on o.OrderStatusId equals s.StatusId
                orderby o.OrderedAt descending
                select new
                {
                    DateOfTransaction = o.OrderedAt,
                    CompanyName = c.CompanyName,
                    Item = "Phone electronics",
                    AccountNo = c.BankAccountNumber,
                    Amount = o.TotalAmount,
                    Status = s.Status
                }

            ).ToListAsync();

            return Ok(orders);
        }

        [HttpGet("payments")]
        public async Task<IActionResult> GetPaymentsOverTime()
        {
            var payments = await _context.Payments
                .Where(p => p.Status == "COMPLETED")
                .OrderBy(p => p.Timestamp)
                .ToListAsync();

            var result = new List<object>();
            decimal runningTotal = 0;

            foreach (var p in payments)
            {
                runningTotal += p.Amount;
                result.Add(new
                {
                    timestamp = p.Timestamp,
                    cumulativeBalance = runningTotal
                });
            }

            return Ok(result);
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

        [HttpGet("bank/balance")]
        public async Task<IActionResult> GetBankBalance()
        {
            var latest = await _context.BankBalanceSnapshots
                .OrderByDescending(b => b.SimulationDay)
                .ThenByDescending(b => b.Timestamp)
                .FirstOrDefaultAsync();

            if (latest == null)
                return Ok(new { balance = 0 });

            return Ok(new { balance = latest.Balance });
        }
    }
}
