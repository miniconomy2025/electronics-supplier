using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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