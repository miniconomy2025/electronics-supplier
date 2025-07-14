namespace esAPI.Services
{
    public class RecyclerOrderRetryJob : IRetryJob
    {
        public string JobType { get; set; } = "RecyclerOrderRetry";
        public int RetryAttempt { get; set; }

        public string MaterialName { get; set; } = string.Empty;
        public int QuantityInKg { get; set; }

        // Add any other fields you need for the retry job
    }
}
