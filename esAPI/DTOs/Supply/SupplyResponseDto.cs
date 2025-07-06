namespace esAPI.DTOs.Supply
{
    public class SupplyDto
    {
        public int SupplyId { get; set; }
        public decimal ReceivedAt { get; set; }
        public decimal? ProcessedAt { get; set; }
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
    }
}