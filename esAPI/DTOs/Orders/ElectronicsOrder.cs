using System.Text.Json.Serialization;
using esAPI.Services;

namespace esAPI.DTOs.Orders
{
    public class ElectronicsOrder
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal OrderedAt { get; set; }
        public int TotalAmount { get; set; }
        public int RemainingAmount { get; set; }

        // Simulation timestamp conversions
        public DateTime OrderedAtSimTimestamp => OrderedAt.ToCanonicalTime();
    }

    public class ElectronicsOrderReceivedEvent
    {
        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("orderReceivedAtUtc")]
        public DateTime OrderReceivedAtUtc { get; set; }
    }
}
