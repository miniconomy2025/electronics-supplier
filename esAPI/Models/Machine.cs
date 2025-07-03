using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    public class Machine
    {
        [Key]
        [Column("machine_id")]
        public int MachineId { get; set; }
        [Required]
        [Column("status")]
        public MachineStatus Status { get; set; }

        [Required]
        [Column("purchase_price")]
        public float PurchasePrice { get; set; }
        [Required]
        [Column("purchased_at")]
        public DateTime PurchasedAt { get; set; }
        [Column("sold_at")]
        public DateTime? SoldAt { get; set; }
    }
}