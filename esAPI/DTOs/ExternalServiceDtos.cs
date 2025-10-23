using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;

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
    [JsonPropertyName("itemName")]
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
    [JsonPropertyName("pickupRequestId")]
    public int PickupRequestId { get; set; }

    [JsonPropertyName("cost")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal Cost { get; set; }

    [JsonPropertyName("paymentReferenceId")]
    public string? PaymentReferenceId { get; set; }

    [JsonPropertyName("bulkLogisticsBankAccountNumber")]
    public string? BulkLogisticsBankAccountNumber { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("statusCheckUrl")]
    public string? StatusCheckUrl { get; set; }
}

public class StringToDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            throw new JsonException($"Unable to convert \"{stringValue}\" to decimal");
        }
        
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }
        
        throw new JsonException($"Unexpected token type {reader.TokenType} when parsing decimal");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
