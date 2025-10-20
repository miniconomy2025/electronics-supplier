using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("bank_balance_snapshots")]
    public class BankBalanceSnapshot
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("simulation_day")]
        public int SimulationDay { get; set; }
        [Column("balance")]
        public double Balance { get; set; }
        [Column("timestamp", TypeName = "numeric(1000,3)")]
        public decimal Timestamp { get; set; }
    }
}
