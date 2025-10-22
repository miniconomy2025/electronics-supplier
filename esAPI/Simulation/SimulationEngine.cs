using Microsoft.Extensions.Logging;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Logging;

namespace esAPI.Simulation
{
    public class SimulationEngine(ISimulationStartupService startupService, ISimulationDayService dayService, ILogger<SimulationEngine> logger)
    {
        private readonly ISimulationStartupService _startupService = startupService;
        private readonly ISimulationDayService _dayService = dayService;
        private readonly ILogger<SimulationEngine> _logger = logger;

        public static event Func<int, Task>? OnDayAdvanced;

        public async Task RunDayAsync(int dayNumber)
        {
            _logger.LogInformation("[SimulationEngine] Starting simulation day {DayNumber}", dayNumber);

            try
            {
                if (dayNumber == 1)
                {
                    _logger.LogInformation("[SimulationEngine] Executing startup sequence for day 1");
                    var startupSuccess = await _startupService.ExecuteStartupSequenceAsync();
                    if (!startupSuccess)
                    {
                        _logger.LogErrorColored("[SimulationEngine] Startup sequence failed for day {0}", dayNumber);
                        return;
                    }
                }

                // Execute daily operations using the day service
                var daySuccess = await _dayService.ExecuteDayAsync(dayNumber);
                if (!daySuccess)
                {
                    _logger.LogErrorColored("[SimulationEngine] Daily operations failed for day {0}", dayNumber);
                    return;
                }

                // Trigger any day advanced events
                if (OnDayAdvanced != null)
                {
                    _logger.LogInformation("[SimulationEngine] Triggering OnDayAdvanced event for day {DayNumber}", dayNumber);
                    await OnDayAdvanced(dayNumber);
                }

                _logger.LogInformation("[SimulationEngine] Simulation day {DayNumber} completed successfully", dayNumber);
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored(ex, "[SimulationEngine] Critical error during simulation day {0}", dayNumber);
                throw;
            }
        }

    }
}
