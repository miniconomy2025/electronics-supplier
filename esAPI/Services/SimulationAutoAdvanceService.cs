using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using esAPI.Data;
using esAPI.Models;
using esAPI.Simulation;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using esAPI.Services;
using Microsoft.Extensions.Configuration;

namespace esAPI.Services
{
    public class SimulationAutoAdvanceService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource? _cts;
        private Task? _backgroundTask;
        private const int MinutesPerSimDay = 2;
        private const int MaxSimDays = 365;

        public SimulationAutoAdvanceService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var bankAccountService = scope.ServiceProvider.GetRequiredService<BankAccountService>();
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
                    var bankAccountService = scope.ServiceProvider.GetRequiredService<BankAccountService>();
                    var dayOrchestrator = scope.ServiceProvider.GetRequiredService<SimulationDayOrchestrator>();
                    var stateService = scope.ServiceProvider.GetRequiredService<ISimulationStateService>();
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
                        using (var dbScope = _serviceProvider.CreateScope())
                        {
                            var db = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
                            var engine = new SimulationEngine(db, bankAccountService, dayOrchestrator);
                            await engine.RunDayAsync(stateService.CurrentDay);
                            stateService.AdvanceDay();
                        }
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(MinutesPerSimDay), stoppingToken);
            }
        }
    }
} 