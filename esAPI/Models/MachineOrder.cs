using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Models
{
        [Table("machine_orders")]
        public class MachineOrder
        {
                [Key]
                [Column("order_id")]
                public int OrderId { get; set; }

                [Column("supplier_id")]
                public int SupplierId { get; set; }

        [Column("external_order_id")]
        public int? ExternalOrderId { get; set; }

        [Column("pickup_request_id")]
        public int? PickupRequestId { get; set; }

        [Column("remaining_amount")]
        public int RemainingAmount { get; set; }

                [Column("order_status")]
                public int OrderStatusId { get; set; }

                [Column("placed_at")]
                public decimal PlacedAt { get; set; }

                [Column("received_at")]
                public decimal? ReceivedAt { get; set; }

                [ForeignKey("SupplierId")]
                public Company? Supplier { get; set; }

                [ForeignKey("OrderStatusId")]
                public OrderStatus? OrderStatus { get; set; }
        }
}

[Keyless]
public class EffectiveMaterialStock
{
        [Column("material_id")]
        public int MaterialId { get; set; }

        [Column("material_name")]
        public required string MaterialName { get; set; }

        [Column("effective_quantity")]
        public long EffectiveQuantity { get; set; } // Use long to be safe with large sums
}
