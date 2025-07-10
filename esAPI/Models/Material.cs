using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
        [Table("materials")]
        public class Material
        {
                [Key]
                [Column("material_id")]
                public int MaterialId { get; set; }

                [Column("material_name")]
                public string MaterialName { get; set; } = string.Empty;

                [Column("price_per_kg")]
                public decimal PricePerKg { get; set; }
        }
}