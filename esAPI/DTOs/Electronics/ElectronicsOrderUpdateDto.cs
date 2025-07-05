namespace esAPI.DTOs.Electronics
{
    public class ElectronicsOrderUpdateDto
    {
        public int? ManufacturerId { get; set; }
        public int? RemainingAmount { get; set; }
        public decimal? OrderedAt { get; set; }
        public decimal? ProcessedAt { get; set; }
        public int? OrderStatusId { get; set; }
    }
}
