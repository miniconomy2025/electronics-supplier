using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("machines")]
    public class Machine
    {
        [Key]
        [Column("machine_id")]
        public int MachineId { get; set; }
        [Column("status_id")]
        public int StatusId { get; set; }
        [Column("purchase_price")]
        public float PurchasePrice { get; set; }
        [Column("purchased_at")]
        public DateTime PurchasedAt { get; set; }
    }
}