using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("material_supplies")]
    public class MaterialSupply
    {
        [Key]
        [Column("supply_id")]
        public int SupplyId { get; set; }
        [Column("material_id")]
        public int MaterialId { get; set; }
        [Column("received_at")]
        public decimal ReceivedAt { get; set; }
        [Column("processed_at")]
        public decimal ProcessedAt { get; set; }
        [ForeignKey("MaterialId")]
        public Material? Material { get; set; }
    }
} 