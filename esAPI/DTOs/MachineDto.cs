using esAPI.Services;

namespace esAPI.DTOs
{
    public class MachineDto
    {
        public int MachineId { get; set; }
        public required string Status { get; set; } // status name, e.g. "WORKING"
        public float PurchasePrice { get; set; }
        public decimal PurchasedAt { get; set; }

        // Simulation timestamp conversions
        public DateTime PurchasedAtSimTimestamp => PurchasedAt.ToCanonicalTime();
    }
}
