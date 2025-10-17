using System.Text.Json.Serialization;

namespace esAPI.DTOs
{
    public class ThohMaterialInfo
    {
        [JsonPropertyName("rawMaterialName")]
        public required string RawMaterialName { get; set; }

        [JsonPropertyName("quantityAvailable")]
        public int QuantityAvailable { get; set; }

        [JsonPropertyName("pricePerKg")]
        public decimal PricePerKg { get; set; }
    }
}


