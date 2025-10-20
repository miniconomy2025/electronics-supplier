using System.Text.Json.Serialization;

namespace esAPI.DTOs;

public class RecyclerOrderItemDto
{
    [JsonPropertyName("rawMaterialName")]
    public required string RawMaterialName { get; set; }

    [JsonPropertyName("quantityInKg")]
    public int QuantityInKg { get; set; }
}

public class RecyclerOrderRequestDto
{
    [JsonPropertyName("companyName")]
    public required string CompanyName { get; set; }

    [JsonPropertyName("orderItems")]
    public List<RecyclerOrderItemDto> OrderItems { get; set; } = new();
}

public class RecyclerMaterialDto
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("availableQuantityInKg")]
    public int AvailableQuantityInKg { get; set; }

    [JsonPropertyName("pricePerKg")]
    public decimal PricePerKg { get; set; }
}

public class RecyclerApiResponseDto
{
    [JsonPropertyName("materials")]
    public List<RecyclerMaterialDto> Materials { get; set; } = [];
}

public class SupplierMaterialInfo
{
    public required string MaterialName { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal PricePerKg { get; set; }
}

public class RecyclerOrderResponseWrapper
{
    [JsonPropertyName("data")]
    public required RecyclerOrderData Data { get; set; }

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timeStamp")]
    public string? TimeStamp { get; set; }
}

public class RecyclerOrderData
{
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("orderStatus")]
    public required RecyclerOrderStatus OrderStatus { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("companyId")]
    public int CompanyId { get; set; }

    [JsonPropertyName("orderExpiresAt")]
    public string? OrderExpiresAt { get; set; }

    [JsonPropertyName("orderItems")]
    public List<RecyclerOrderItemDetail> OrderItems { get; set; } = new();

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }
}

public class RecyclerOrderStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class RecyclerOrderItemDetail
{
    [JsonPropertyName("rawMaterial")]
    public required RecyclerMaterialDetail RawMaterial { get; set; }
    [JsonPropertyName("quantityInKg")]
    public int QuantityInKg { get; set; }
    [JsonPropertyName("pricePerKg")]
    public decimal PricePerKg { get; set; }
}

public class RecyclerMaterialDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("pricePerKg")]
    public decimal PricePerKg { get; set; }
}
