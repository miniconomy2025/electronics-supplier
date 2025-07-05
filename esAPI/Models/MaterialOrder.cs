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
        [Column("external_order_id")]
        public int ExternalOrderId { get; set; }
        [Column("material_id")]
        public int MaterialId { get; set; }

        [Column("remaining_amount")]
        public int RemainingAmount { get; set; }

        [Column("ordered_at")]
        public DateTime OrderedAt { get; set; }

        [Column("received_at")]
        public DateTime? ReceivedAt { get; set; }

        [ForeignKey("SupplierId")]
        public MaterialSupplier? Supplier { get; set; }

        [ForeignKey("MaterialId")]
        public Material? Material { get; set; }
    }
}
