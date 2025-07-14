namespace esAPI.Services
{
    public interface IRetryJob
    {
        string JobType { get; set; }
        int RetryAttempt { get; set; }
    }
}

