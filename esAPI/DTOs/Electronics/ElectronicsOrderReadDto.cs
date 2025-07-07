namespace esAPI.DTOs.Electronics
{
        public class ElectronicsOrderReadDto
        {
                public int OrderId { get; set; }
                // public int ManufacturerId { get; set; }
                public int RemainingAmount { get; set; }
                public decimal OrderedAt { get; set; }
                public decimal? ProcessedAt { get; set; }
                // public int OrderStatusId { get; set; }
                public string? OrderStatus { get; set; }
                // public string? ManufacturerName { get; set; }
                public int TotalAmount { get; set; }
    }
}
