namespace esAPI.Models
{
    public class Machine
    {
        public int MachineId { get; set; }
        public MachineStatus Status { get; set; }
        public float PurchasePrice { get; set; }
        public DateTime PurchasedAt { get; set; }
        public DateTime? SoldAt { get; set; }
    }
}