using System;
using esAPI.Services;

namespace esAPI.DTOs.Electronics
{
    public class ElectronicsOrderUpdateDto
    {
        public int? RemainingAmount { get; set; }
        public decimal? ProcessedAt { get; set; }
        public string? OrderStatus { get; set; }
        
        // Simulation timestamp conversions
        public DateTime? ProcessedAtSimTimestamp => ProcessedAt?.ToCanonicalTime();
    }
}
