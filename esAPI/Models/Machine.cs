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
        [Column("output_amount")]
        public int OutputAmount { get; set; }
        [Column("machine_status")]
        public int MachineStatusId { get; set; }
        [Column("order_id")]
        public int OrderId { get; set; }
        [Column("purchased_at")]
        public decimal PurchasedAt { get; set; }
        [Column("received_at")]
        public decimal? ReceivedAt { get; set; }
        [Column("removed_at")]
        public decimal? RemovedAt { get; set; }
        [ForeignKey("MachineStatusId")]
        public MachineStatus? MachineStatus { get; set; }
        [ForeignKey("OrderId")]
        public MachineOrder? MachineOrder { get; set; }
    }
}