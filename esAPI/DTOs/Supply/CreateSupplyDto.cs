namespace esAPI.DTOs.Supply
{
    public class CreateSupplyDto
    {
        public int MaterialId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}

