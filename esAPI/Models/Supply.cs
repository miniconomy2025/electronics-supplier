namespace esAPI.Models
{
    public class Supply
    {
        public int SupplyId { get; set; }
        public int MaterialId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Material? Material { get; set; }
    }
} 