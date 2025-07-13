using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using esAPI.Data;
using esAPI.Services;
using esAPI.Simulation;
using esAPI.Interfaces;
using esAPI.Clients;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("simulation")]
    public class SimulationController(AppDbContext context, BankService bankService, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator, ISimulationStateService stateService, IStartupCostCalculator costCalculator, ICommercialBankClient bankClient, OrderExpirationBackgroundService orderExpirationBackgroundService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly IStartupCostCalculator _costCalculator = costCalculator;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly OrderExpirationBackgroundService _orderExpirationBackgroundService = orderExpirationBackgroundService;

        private readonly BankService _bankService = bankService;


        // POST /simulation - start the simulation
        [HttpPost]
        public async Task<IActionResult> StartSimulation()
        {
            var result = await _dayOrchestrator.OrchestrateAsync();
            if (!result.Success)
            {
                return StatusCode(502, $"Failed to set up Commercial Bank account or notification URL. Error: {result.Error}");
            }
            _stateService.Start();
            // Start order expiration background service only after simulation starts
            _orderExpirationBackgroundService.StartAsync();

            // Persist simulation start to the database
            var sim = await _context.Simulations.FirstOrDefaultAsync();
            if (sim == null)
            {
                sim = new Models.Simulation
                {
                    IsRunning = true,
                    StartedAt = DateTime.UtcNow,
                    DayNumber = 1
                };
                _context.Simulations.Add(sim);
            }
            else
            {
                sim.IsRunning = true;
                sim.StartedAt = DateTime.UtcNow;
                sim.DayNumber = 1;
            }
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(result.AccountNumber))
            {
                return StatusCode(201, new { message = "Simulation started, account created, and notification URL set.", account_number = result.AccountNumber });
            }
            if (result.Error == "accountAlreadyExists")
            {
                return Ok(new { message = "Simulation started, account already exists, notification URL set." });
            }
            return Ok("Simulation started and notification URL set.");
        }

        // POST /simulation/manual-backdoor - manually start the simulation without any external API calls
        [HttpPost("manual-backdoor")]
        public async Task<IActionResult> ManualStartSimulation()
        {
            _stateService.Start();
            _orderExpirationBackgroundService.StartAsync();

            // Persist simulation start to the database
            var sim = await _context.Simulations.FirstOrDefaultAsync();
            if (sim == null)
            {
                sim = new Models.Simulation
                {
                    IsRunning = true,
                    StartedAt = DateTime.UtcNow,
                    DayNumber = 1
                };
                _context.Simulations.Add(sim);
            }
            else
            {
                sim.IsRunning = true;
                sim.StartedAt = DateTime.UtcNow;
                sim.DayNumber = 1;
            }
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Simulation started via manual backdoor. No external API calls were made." });
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

            var engine = new SimulationEngine(_context, _bankService, _bankAccountService, _dayOrchestrator, _costCalculator, _bankClient);
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
