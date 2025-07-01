using System.ComponentModel.DataAnnotations;

namespace esAPI.Models
{
    public class MaterialOrder
    {
        [Key]
        public int OrderId { get; set; }
        public int SupplierId { get; set; }
        public DateTime OrderedAt { get; set; }
        public DateTime ReceivedAt { get; set; }
        public MaterialSupplier? Supplier { get; set; }
    }
} 