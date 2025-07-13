using System.Text.Json.Serialization;

namespace esAPI.Services
{
    public class BankBalanceRetryJob : IRetryJob
    {
        [JsonPropertyName("JobType")]
        public string JobType { get; set; } = "BankBalanceRetry";
        public int RetryAttempt { get; set; }
        public int SimulationDay { get; set; }
    }
}