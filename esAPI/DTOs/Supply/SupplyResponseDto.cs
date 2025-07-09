using System;
using esAPI.Services;

namespace esAPI.DTOs.Supply
{
    public class SupplyDto
    {
        public int SupplyId { get; set; }
        public decimal ReceivedAt { get; set; }
        public decimal? ProcessedAt { get; set; }
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
        
        // Simulation timestamp conversions
        public DateTime ReceivedAtSimTimestamp => ReceivedAt.ToCanonicalTime();
        public DateTime? ProcessedAtSimTimestamp => ProcessedAt?.ToCanonicalTime();
    }
}