using esAPI.Services;

namespace esAPI.DTOs.Supply
{
    public class CreateSupplyDto
    {
        public int MaterialId { get; set; }
        public decimal ReceivedAt { get; set; }
        public decimal? ProcessedAt { get; set; }

        // Simulation timestamp conversions
        public DateTime ReceivedAtSimTimestamp => ReceivedAt.ToCanonicalTime();
        public DateTime? ProcessedAtSimTimestamp => ProcessedAt?.ToCanonicalTime();
    }
}

