namespace esAPI.Services
{
    public class PaymentRetryJob : IRetryJob
    {
        public string JobType { get; set; } = "PaymentRetry";
        public int RetryAttempt { get; set; }
        
        public string ToAccountNumber { get; set; } = string.Empty;
        public string ToBankName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
