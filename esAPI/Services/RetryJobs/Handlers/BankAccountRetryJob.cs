namespace esAPI.Services
{
    public class BankAccountRetryJob : IRetryJob
    {
        public string JobType => "BankAccountRetry";
        public int RetryAttempt { get; set; }
        public int CompanyId { get; set; }
        public string NotificationUrl { get; set; }
    }

}