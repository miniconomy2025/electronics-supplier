namespace esAPI.Services
{
    public class RecyclerMaterialsFetchRetryJob : IRetryJob
    {
        public string JobType { get; set; } = "RecyclerMaterialsFetchRetry";
        public int RetryAttempt { get; set; } = 0;
        // You could add more properties if needed, like timestamps, etc.
    }
}
