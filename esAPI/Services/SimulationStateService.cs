using esAPI.Interfaces;
using SimulationModel = esAPI.Models.Simulation;

namespace esAPI.Services
{
    public class SimulationStateService : ISimulationStateService
    {
        private readonly Lock _lock = new();
        private bool _isRunning;
        private DateTime? _startTimeUtc;
        private int _currentDay;

        public void Start()
        {
            lock (_lock)
            {
                _isRunning = true;
                _startTimeUtc = DateTime.UtcNow;
                _currentDay = 1;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _startTimeUtc = null;
                _currentDay = 0;
            }
        }

        public void AdvanceDay()
        {
            lock (_lock)
            {
                if (_isRunning)
                    _currentDay++;
            }
        }

        public bool IsRunning
        {
            get { lock (_lock) { return _isRunning; } }
        }

        public DateTime? StartTimeUtc
        {
            get { lock (_lock) { return _startTimeUtc; } }
        }

        public int CurrentDay
        {
            get { lock (_lock) { return _currentDay; } }
        }

        // Returns the current simulation time as a decimal (e.g., 1.500 for halfway through Day 1)
        public decimal GetCurrentSimulationTime(int precision = 3)
        {
            lock (_lock)
            {
                if (!_isRunning || !_startTimeUtc.HasValue)
                    return 0m;
                var elapsed = DateTime.UtcNow - _startTimeUtc.Value;
                var totalSimDays = (decimal)(elapsed.TotalMinutes / 2.0);
                var simTime = 1m + totalSimDays; // Day 1 starts at 1.000
                return Math.Round(simTime, precision);
            }
        }

        // Restore state from a Simulation entity (e.g., on startup)
        public void RestoreFromBackup(SimulationModel sim)
        {
            lock (_lock)
            {
                _isRunning = sim.IsRunning;
                _startTimeUtc = sim.StartedAt;
                _currentDay = sim.DayNumber;
            }
        }

        // Create a Simulation entity for backup
        public SimulationModel ToBackupEntity()
        {
            lock (_lock)
            {
                return new SimulationModel
                {
                    IsRunning = _isRunning,
                    StartedAt = _startTimeUtc,
                    DayNumber = _currentDay
                };
            }
        }
    }
}