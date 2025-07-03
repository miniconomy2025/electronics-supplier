using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("phone_manufacturers")]
    public class PhoneManufacturer
    {
        [Key]
        [Column("manufacturer_id")]
        public int ManufacturerId { get; set; }
        [Column("manufacturer_name")]
        public string ManufacturerName { get; set; } = string.Empty;
    }
} 