using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
        [Table("electronics_orders")]
        public class ElectronicsOrder
        {
                [Key]
                [Column("order_id")]
                public int OrderId { get; set; }

                [Column("order_status")]
                public int OrderStatusId { get; set; }

                [ForeignKey("ManufacturerId")]
                [Column("manufacturer_id")]
                public int ManufacturerId { get; set; }

                [Column("total_amount")]
                public int TotalAmount { get; set; }

                [Column("remaining_amount")]
                public int RemainingAmount { get; set; }

                [Column("ordered_at", TypeName = "numeric(1000,3)")]
                public decimal OrderedAt { get; set; }

                [Column("processed_at", TypeName = "numeric(1000,3)")]
                public decimal? ProcessedAt { get; set; }

                [ForeignKey("OrderStatusId")]
                public OrderStatus? OrderStatus { get; set; }

        }
}
