namespace esAPI.DTOs
{
    public class PaymentNotificationDto
    {
        public string? TransactionNumber { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public double Timestamp { get; set; }
        public string? Description { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
    }
}