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
        [Column("remaining_amount")]
        public int RemainingAmount { get; set; }
        [Column("ordered_at")]
        public decimal OrderedAt { get; set; }
        [Column("processed_at")]
        public decimal? ProcessedAt { get; set; }
        [ForeignKey("ManufacturerId")]
        public Company? Manufacturer { get; set; }
        [ForeignKey("OrderStatusId")]
        public OrderStatus? OrderStatus { get; set; }
    }
} 