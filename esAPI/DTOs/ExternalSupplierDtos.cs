using System.Text.Json.Serialization;

namespace esAPI.DTOs;

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
    public int MaterialId { get; set; }
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
}