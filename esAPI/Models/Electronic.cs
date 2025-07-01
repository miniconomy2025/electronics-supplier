using System.ComponentModel.DataAnnotations;

namespace esAPI.Models
{
    public class Electronic
    {
        [Key]
        public int ElectronicId { get; set; }
        public DateTime ProducedAt { get; set; }
        public DateTime? SoldAt { get; set; }
    }
} 