using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("material_suppliers")]
    public class MaterialSupplier
    {
        [Key]
        [Column("supplier_id")]
        public int SupplierId { get; set; }
        [Column("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;
    }
} 