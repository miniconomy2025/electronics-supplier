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

namespace esAPI.Services
{
    public class SimulationAutoAdvanceService : IHostedService
    {
        private readonly AppDbContext _context;
        private readonly BankAccountService _bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator;
        private readonly ISimulationStateService _stateService;
        private CancellationTokenSource _cts;
        private Task _backgroundTask;
        private const int MinutesPerSimDay = 2;
        private const int MaxSimDays = 365;

        public SimulationAutoAdvanceService(AppDbContext context, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator, ISimulationStateService stateService)
        {
            _context = context;
            _bankAccountService = bankAccountService;
            _dayOrchestrator = dayOrchestrator;
            _stateService = stateService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _backgroundTask = RunSimulationLoop(_cts.Token);
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
                if (_stateService.IsRunning)
                {
                    if (_stateService.CurrentDay > MaxSimDays)
                    {
                        _stateService.Stop();
                        break;
                    }
                    var engine = new SimulationEngine(_context, _bankAccountService, _dayOrchestrator);
                    await engine.RunDayAsync(_stateService.CurrentDay);
                    _stateService.AdvanceDay();
                }
                await Task.Delay(TimeSpan.FromMinutes(MinutesPerSimDay), stoppingToken);
            }
        }
    }
} 