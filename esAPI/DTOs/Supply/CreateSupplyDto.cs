namespace esAPI.DTOs.Supply
{
    public class CreateSupplyDto
    {
        public int MaterialId { get; set; }
        public decimal ReceivedAt { get; set; }
        public decimal? ProcessedAt { get; set; }
    }
}

