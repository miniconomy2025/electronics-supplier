using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("electronics")]
    public class Electronic
    {
        [Key]
        [Column("electronic_id")]
        public int ElectronicId { get; set; }
        [Column("produced_at")]
        public DateTime ProducedAt { get; set; }
        [Column("sold_at")]
        public DateTime? SoldAt { get; set; }
    }
} 