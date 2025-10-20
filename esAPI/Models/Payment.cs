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
        
        [Required]
        [Column("transaction_number")]
        public string TransactionNumber { get; set; } = string.Empty;
        
        [Required]
        [Column("status")]
        public string Status { get; set; } = string.Empty;
        
        [Column("amount")]
        public decimal Amount { get; set; }
        
        [Column("timestamp")]
        public double Timestamp { get; set; }
        
        [Column("description")]
        public string? Description { get; set; }
        
        [Required]
        [Column("from_account")]
        public string FromAccount { get; set; } = string.Empty;
        
        [Required]
        [Column("to_account")]
        public string ToAccount { get; set; } = string.Empty;
        
        [Column("order_id")]
        public int? OrderId { get; set; }
    }
}