using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace esAPI.Models
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        [Column("payment_id")]
        public int PaymentId { get; set; }
        [Column("transaction_number")]
        public string? TransactionNumber { get; set; }
        [Column("status")]
        public string? Status { get; set; }
        [Column("amount")]
        public decimal Amount { get; set; }
        [Column("timestamp")]
        public double Timestamp { get; set; }
        [Column("description")]
        public string? Description { get; set; }
        [Column("from_account")]
        public string? FromAccount { get; set; }
        [Column("to_account")]
        public string? ToAccount { get; set; }
        [Column("order_id")]
        public int? OrderId { get; set; }
    }
} 