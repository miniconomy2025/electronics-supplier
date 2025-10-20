using System.ComponentModel.DataAnnotations;

namespace esAPI.DTOs
{
    public class PaymentNotificationDto
    {
        [Required]
        public string TransactionNumber { get; set; } = string.Empty;
        
        [Required]
        public string Status { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [Required]
        public double Timestamp { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public string From { get; set; } = string.Empty;
        
        [Required]
        public string To { get; set; } = string.Empty;
    }
}