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

namespace esAPI.Services
{
    public class SimulationAutoAdvanceService(IServiceProvider serviceProvider, SimulationStateService stateService) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly SimulationStateService _stateService = stateService;
        private const int MinutesPerSimDay = 2;
        private const int MaxSimDays = 365;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_stateService.IsRunning && _stateService.CurrentDay < MaxSimDays)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var engine = new SimulationEngine(db);
                        await engine.RunDayAsync(_stateService.CurrentDay);
                        _stateService.AdvanceDay();

                        // Backup to DB
                        var sim = db.Simulations.FirstOrDefault();
                        if (sim != null)
                        {
                            sim.DayNumber = _stateService.CurrentDay;
                            await db.SaveChangesAsync();
                        }
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(MinutesPerSimDay), stoppingToken);
            }
        }
    }
} 