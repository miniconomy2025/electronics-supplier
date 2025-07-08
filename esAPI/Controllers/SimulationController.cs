using Microsoft.AspNetCore.Mvc;
using esAPI.Data;
using esAPI.Models;
using System.Threading.Tasks;
using System.Linq;
using esAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("simulation")]
    public class SimulationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly SimulationTimerService _timerService;
        public SimulationController(AppDbContext context, SimulationTimerService timerService)
        {
            _context = context;
            _timerService = timerService;
        }

        // POST /simulation - start the simulation
        [HttpPost]
        public IActionResult StartSimulation()
        {
            if (_timerService.IsRunning)
                return BadRequest("Simulation already running.");
            _timerService.Start();
            return Ok(new { message = "Simulation started." });
        }

        // DELETE /simulation - stop the simulation and delete all data
        [HttpDelete]
        public async Task<IActionResult> StopAndDeleteSimulation()
        {
            if (!_timerService.IsRunning)
                return BadRequest("Simulation is not running.");
            _timerService.Stop();

            // Truncate all tables except views
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Companies\", \"Materials\", \"MaterialSupplies\", \"MaterialOrders\", \"Machines\", \"MachineOrders\", \"MachineRatios\", \"MachineStatuses\", \"MachineDetails\", \"Electronics\", \"ElectronicsOrders\", \"ElectronicsStatuses\", \"OrderStatuses\", \"LookupValues\", \"Simulations\" RESTART IDENTITY CASCADE;");

            return Ok(new { message = "Simulation stopped and all data deleted." });
        }

        // GET /simulation - get current simulation state
        [HttpGet]
        public ActionResult<Simulation> GetSimulation()
        {
            var sim = _context.Simulations.FirstOrDefault();
            if (sim == null)
                return NotFound();
            return Ok(sim);
        }

        // GET /simulation/time - get current simulation day and running status
        [HttpGet("time")]
        public IActionResult GetSimulationTime()
        {
            var isRunning = _timerService.IsRunning;
            var day = _timerService.GetCurrentSimDay();
            return Ok(new { isRunning, currentDay = day });
        }

        // PATCH /simulation/advance - advance the simulation by one day
        [HttpPatch("advance")]
        public async Task<IActionResult> AdvanceDay()
        {
            var sim = _context.Simulations.FirstOrDefault();
            if (sim == null || !sim.IsRunning)
                return BadRequest("Simulation not running.");
            if (sim.DayNumber >= 365)
                return BadRequest("Simulation has reached the maximum number of days (1 year).");
            sim.DayNumber += 1;
            await _context.SaveChangesAsync();
            return Ok(new { sim.DayNumber });
        }
    }
} 