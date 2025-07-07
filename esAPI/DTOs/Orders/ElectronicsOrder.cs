namespace esAPI.DTOs.Orders
{
    public class ElectronicsOrder
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal OrderedAt { get; set; }
        public int TotalAmount { get; set; }
        public int RemainingAmount { get; set; }
    }
} 