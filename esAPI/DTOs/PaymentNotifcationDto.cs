using System.Text.Json.Serialization;

namespace esAPI.DTOs
{
    public class PaymentNotificationDto
    {
        public string? TransactionNumber { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public double Timestamp { get; set; }
        public string? Description { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
    }

    public class PaymentRetryEvent
    {
        // The ID of our internal material_order record
        [JsonPropertyName("localOrderId")]
        public int LocalOrderId { get; set; }

        [JsonPropertyName("recipientBankAccount")]
        public required string RecipientBankAccount { get; set; }

        [JsonPropertyName("recipientName")]
        public required string RecipientName { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("reference")]
        public required string Reference { get; set; }
    }
}