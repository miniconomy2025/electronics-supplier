using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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