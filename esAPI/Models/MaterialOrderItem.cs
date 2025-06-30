namespace esAPI.Models
{
    public class MaterialOrderItem
    {
        public int ItemId { get; set; }
        public int MaterialId { get; set; }
        public int Amount { get; set; }
        public int OrderId { get; set; }
        public Material? Material { get; set; }
        public MaterialOrder? Order { get; set; }
    }
} 