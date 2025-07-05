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
        public decimal ProducedAt { get; set; }
        [Column("electronics_status")]
        public int ElectronicsStatusId { get; set; }
        [Column("sold_at")]
        public decimal? SoldAt { get; set; }
        [ForeignKey("ElectronicsStatusId")]
        public ElectronicsStatus? ElectronicsStatus { get; set; }
    }
} 