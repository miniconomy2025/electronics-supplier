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
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("available_quantity_in_kg")]
    public int AvailableQuantityInKg { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
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