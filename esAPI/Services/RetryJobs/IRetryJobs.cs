namespace esAPI.Services
{
    public interface IRetryJob
    {
        string JobType { get; }
        int RetryAttempt { get; set; }
    }
}

