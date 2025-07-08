using System;

namespace esAPI.Services
{
    public class SimulationTimerService
    {
        private DateTime? _startTimeUtc;
        private bool _isRunning;
        private readonly object _lock = new object();
        private const double MinutesPerSimDay = 2.0;

        public void Start()
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    _startTimeUtc = DateTime.UtcNow;
                    _isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _startTimeUtc = null;
            }
        }

        public bool IsRunning
        {
            get { lock (_lock) { return _isRunning; } }
        }

        public int GetCurrentSimDay()
        {
            lock (_lock)
            {
                if (!_isRunning || !_startTimeUtc.HasValue)
                    return 0;
                var elapsed = DateTime.UtcNow - _startTimeUtc.Value;
                return (int)(elapsed.TotalMinutes / MinutesPerSimDay) + 1; // Day 1-based
            }
        }

        public DateTime? GetStartTimeUtc()
        {
            lock (_lock)
            {
                return _startTimeUtc;
            }
        }
    }
} 