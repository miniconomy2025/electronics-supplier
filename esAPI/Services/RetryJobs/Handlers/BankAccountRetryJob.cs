using System.Text.Json.Serialization;

namespace esAPI.Services
{
    public class BankAccountRetryJob : IRetryJob
    {
        [JsonPropertyName("JobType")]
        public string JobType { get; set; } = "BankAccountRetry";
        public int RetryAttempt { get; set; }
        public int CompanyId { get; set; }
        public string NotificationUrl { get; set; } = string.Empty;
    }
}
