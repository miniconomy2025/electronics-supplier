namespace esAPI.DTOs.Supply
{
    public class SupplyDto
    {
        public int SupplyId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
    }

}