namespace esAPI.Models
{
    public class ElectronicsOrder
    {
        public int OrderId { get; set; }
        public int ManufacturerId { get; set; }
        public int Amount { get; set; }
        public DateTime OrderedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public PhoneManufacturer? Manufacturer { get; set; }
    }
} 