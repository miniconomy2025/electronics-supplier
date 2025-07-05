namespace esAPI.Dtos.ElectronicsDto
{
    public class ElectronicsOrderReadDto
    {
        public int OrderId { get; set; }
        public int ManufacturerId { get; set; }
        public int RemainingAmount { get; set; }
        public DateTime OrderedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
