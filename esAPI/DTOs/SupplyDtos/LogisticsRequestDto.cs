namespace esAPI.Dtos
{
    public class LogisticsItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    } 

    public class LogisticsRequestDto
    {
        public string Id { get; set; } = string.Empty; // Order ID or Pickup Request ID
        public string Type { get; set; } = string.Empty; // "PICKUP", "DELIVERY", or "MACHINE"
        public List<LogisticsItemDto> Items { get; set; } = [];
    }
}
