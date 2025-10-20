namespace esAPI.Services
{
    public interface IRetryHandler<T> where T : IRetryJob
    {
        Task<bool> HandleAsync(T job, CancellationToken token);
    }
}

