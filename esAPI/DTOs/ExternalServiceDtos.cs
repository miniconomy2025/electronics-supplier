using System.Text.Json.Serialization;

namespace esAPI.DTOs;

// --- DTO for Supplier's /raw-materials POST endpoint ---
public class SupplierOrderRequest
{
    [JsonPropertyName("materialName")]
    public required string MaterialName { get; set; }

    [JsonPropertyName("weightQuantity")]
    public int WeightQuantity { get; set; }
}
public class SupplierOrderResponse
{
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("bankAccount")]
    public string? BankAccount { get; set; }

    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }
}

public class LogisticsItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}
public class LogisticsPickupRequest
{
    [JsonPropertyName("originalExternalOrderId")]
    public required string OriginalExternalOrderId { get; set; }

    [JsonPropertyName("originCompany")]
    public required string OriginCompany { get; set; }

    [JsonPropertyName("destinationCompany")]
    public required string DestinationCompany { get; set; }

    [JsonPropertyName("items")]
    public required LogisticsItem[] Items { get; set; }
}
public class LogisticsPickupResponse
{
    [JsonPropertyName("cost")]
    public decimal Cost { get; set; }

    [JsonPropertyName("bulkLogisticsBankAccountNumber")]
    public string? BulkLogisticsBankAccountNumber { get; set; }
}
