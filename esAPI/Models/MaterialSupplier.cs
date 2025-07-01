using System.ComponentModel.DataAnnotations;

namespace esAPI.Models
{
    public class MaterialSupplier
    {
        [Key]
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }
} 