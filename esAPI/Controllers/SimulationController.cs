using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using esAPI.Data;
using esAPI.Services;
using esAPI.Simulation;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;
using esAPI.Clients;
using esAPI.DTOs.Simulation;
using System.Net.Http;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("simulation")]
    public class SimulationController(SimulationStartupService simulationStartupService, SimulationDayOrchestrator dayOrchestrator, ISimulationStateService stateService, IStartupCostCalculator costCalculator, ICommercialBankClient bankClient, AppDbContext context, BankService bankService, BankAccountService bankAccountService, RecyclerApiClient recyclerClient, IBulkLogisticsClient bulkLogisticsClient, IElectronicsService electronicsService, IHttpClientFactory httpClientFactory, ThohApiClient thohApiClient, ILogger<SimulationController> logger, ILoggerFactory loggerFactory) : ControllerBase
    {
        private readonly SimulationStartupService _simulationStartupService = simulationStartupService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly IStartupCostCalculator _costCalculator = costCalculator;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly AppDbContext _context = context;
        private readonly BankService _bankService = bankService;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly RecyclerApiClient _recyclerClient = recyclerClient;
        private readonly IBulkLogisticsClient _bulkLogisticsClient = bulkLogisticsClient;
        private readonly IElectronicsService _electronicsService = electronicsService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ThohApiClient _thohApiClient = thohApiClient;
        private readonly ILogger<SimulationController> _logger = logger;
        private readonly ILoggerFactory _loggerFactory = loggerFactory;

        // POST /simulation - start the simulation
        [HttpPost]
        public async Task<IActionResult> StartSimulation([FromBody] SimulationStartRequestDto? request = null)
        {
            _logger.LogInformation("🚀 ===== MAIN SIMULATION ENDPOINT CALLED =====");
            
            // Start simulation with or without external epoch time
            if (request?.EpochStartTime != null)
            {
                _logger.LogInformation("📅 Starting simulation with external epoch start time: {EpochStartTime}", request.EpochStartTime);
                _stateService.Start(request.EpochStartTime);
            }
            else
            {
                _logger.LogInformation("📅 Starting simulation with current time");
                _stateService.Start();
            }

            var result = await _simulationStartupService.StartSimulationAsync();
            if (!result.Success)
            {
                _logger.LogError("❌ Failed to start simulation. Error: {Error}", result.Error);
                return StatusCode(502, $"Failed to start simulation. Error: {result.Error}");
            }

            return Ok(new { message = "Simulation started" });
        }

        // GET /simulation - get current simulation state
        [HttpGet]
        public ActionResult<DTOs.SimulationStateDto> GetSimulation()
        {
            _logger.LogDebug("📊 Retrieving current simulation state");
            
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

            _logger.LogDebug("📊 Simulation state: Running={IsRunning}, Day={CurrentDay}, Time={SimTime}", 
                dto.IsRunning, dto.CurrentDay, simTime);
            
            return Ok(dto);
        }

        // PATCH /simulation/advance - advance the simulation by one day
        [HttpPatch("advance")]
        public async Task<IActionResult> AdvanceDay()
        {
            if (!_stateService.IsRunning)
            {
                _logger.LogWarning("⚠️ Attempted to advance simulation when not running");
                return BadRequest("Simulation not running.");
            }

            _logger.LogInformation("⏭️ Advancing simulation from day {CurrentDay} to {NextDay}", 
                _stateService.CurrentDay, _stateService.CurrentDay + 1);

            var engine = new SimulationEngine(_context, _bankService, _bankAccountService, _dayOrchestrator, _costCalculator, _bankClient, _recyclerClient, _bulkLogisticsClient, _stateService, _electronicsService, _httpClientFactory, _thohApiClient, _loggerFactory.CreateLogger<SimulationEngine>());
            await engine.RunDayAsync(_stateService.CurrentDay);
            _logger.LogInformation("✅ Day {Day} simulation logic completed", _stateService.CurrentDay);
            
            _stateService.AdvanceDay();
            _logger.LogInformation("📈 Simulation advanced to day {NewDay}", _stateService.CurrentDay);

            // Backup to DB
            _logger.LogInformation("💾 Updating simulation progress in database");
            var sim = _context.Simulations.FirstOrDefault();
            if (sim != null)
            {
                sim.DayNumber = _stateService.CurrentDay;
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Simulation progress saved to database");
            }
            else
            {
                _logger.LogWarning("⚠️ No simulation record found in database for backup");
            }
            
            return Ok(new { currentDay = _stateService.CurrentDay });
        }

        // DELETE /simulation - stop the simulation and delete all data
        [HttpDelete]
        public async Task<IActionResult> StopAndDeleteSimulation()
        {
            if (!_stateService.IsRunning)
            {
                _logger.LogWarning("⚠️ Attempted to stop simulation when not running");
                return BadRequest("Simulation is not running.");
            }
            
            _logger.LogInformation("🛑 Stopping simulation and cleaning up data");
            _stateService.Stop();
            _logger.LogInformation("📊 Simulation state service stopped");

            // Backup to DB
            _logger.LogInformation("💾 Updating simulation stop in database");
            var sim = _context.Simulations.FirstOrDefault();
            if (sim != null)
            {
                sim.IsRunning = false;
                sim.StartedAt = null;
                sim.DayNumber = 0;
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Simulation stop saved to database");
            }
            else
            {
                _logger.LogWarning("⚠️ No simulation record found in database for cleanup");
            }

            // Truncate all tables except views and migration history
            _logger.LogInformation("🗑️ Truncating all simulation data tables");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE material_supplies, material_orders, machines, machine_orders, machine_ratios, machine_details, electronics, electronics_orders, lookup_values, simulation, disasters RESTART IDENTITY CASCADE;");
            _logger.LogInformation("✅ All simulation data cleared from database");

            return Ok(new { message = "Simulation stopped and all data deleted." });
        }


    }
}
