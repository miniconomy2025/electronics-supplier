using System.Text.Json.Serialization;

namespace esAPI.Services
{
    public class LoanRequestRetryJob : IRetryJob
    {
        [JsonPropertyName("JobType")]
        public string JobType { get; set; } = "LoanRequestRetry";
        public int RetryAttempt { get; set; }
        public decimal Amount { get; set; }
    }
}
