namespace esAPI.Models
{
    public class MachineDto
    {
        public int MachineId { get; set; }
        public required string Status { get; set; } // status name, e.g. "WORKING"
        public float PurchasePrice { get; set; }
        public DateTime PurchasedAt { get; set; }
    }
}