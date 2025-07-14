using System.Text.Json.Serialization;

public class BankNotificationRequest { [JsonPropertyName("notification_url")] public required string NotificationUrl { get; set; } }
public class BankLoanRequest { [JsonPropertyName("amount")] public decimal Amount { get; set; } }
public class BankPaymentRequest
{
    [JsonPropertyName("to_account_number")] public required string ToAccountNumber { get; set; }
    [JsonPropertyName("to_bank_name")] public required string ToBankName { get; set; }
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("description")] public required string Description { get; set; }
}

public class BankBalanceResponse { [JsonPropertyName("balance")] public decimal Balance { get; set; } }
public class BankAccountResponse { [JsonPropertyName("account_number")] public string? AccountNumber { get; set; } }
public class BankLoanResponse
{
    [JsonPropertyName("loan_number")] public string? LoanNumber { get; set; }

    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("error")] public string? Error { get; set; }

    [JsonPropertyName("amount_remaining")] public double? AmountRemaining { get; set; }
}
public class BankPaymentResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("transaction_number")] public string? TransactionNumber { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}
public class BankNotifyResponse { [JsonPropertyName("success")] public bool Success { get; set; } }