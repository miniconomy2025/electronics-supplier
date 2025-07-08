using System;
using esAPI.Models;
using SimulationModel = esAPI.Models.Simulation;

namespace esAPI.Services
{
    public class SimulationStateService
    {
        private readonly object _lock = new();
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