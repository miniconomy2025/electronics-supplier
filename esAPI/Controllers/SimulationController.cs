using Microsoft.AspNetCore.Mvc;
using esAPI.Data;
using esAPI.Models;
using System.Threading.Tasks;
using System.Linq;
using esAPI.Simulation;
using SimulationModel = esAPI.Models.Simulation;
using esAPI.Services;
using esAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("simulation")]
    public class SimulationController(AppDbContext context, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator, ISimulationStateService stateService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;
        private readonly ISimulationStateService _stateService = stateService;

        // POST /simulation - start the simulation
        [HttpPost]
        public IActionResult StartSimulation()
        {
            _stateService.Start();
            return Ok("Simulation started.");
        }

        // GET /simulation - get current simulation state
        [HttpGet]
        public ActionResult<DTOs.SimulationStateDto> GetSimulation()
        {
            var simTime = _stateService.GetCurrentSimulationTime(3);
            var canonicalSimDate = simTime.ToCanonicalTime();
            var simulationEpoch = new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var simulationUnixEpoch = (long)(canonicalSimDate - simulationEpoch).TotalSeconds;

            var dto = new DTOs.SimulationStateDto
            {
                IsRunning = _stateService.IsRunning,
                StartTimeUtc = _stateService.StartTimeUtc,
                CurrentDay = _stateService.CurrentDay,
                SimulationUnixEpoch = simulationUnixEpoch,
                CanonicalSimulationDate = canonicalSimDate
            };

            return Ok(dto);
        }

        // PATCH /simulation/advance - advance the simulation by one day
        [HttpPatch("advance")]
        public async Task<IActionResult> AdvanceDay()
        {
            if (!_stateService.IsRunning)
                return BadRequest("Simulation not running.");
            if (_stateService.CurrentDay >= 365)
                return BadRequest("Simulation has reached the maximum number of days (1 year).");

            var engine = new SimulationEngine(_context, _bankAccountService, _dayOrchestrator);
            await engine.RunDayAsync(_stateService.CurrentDay);
            _stateService.AdvanceDay();

            // Backup to DB
            var sim = _context.Simulations.FirstOrDefault();
            if (sim != null)
            {
                sim.DayNumber = _stateService.CurrentDay;
                await _context.SaveChangesAsync();
            }
            return Ok(new { currentDay = _stateService.CurrentDay });
        }

        // DELETE /simulation - stop the simulation and delete all data
        [HttpDelete]
        public async Task<IActionResult> StopAndDeleteSimulation()
        {
            if (!_stateService.IsRunning)
                return BadRequest("Simulation is not running.");
            _stateService.Stop();

            // Backup to DB
            var sim = _context.Simulations.FirstOrDefault();
            if (sim != null)
            {
                sim.IsRunning = false;
                sim.StartedAt = null;
                sim.DayNumber = 0;
                await _context.SaveChangesAsync();
            }

            // Truncate all tables except views and migration history
            // Use lowercase table names as they appear in the database
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE material_supplies, material_orders, machines, machine_orders, machine_ratios, machine_statuses, machine_details, electronics, electronics_orders, lookup_values, simulation, disasters RESTART IDENTITY CASCADE;");

            return Ok(new { message = "Simulation stopped and all data deleted." });
        }
    }
} 