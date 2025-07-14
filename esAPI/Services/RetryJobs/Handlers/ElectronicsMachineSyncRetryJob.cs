namespace esAPI.Services
{
    // In Simulation/Tasks or a similar namespace
public class ElectronicsMachineSyncRetryJob : IRetryJob
{
    public string JobType { get; set; } = "ElectronicsMachineSyncRetry";
    public int RetryAttempt { get; set; }
}

}