using Microsoft.AspNetCore.Mvc;
using esAPI.Data;
using esAPI.Models;
using System.Threading.Tasks;
using System.Linq;
using esAPI.Simulation;
using SimulationModel = esAPI.Models.Simulation;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("simulation")]
    public class SimulationController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SimulationController(AppDbContext context)
        {
            _context = context;
        }

        // POST /simulation - start the simulation
        [HttpPost]
        public async Task<IActionResult> StartSimulation()
        {
            var sim = _context.Simulations.FirstOrDefault();
            if (sim == null)
            {
                sim = new SimulationModel { DayNumber = 1, StartedAt = DateTime.UtcNow, IsRunning = true };
                _context.Simulations.Add(sim);
            }
            else
            {
                if (sim.IsRunning)
                    return BadRequest("Simulation already running.");
                sim.DayNumber = 1;
                sim.StartedAt = DateTime.UtcNow;
                sim.IsRunning = true;
            }
            await _context.SaveChangesAsync();
            return Ok(sim);
        }

        // GET /simulation - get current simulation state
        [HttpGet]
        public ActionResult<SimulationModel> GetSimulation()
        {
            var sim = _context.Simulations.FirstOrDefault();
            if (sim == null)
                return NotFound();
            return Ok(sim);
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

            var engine = new SimulationEngine(_context);
            await engine.RunDayAsync(sim.DayNumber);
            sim.DayNumber += 1;
            await _context.SaveChangesAsync();
            return Ok(new { sim.DayNumber });
        }
    }
} 