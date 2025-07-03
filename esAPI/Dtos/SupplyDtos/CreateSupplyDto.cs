namespace esAPI.Dtos
{
    public class CreateSupplyDto
    {
        public int MaterialId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}

