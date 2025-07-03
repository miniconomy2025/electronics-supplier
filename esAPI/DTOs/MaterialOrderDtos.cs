using System.ComponentModel.DataAnnotations;

namespace esAPI.DTOs;


public class MaterialOrderItemResponse
{
    public int MaterialId { get; set; }
    public required string MaterialName { get; set; }
    public int Amount { get; set; }
}

public class MaterialOrderResponse
{
    public int OrderId { get; set; }
    public int SupplierId { get; set; }
    public required string SupplierName { get; set; }
    public DateTime OrderedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public required string Status { get; set; }
    public List<MaterialOrderItemResponse> Items { get; set; } = new();
}


public class CreateMaterialOrderItemRequest
{
    [Required]
    public int MaterialId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be a positive integer.")]
    public int Amount { get; set; }
}

public class CreateMaterialOrderRequest
{
    [Required]
    public int SupplierId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<CreateMaterialOrderItemRequest> Items { get; set; } = new();
}

public class UpdateMaterialOrderRequest
{
    public int? SupplierId { get; set; }
    public DateTime? OrderedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
}