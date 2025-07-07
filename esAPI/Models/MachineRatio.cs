using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("machine_ratios")]
    public class MachineRatio
    {
        [Key]
        [Column("ratio_id")]
        public int RatioId { get; set; }
        
        [Column("material_id")]
        public int MaterialId { get; set; }
        
        [Column("ratio")]
        public int Ratio { get; set; }
        
        [ForeignKey("MaterialId")]
        public Material? Material { get; set; }
    }
} 