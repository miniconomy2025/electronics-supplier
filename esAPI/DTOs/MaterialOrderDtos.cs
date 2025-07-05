using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace esAPI.DTOs;

public class MaterialOrderResponse
{
    public int OrderId { get; set; }
    public int SupplierId { get; set; }
    public required string SupplierName { get; set; }
    public int MaterialId { get; set; }
    public required string MaterialName { get; set; }
    public int RemainingAmount { get; set; }
    public DateTime OrderedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public required string Status { get; set; }
}

public class CreateMaterialOrderRequest
{
    [Required]
    [JsonPropertyName("supplier_id")]
    public int SupplierId { get; set; }

    [Required]
    [JsonPropertyName("material_id")]
    public int MaterialId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Remaining amount must be a positive integer.")]
    [JsonPropertyName("remaining_amount")]
    public int RemainingAmount { get; set; }
}

public class UpdateMaterialOrderRequest
{
    public int? SupplierId { get; set; }
    public int? MaterialId { get; set; }
    public int? RemainingAmount { get; set; }
    public DateTime? OrderedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
}
