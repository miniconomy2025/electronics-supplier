namespace esAPI.Dtos.ElectronicsDto
{
    public class ElectronicsOrderUpdateDto
    {
        public int ManufacturerId { get; set; }
        public int RemainingAmount { get; set; }
        public DateTime OrderedAt { get; set; } // If you want to allow setting this
    }
}
