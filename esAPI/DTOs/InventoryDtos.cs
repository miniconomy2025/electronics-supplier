using System.Text.Json.Serialization;

namespace esAPI.DTOs;

public class MachineSummaryDto
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("standby")]
    public int Standby { get; set; }

    [JsonPropertyName("inUse")]
    public int InUse { get; set; }

    [JsonPropertyName("broken")]
    public int Broken { get; set; }
}

public class MaterialStockDto
{
    [JsonPropertyName("materialId")]
    public int MaterialId { get; set; }

    [JsonPropertyName("materialName")]
    public required string MaterialName { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public class InventorySummaryDto
{
    [JsonPropertyName("machines")]
    public required MachineSummaryDto Machines { get; set; }

    [JsonPropertyName("materialsInStock")]
    public required List<MaterialStockDto> MaterialsInStock { get; set; }

    [JsonPropertyName("electronicsInStock")]
    public int ElectronicsInStock { get; set; }
}