namespace esAPI.DTOs.SupplyDtos
{
    public class LogisticsItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class LogisticsRequestDto
    {
        public int Id { get; set; } // Order ID or Pickup Request ID (changed from string to int)
        public string Type { get; set; } = string.Empty; // "PICKUP", "DELIVERY", or "MACHINE"
        public List<LogisticsItemDto> Items { get; set; } = [];
    }
}
