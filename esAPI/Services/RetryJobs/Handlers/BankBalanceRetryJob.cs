namespace esAPI.Services
{
    public class BankBalanceRetryJob : IRetryJob
{
    public string JobType => "BankBalanceRetry";
    public int RetryAttempt { get; set; }
    public int SimulationDay { get; set; }
}

}