using esAPI.Data;
using esAPI.Simulation;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;
using esAPI.Clients;

namespace esAPI.Services
{
    public class SimulationAutoAdvanceService(IServiceProvider serviceProvider) : IHostedService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private CancellationTokenSource? _cts;
        private Task? _backgroundTask;
        private const int MinutesPerSimDay = 2;
        private const int MaxSimDays = 365;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var bankAccountService = scope.ServiceProvider.GetRequiredService<IBankAccountService>();
                var dayOrchestrator = scope.ServiceProvider.GetRequiredService<SimulationDayOrchestrator>();
                var stateService = scope.ServiceProvider.GetRequiredService<ISimulationStateService>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var autoAdvanceEnabled = config.GetValue<bool>("Simulation:AutoAdvanceEnabled");

                if (!autoAdvanceEnabled)
                    return Task.CompletedTask;

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _backgroundTask = RunSimulationLoop(_cts.Token);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        private async Task RunSimulationLoop(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var bankAccountService = scope.ServiceProvider.GetRequiredService<IBankAccountService>();
                    var dayOrchestrator = scope.ServiceProvider.GetRequiredService<SimulationDayOrchestrator>();
                    var stateService = scope.ServiceProvider.GetRequiredService<ISimulationStateService>();
                    var startupCostCalculator = scope.ServiceProvider.GetRequiredService<IStartupCostCalculator>();
                    var bankService = scope.ServiceProvider.GetRequiredService<IBankService>();
                    var bankClient = scope.ServiceProvider.GetRequiredService<ICommercialBankClient>();
                    var bulkLogisticsClient = scope.ServiceProvider.GetRequiredService<IBulkLogisticsClient>();
                    var electronicsService = scope.ServiceProvider.GetRequiredService<IElectronicsService>();
                    var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                    var thohApiClient = scope.ServiceProvider.GetRequiredService<IThohApiClient>();

                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var autoAdvanceEnabled = config.GetValue<bool>("Simulation:AutoAdvanceEnabled");

                    if (!autoAdvanceEnabled)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(MinutesPerSimDay), stoppingToken);
                        continue;
                    }

                    if (stateService.IsRunning)
                    {
                        if (stateService.CurrentDay > MaxSimDays)
                        {
                            stateService.Stop();
                            break;
                        }
                        using var dbScope = _serviceProvider.CreateScope();
                        var engine = dbScope.ServiceProvider.GetRequiredService<SimulationEngine>();
                        await engine.RunDayAsync(stateService.CurrentDay);
                    }
                }
                // Wait for the interval to elapse
                await Task.Delay(TimeSpan.FromMinutes(MinutesPerSimDay), stoppingToken);
                // Now increment the day
                using (var scope = _serviceProvider.CreateScope())
                {
                    var stateService = scope.ServiceProvider.GetRequiredService<ISimulationStateService>();
                    if (stateService.IsRunning)
                        stateService.AdvanceDay();
                }
            }
        }
    }
}
