using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("pickup_requests")]
    public class PickupRequest
    {
        [Key]
        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("external_request_id")]
        public int ExternalRequestId { get; set; }

        [Column("pickup_request_id")]
        public int? PickupRequestId { get; set; }

        [Column("type")]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty;

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("placed_at")]
        public double PlacedAt { get; set; }
    }

}
