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

        [Column("purchase_price")]
        public float PurchasePrice { get; set; }

        [Column("machine_status")]
        public int MachineStatusId { get; set; }

        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("purchased_at", TypeName = "numeric(1000,2)")]
        public decimal PurchasedAt { get; set; }

        [Column("received_at", TypeName = "numeric(1000,2)")]
        public decimal? ReceivedAt { get; set; }

        [Column("removed_at", TypeName = "numeric(1000,2)")]
        public decimal? RemovedAt { get; set; }

        [ForeignKey("MachineStatusId")]
        public MachineStatus? MachineStatus { get; set; }

        [ForeignKey("OrderId")]
        public MachineOrder? MachineOrder { get; set; }
    }
}
