using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("supplies")]
    public class Supply
    {
        [Key]
        [Column("supply_id")]
        public int SupplyId { get; set; }
        [Column("material_id")]
        public int MaterialId { get; set; }
        [Column("received_at")]
        public DateTime ReceivedAt { get; set; }
        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }
        [ForeignKey("MaterialId")]
        public Material? Material { get; set; }
    }
} 