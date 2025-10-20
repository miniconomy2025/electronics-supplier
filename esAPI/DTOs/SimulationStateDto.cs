namespace esAPI.DTOs
{
    public class SimulationStateDto
    {
        public bool IsRunning { get; set; }
        public DateTime? StartTimeUtc { get; set; }
        public int CurrentDay { get; set; }
        public long SimulationUnixEpoch { get; set; }
        public DateTime CanonicalSimulationDate { get; set; }
    }
}
