using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using esAPI.Data;
using esAPI.Services;
using esAPI.Simulation;
using esAPI.Interfaces;
using esAPI.Clients;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("simulation")]
    public class SimulationController(AppDbContext context, BankService bankService, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator, ISimulationStateService stateService, IStartupCostCalculator costCalculator, ICommercialBankClient bankClient, OrderExpirationBackgroundService orderExpirationBackgroundService, ILogger<SimulationController> logger, ILoggerFactory loggerFactory) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly IStartupCostCalculator _costCalculator = costCalculator;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly OrderExpirationBackgroundService _orderExpirationBackgroundService = orderExpirationBackgroundService;
        private readonly ILogger<SimulationController> _logger = logger;
        private readonly ILoggerFactory _loggerFactory = loggerFactory;

        private readonly BankService _bankService = bankService;

        // POST /simulation - start the simulation
        [HttpPost]
        public async Task<IActionResult> StartSimulation()
        {
            _logger.LogInformation("🚀 ===== MAIN SIMULATION ENDPOINT CALLED =====");
            _logger.LogInformation("🚀 Starting simulation");
            
            // Set up bank account with commercial bank
            _logger.LogInformation("🏦 Setting up bank account with commercial bank...");
            var bankSetupResult = await SetupBankAccountAsync();
            if (!bankSetupResult.Success)
            {
                _logger.LogError("❌ Failed to set up bank account. Error: {Error}", bankSetupResult.Error);
                return StatusCode(502, $"Failed to set up bank account. Error: {bankSetupResult.Error}");
            }
            
            _logger.LogInformation("✅ Bank account setup completed successfully");
            _stateService.Start();
            _logger.LogInformation("📊 Simulation state service started");
            
            // Start order expiration background service only after simulation starts
            _orderExpirationBackgroundService.StartAsync();
            _logger.LogInformation("⏰ Order expiration background service started");

            // Persist simulation start to the database
            _logger.LogInformation("💾 Persisting simulation start to database");
            var sim = await _context.Simulations.FirstOrDefaultAsync();
            if (sim == null)
            {
                _logger.LogInformation("📝 Creating new simulation record in database");
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
                _logger.LogInformation("🔄 Updating existing simulation record in database");
                sim.IsRunning = true;
                sim.StartedAt = DateTime.UtcNow;
                sim.DayNumber = 1;
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Simulation start persisted to database");

            _logger.LogInformation("✅ Simulation started successfully with bank account: {AccountNumber}", bankSetupResult.AccountNumber);
            return Ok(new { message = "Simulation started and bank account setup completed.", account_number = bankSetupResult.AccountNumber });
        }

        // POST /simulation/manual-backdoor - manually start the simulation with bank account setup
        [HttpPost("manual-backdoor")]
        public async Task<IActionResult> ManualStartSimulation()
        {
            _logger.LogInformation("🚀 ===== MANUAL BACKDOOR ENDPOINT CALLED =====");
            _logger.LogInformation("🚀 Starting simulation via manual backdoor with bank account setup");
            
            // Set up bank account with commercial bank
            _logger.LogInformation("🏦 Setting up bank account with commercial bank...");
            var bankSetupResult = await SetupBankAccountAsync();
            if (!bankSetupResult.Success)
            {
                _logger.LogError("❌ Failed to set up bank account. Error: {Error}", bankSetupResult.Error);
                return StatusCode(502, $"Failed to set up bank account. Error: {bankSetupResult.Error}");
            }
            
            _logger.LogInformation("✅ Bank account setup completed successfully");
            _stateService.Start();
            _logger.LogInformation("📊 Simulation state service started");
            
            _orderExpirationBackgroundService.StartAsync();
            _logger.LogInformation("⏰ Order expiration background service started");

            // Persist simulation start to the database
            _logger.LogInformation("💾 Persisting simulation start to database");
            var sim = await _context.Simulations.FirstOrDefaultAsync();
            if (sim == null)
            {
                _logger.LogInformation("📝 Creating new simulation record in database");
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
                _logger.LogInformation("🔄 Updating existing simulation record in database");
                sim.IsRunning = true;
                sim.StartedAt = DateTime.UtcNow;
                sim.DayNumber = 1;
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Simulation start persisted to database");

            _logger.LogInformation("✅ Simulation started successfully via manual backdoor with bank account: {AccountNumber}", bankSetupResult.AccountNumber);
            return Ok(new { message = "Simulation started via manual backdoor with bank account setup completed.", account_number = bankSetupResult.AccountNumber });
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
            if (_stateService.CurrentDay >= 365)
            {
                _logger.LogWarning("⚠️ Attempted to advance simulation beyond maximum days (365)");
                return BadRequest("Simulation has reached the maximum number of days (1 year).");
            }

            _logger.LogInformation("⏭️ Advancing simulation from day {CurrentDay} to {NextDay}", 
                _stateService.CurrentDay, _stateService.CurrentDay + 1);

            var engine = new SimulationEngine(_context, _bankService, _bankAccountService, _dayOrchestrator, _costCalculator, _bankClient, _loggerFactory.CreateLogger<SimulationEngine>());
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
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE material_supplies, material_orders, machines, machine_orders, machine_ratios, machine_statuses, machine_details, electronics, electronics_orders, lookup_values, simulation, disasters RESTART IDENTITY CASCADE;");
            _logger.LogInformation("✅ All simulation data cleared from database");

            return Ok(new { message = "Simulation stopped and all data deleted." });
        }

        private async Task<(bool Success, string AccountNumber, string Error)> SetupBankAccountAsync()
        {
            try
            {
                _logger.LogInformation("🏦 Creating bank account with notification URL...");
                
                // Create account with notification URL
                var createAccountRequest = new
                {
                    notification_url = "https://electronics-supplier-api.projects.bbdgrad.com/payments"
                };

                var createResponse = await _bankClient.CreateAccountAsync(createAccountRequest);
                
                if (createResponse.IsSuccessStatusCode)
                {
                    var accountNumber = await createResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("🏦 Bank account created successfully: {AccountNumber}", accountNumber);
                    
                    // Store account number in database
                    await StoreBankAccountNumberAsync(accountNumber);
                    
                    return (true, accountNumber, null);
                }
                else if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _logger.LogInformation("🏦 Account already exists, retrieving account number...");
                    
                    // Get existing account number
                    var getResponse = await _bankClient.GetAccountAsync();
                    if (getResponse.IsSuccessStatusCode)
                    {
                        var accountNumber = await getResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation("🏦 Retrieved existing account number: {AccountNumber}", accountNumber);
                        
                        // Store account number in database
                        await StoreBankAccountNumberAsync(accountNumber);
                        
                        return (true, accountNumber, null);
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to retrieve existing account number. Status: {Status}", getResponse.StatusCode);
                        return (false, null, $"Failed to retrieve existing account number. Status: {getResponse.StatusCode}");
                    }
                }
                else
                {
                    _logger.LogError("❌ Failed to create bank account. Status: {Status}", createResponse.StatusCode);
                    return (false, null, $"Failed to create bank account. Status: {createResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception during bank account setup");
                return (false, null, ex.Message);
            }
        }

        private async Task StoreBankAccountNumberAsync(string accountNumber)
        {
            try
            {
                // Store the account number in the database
                // You might want to create a dedicated table for this or store it in an existing table
                _logger.LogInformation("💾 Storing bank account number in database: {AccountNumber}", accountNumber);
                
                // For now, we'll just log it. You can implement the actual database storage as needed
                // Example: await _context.BankAccounts.AddAsync(new BankAccount { AccountNumber = accountNumber });
                // await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Bank account number stored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to store bank account number in database");
            }
        }
    }
}
