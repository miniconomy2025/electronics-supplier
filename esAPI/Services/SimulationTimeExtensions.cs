namespace esAPI.Services
{
    public static class SimulationTimeExtensions
    {
        /// <summary>
        /// Converts a decimal simulation time to a canonical DateTime for API responses
        /// </summary>
        /// <param name="simulationTime">Decimal simulation time</param>
        /// <returns>Canonical DateTime in UTC</returns>
        public static DateTime ToCanonicalTime(this decimal simulationTime)
        {
            return SimulationTimeService.ToCanonicalTime(simulationTime);
        }

        /// <summary>
        /// Converts a canonical DateTime to decimal simulation time for storage
        /// </summary>
        /// <param name="canonicalTime">Canonical DateTime in UTC</param>
        /// <returns>Decimal simulation time</returns>
        public static decimal ToSimulationTime(this DateTime canonicalTime)
        {
            return SimulationTimeService.FromCanonicalTime(canonicalTime);
        }

        /// <summary>
        /// Converts a nullable decimal simulation time to a nullable canonical DateTime
        /// </summary>
        /// <param name="simulationTime">Nullable decimal simulation time</param>
        /// <returns>Nullable canonical DateTime in UTC</returns>
        public static DateTime? ToCanonicalTime(this decimal? simulationTime)
        {
            return simulationTime?.ToCanonicalTime();
        }

        /// <summary>
        /// Converts a nullable canonical DateTime to nullable decimal simulation time
        /// </summary>
        /// <param name="canonicalTime">Nullable canonical DateTime in UTC</param>
        /// <returns>Nullable decimal simulation time</returns>
        public static decimal? ToSimulationTime(this DateTime? canonicalTime)
        {
            return canonicalTime?.ToSimulationTime();
        }
    }
}