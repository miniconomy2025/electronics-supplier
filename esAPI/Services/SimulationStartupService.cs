using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Services;
using esAPI.Simulation;
using esAPI.Interfaces;
using esAPI.Clients;
using esAPI.Logging;

namespace esAPI.Services
{
    public class SimulationStartupService
    {
        private readonly AppDbContext _context;
        private readonly IBankAccountService _bankAccountService;
        private readonly ISimulationStateService _stateService;
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<SimulationStartupService> _logger;
        private readonly IThohApiClient _thohApiClient;
        private readonly IElectronicsMachineDetailsService _machineDetailsService;

        public SimulationStartupService(
            AppDbContext context,
            IBankAccountService bankAccountService,
            ISimulationStateService stateService,
            ICommercialBankClient bankClient,
            ILogger<SimulationStartupService> logger,
            IThohApiClient thohApiClient,
            IElectronicsMachineDetailsService machineDetailsService)
        {
            _context = context;
            _bankAccountService = bankAccountService;
            _stateService = stateService;
            _bankClient = bankClient;
            _logger = logger;
            _thohApiClient = thohApiClient;
            _machineDetailsService = machineDetailsService;
        }

        public async Task<(bool Success, string? AccountNumber, string? Error)> StartSimulationAsync()
        {
            try
            {
                _logger.LogInformation("[SimulationStartup] Starting simulation startup process");

                // Note: Simulation state service already started by controller
                // Note: Order expiration background service starts automatically as a hosted service

                // Ensure company exists in database
                _logger.LogInformation("[SimulationStartup] Ensuring company record exists");
                await EnsureCompanyExistsAsync();

                // Set up bank account with commercial bank
                _logger.LogInformation("[SimulationStartup] Setting up bank account with commercial bank");
                var bankSetupResult = await _bankAccountService.SetupBankAccountAsync();
                if (!bankSetupResult.Success)
                {
                    _logger.LogErrorColored("[SimulationStartup] Failed to set up bank account. Error: {0}", bankSetupResult.Error ?? "Unknown error");
                    return (false, null, $"Failed to set up bank account. Error: {bankSetupResult.Error}");
                }

                _logger.LogInformation("[SimulationStartup] Bank account setup completed successfully");

                // Check current balance and request loan if needed
                _logger.LogInformation("[SimulationStartup] Checking current account balance");
                var currentBalance = await _bankClient.GetAccountBalanceAsync();
                _logger.LogInformation("[SimulationStartup] Current account balance: {Balance}", currentBalance);

                if (currentBalance <= 10000m)
                {
                    _logger.LogInformation("[SimulationStartup] Balance is {Balance} (≤ 10,000), requesting startup loan", currentBalance);
                    try
                    {
                        const decimal initialLoanAmount = 200000m; // R200k
                        string? loanSuccess = await _bankClient.RequestLoanAsync(initialLoanAmount);
                        if (loanSuccess == null)
                        {
                            _logger.LogWarning("⚠️ Initial loan request failed, trying with smaller amount...");
                            // Try with a smaller amount if the initial request fails
                            const decimal fallbackLoanAmount = 10000000m; // 10 million
                            loanSuccess = await _bankClient.RequestLoanAsync(fallbackLoanAmount);
                            if (loanSuccess == null)
                            {
                                _logger.LogWarning("⚠️ Failed to request startup loan with both amounts - simulation will continue without loan");
                            }
                            else
                            {
                                _logger.LogInformation("✅ Startup loan requested successfully with fallback amount: {LoanNumber}", loanSuccess);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("✅ Startup loan requested successfully: {LoanNumber}", loanSuccess);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SimulationStartup] Exception during startup loan request - simulation will continue without loan");
                    }
                }
                else
                {
                    _logger.LogInformation("[SimulationStartup] Balance is {Balance}, no loan needed", currentBalance);
                }

                // Query and sync electronics machine details
                _logger.LogInformation("[SimulationStartup] Syncing electronics machine details from THOH");
                var machineDetailsSynced = await _machineDetailsService.SyncElectronicsMachineDetailsAsync();
                if (!machineDetailsSynced)
                {
                    _logger.LogWarning("[SimulationStartup] Could not sync electronics machine details from THOH. Continuing simulation startup");
                }
                else
                {
                    _logger.LogInformation("[SimulationStartup] Electronics machine details synced");
                }

                // Persist simulation start to the database
                await PersistSimulationStartAsync();

                _logger.LogInformation("[SimulationStartup] Simulation started successfully with bank account: {AccountNumber}", bankSetupResult.AccountNumber);
                return (true, bankSetupResult.AccountNumber, null);
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored(ex, "[SimulationStartup] Exception during simulation startup");
                return (false, null, ex.Message);
            }
        }

        private async Task PersistSimulationStartAsync()
        {
            _logger.LogInformation("[SimulationStartup] Persisting simulation start to database");
            var sim = await _context.Simulations.FirstOrDefaultAsync();
            if (sim == null)
            {
                _logger.LogInformation("[SimulationStartup] Creating new simulation record in database");
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
                _logger.LogInformation("[SimulationStartup] Updating existing simulation record in database");
                sim.IsRunning = true;
                sim.StartedAt = DateTime.UtcNow;
                sim.DayNumber = 1;
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Simulation start persisted to database");
        }

        private async Task EnsureCompanyExistsAsync()
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == 1);
            if (company == null)
            {
                _logger.LogInformation("[SimulationStartup] Creating Electronics Supplier company record (ID=1)");
                company = new Models.Company
                {
                    CompanyId = 1,
                    CompanyName = "Electronics Supplier",
                    BankAccountNumber = null
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[SimulationStartup] Electronics Supplier company record created");
            }
            else
            {
                _logger.LogInformation("[SimulationStartup] Electronics Supplier company record already exists");
            }
        }
    }
}
