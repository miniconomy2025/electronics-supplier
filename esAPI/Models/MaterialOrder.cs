using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Column("ordered_at")]
        public DateTime OrderedAt { get; set; }
        [Column("received_at")]
        public DateTime? ReceivedAt { get; set; }
        [ForeignKey("SupplierId")]
        public MaterialSupplier? Supplier { get; set; }

        public ICollection<MaterialOrderItem> Items { get; set; } = new List<MaterialOrderItem>();

    }
} 