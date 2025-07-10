using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using esAPI.Services;

namespace esAPI.DTOs.MaterialOrder
{

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
                public int? ExternalOrderId { get; set; }
                public int MaterialId { get; set; }
                public required string MaterialName { get; set; }
                public int RemainingAmount { get; set; }
                public required string Status { get; set; }
                public decimal OrderedAt { get; set; }
                public decimal? ReceivedAt { get; set; }

                // Simulation timestamp conversions
                public DateTime OrderedAtSimTimestamp => OrderedAt.ToCanonicalTime();
                public DateTime? ReceivedAtSimTimestamp => ReceivedAt?.ToCanonicalTime();
        }


        public class CreateMaterialOrderItemRequest
        {
                [Required]
                [JsonPropertyName("material_id")]
                public int MaterialId { get; set; }

                [Required]
                [JsonPropertyName("amount")]
                [Range(1, int.MaxValue, ErrorMessage = "Amount must be a positive integer.")]
                public int Amount { get; set; }
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
                [Range(1, int.MaxValue, ErrorMessage = "Amount must be a positive integer.")]
                [JsonPropertyName("amount")]
                public int Amount { get; set; }
        }

        public class UpdateMaterialOrderRequest
        {
                public int? SupplierId { get; set; }
                public decimal? OrderedAt { get; set; }
                public decimal? ReceivedAt { get; set; }

                // Simulation timestamp conversions
                public DateTime? OrderedAtSimTimestamp => OrderedAt?.ToCanonicalTime();
                public DateTime? ReceivedAtSimTimestamp => ReceivedAt?.ToCanonicalTime();
        }
}

