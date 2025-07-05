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
        [Column("manufacturer_id")]
        public int ManufacturerId { get; set; }
        [Column("remaining_amount")]
        public int RemainingAmount { get; set; }
        [Column("ordered_at")]
        public DateTime OrderedAt { get; set; }
        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }
        [ForeignKey("ManufacturerId")]
        public PhoneManufacturer? Manufacturer { get; set; }
    }
} 