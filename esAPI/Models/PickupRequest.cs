using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static esAPI.Models.Enums.PickupRequest;

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

        [Column("type")]
        public PickupType Type { get; set; }  // EF will store it as string if configured

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("placed_at")]
        public double PlacedAt { get; set; }
    }

}
