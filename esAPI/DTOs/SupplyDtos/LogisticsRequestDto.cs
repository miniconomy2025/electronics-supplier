namespace esAPI.Dtos
{
    public class LogisticsRequestDto
    {
        public string Id { get; set; } = string.Empty; // Can be orderId or pickupRequestId
        public string Type { get; set; } = string.Empty; // "PICKUP" or "DELIVERY"
        public int Quantity { get; set; }
    }
}
