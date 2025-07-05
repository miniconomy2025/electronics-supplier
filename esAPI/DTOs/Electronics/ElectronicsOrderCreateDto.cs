namespace esAPI.DTOs.Electronics
{
    public class ElectronicsOrderCreateDto
    {
        public int ManufacturerId { get; set; }
        public int RemainingAmount { get; set; }
        public int? OrderStatusId { get; set; }
        public decimal? OrderedAt { get; set; }
    }
}
