using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Models
{
        [Table("material_orders")]
        public class MaterialOrder
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

                [Column("order_status")]
                public int OrderStatusId { get; set; }

                [Column("material_id")]
                public int MaterialId { get; set; }

                [Column("remaining_amount")]
                public int RemainingAmount { get; set; }

                [Column("ordered_at", TypeName = "numeric(1000,3)")]
                public decimal OrderedAt { get; set; }

                [Column("received_at", TypeName = "numeric(1000,3)")]
                public decimal? ReceivedAt { get; set; }

                [ForeignKey("SupplierId")]
                public Company? Supplier { get; set; }

                [ForeignKey("MaterialId")]
                public Material? Material { get; set; }
                [ForeignKey("OrderStatusId")]
                public OrderStatus? OrderStatus { get; set; }
        }
}

[Keyless]
public class CurrentSupply
{
        [Column("material_id")]
        public int MaterialId { get; set; }

        [Column("material_name")]
        public required string MaterialName { get; set; }

        [Column("available_supply")]
        public int AvailableSupply { get; set; }
}
