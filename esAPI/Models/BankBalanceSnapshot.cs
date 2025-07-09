using System.ComponentModel.DataAnnotations;

namespace esAPI.Models
{
    public class BankBalanceSnapshot
    {
        [Key]
        public int Id { get; set; }
        public int SimulationDay { get; set; }
        public decimal Balance { get; set; }
        public DateTime Timestamp { get; set; }
    }
}