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
        public decimal Balance { get; set; }
        [Column("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}