using esAPI.Services;

namespace esAPI.DTOs
{
    public class DisasterDto
    {
        public int DisasterId { get; set; }
        public decimal BrokenAt { get; set; }
        public int MachinesAffected { get; set; }

        // Canonical time conversion for API responses
        public DateTime BrokenAtCanonical => BrokenAt.ToCanonicalTime();
    }
}