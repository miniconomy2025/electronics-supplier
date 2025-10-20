using Microsoft.Extensions.Logging;
using esAPI.Services;
using esAPI.Interfaces;

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
            _logger.LogInformation("\n =============== 🏃‍♂️ Starting simulation day {DayNumber} ===============\n", dayNumber);

            try
            {
                if (dayNumber == 1)
                {
                    _logger.LogInformation("🎬 Executing startup sequence for day 1");
                    var startupSuccess = await _startupService.ExecuteStartupSequenceAsync();
                    if (!startupSuccess)
                    {
                        _logger.LogError("❌ Startup sequence failed for day {DayNumber}", dayNumber);
                        return;
                    }
                }

                // Execute daily operations using the day service
                var daySuccess = await _dayService.ExecuteDayAsync(dayNumber);
                if (!daySuccess)
                {
                    _logger.LogError("❌ Daily operations failed for day {DayNumber}", dayNumber);
                    return;
                }

                // Trigger any day advanced events
                if (OnDayAdvanced != null)
                {
                    _logger.LogInformation("📡 Triggering OnDayAdvanced event for day {DayNumber}", dayNumber);
                    await OnDayAdvanced(dayNumber);
                }

                _logger.LogInformation("✅ Simulation day {DayNumber} completed successfully", dayNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Critical error during simulation day {DayNumber}", dayNumber);
                throw;
            }
        }

    }
}
