using System.ComponentModel.DataAnnotations;

namespace esAPI.Models
{
    public class PhoneManufacturer
    {
        [Key]
        public int ManufacturerId { get; set; }
        public string ManufacturerName { get; set; } = string.Empty;
    }
} 