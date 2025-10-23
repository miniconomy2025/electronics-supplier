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
        private long? _externalEpochStartTime;

        public void Start()
        {
            lock (_lock)
            {
                _isRunning = true;
                _startTimeUtc = DateTime.UtcNow;
                _currentDay = 1;
                _externalEpochStartTime = null;
            }
        }

        public void Start(long epochStartTime)
        {
            lock (_lock)
            {
                _isRunning = true;
                _startTimeUtc = DateTime.UtcNow;
                _currentDay = 1;
                _externalEpochStartTime = epochStartTime;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _startTimeUtc = null;
                _currentDay = 0;
                _externalEpochStartTime = null;
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

                DateTime referenceStartTime;

                // If external epoch start time is provided, use it as the reference
                if (_externalEpochStartTime.HasValue)
                {
                    var epochSeconds = _externalEpochStartTime.Value;
                    
                    // Auto-detect and handle millisecond timestamps (backup validation)
                    const long MillisecondThreshold = 1_000_000_000_000; // 1e12 - likely milliseconds
                    const long MinValidSeconds = -62135596800; // 0001-01-01 00:00:00 UTC
                    const long MaxValidSeconds = 253402300799; // 9999-12-31 23:59:59 UTC
                    
                    if (epochSeconds > MillisecondThreshold)
                    {
                        // This should have been caught at the controller level, but handle as backup
                        epochSeconds = epochSeconds / 1000;
                    }
                    
                    // Validate the timestamp is within acceptable range for DateTimeOffset.FromUnixTimeSeconds
                    if (epochSeconds < MinValidSeconds || epochSeconds > MaxValidSeconds)
                    {
                        throw new InvalidOperationException(
                            $"External epoch start time {epochSeconds} is outside valid range. " +
                            $"Valid Unix timestamps are between {MinValidSeconds} and {MaxValidSeconds} seconds. " +
                            $"This corresponds to dates between 0001-01-01 and 9999-12-31.");
                    }
                    
                    referenceStartTime = DateTimeOffset.FromUnixTimeSeconds(epochSeconds).DateTime;
                }
                else
                {
                    referenceStartTime = _startTimeUtc.Value;
                }

                var elapsed = DateTime.UtcNow - referenceStartTime;
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
