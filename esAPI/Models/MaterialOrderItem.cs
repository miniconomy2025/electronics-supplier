using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    public class MaterialOrderItem
    {
        [Key]
        [Column("item_id")]
        public int ItemId { get; set; }
        [Column("material_id")]
        public int MaterialId { get; set; }
        [Column("amount")]
        public int Amount { get; set; }
        [Column("order_id")]
        public int OrderId { get; set; }
        public Material? Material { get; set; }
        public MaterialOrder? Order { get; set; }
    }
}