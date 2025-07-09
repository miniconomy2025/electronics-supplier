namespace esAPI.Services
{
    public static class SimulationTimeService
    {
        // Simulation epoch: 2050-01-01 00:00:00 UTC
        private static readonly DateTime SimulationEpoch = new(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // 2 minutes real-time = 1 simulation day
        private const double MinutesPerSimulationDay = 2.0;
        
        /// <summary>
        /// Converts a decimal simulation time (e.g., 2.500) to a canonical DateTime
        /// </summary>
        /// <param name="simulationTime">Decimal simulation time (e.g., 2.500 for 12pm on day 2)</param>
        /// <returns>Canonical DateTime in UTC</returns>
        public static DateTime ToCanonicalTime(decimal simulationTime)
        {
            if (simulationTime <= 0)
                return SimulationEpoch;
                
            // Extract day number and time within the day
            int dayNumber = (int)Math.Floor(simulationTime);
            decimal timeWithinDay = simulationTime - dayNumber;
            
            // Convert to total days (0-based)
            int totalDays = dayNumber - 1; // Day 1 = 0 days elapsed
            
            // Add the time within the day
            double totalDaysWithFraction = (double)(totalDays + timeWithinDay);
            
            return SimulationEpoch.AddDays(totalDaysWithFraction);
        }
        
        /// <summary>
        /// Converts a canonical DateTime to decimal simulation time
        /// </summary>
        /// <param name="canonicalTime">Canonical DateTime in UTC</param>
        /// <returns>Decimal simulation time</returns>
        public static decimal FromCanonicalTime(DateTime canonicalTime)
        {
            if (canonicalTime < SimulationEpoch)
                return 0m;
                
            var timeSpan = canonicalTime - SimulationEpoch;
            double totalDays = timeSpan.TotalDays;
            
            // Convert to simulation time (1-based day numbering)
            return (decimal)(totalDays + 1);
        }
        
        /// <summary>
        /// Gets the current simulation time based on real elapsed time
        /// </summary>
        /// <param name="startTimeUtc">When the simulation started</param>
        /// <param name="precision">Decimal precision for the result</param>
        /// <returns>Current simulation time as decimal</returns>
        public static decimal GetCurrentSimulationTime(DateTime startTimeUtc, int precision = 3)
        {
            var elapsed = DateTime.UtcNow - startTimeUtc;
            var totalSimDays = (decimal)(elapsed.TotalMinutes / MinutesPerSimulationDay);
            var simTime = 1m + totalSimDays; // Day 1 starts at 1.000
            return Math.Round(simTime, precision);
        }
        
        /// <summary>
        /// Gets the simulation epoch (start date)
        /// </summary>
        public static DateTime Epoch => SimulationEpoch;
        
        /// <summary>
        /// Gets the minutes per simulation day
        /// </summary>
        public static double MinutesPerDay => MinutesPerSimulationDay;
    }
} 