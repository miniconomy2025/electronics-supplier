namespace esAPI.Services
{
    public class StartupPlan
    {
        public required string MachineName { get; set; }
        public decimal MachineCost { get; set; }
        public decimal MaterialsCost { get; set; }
        public decimal TotalCost => MachineCost + MaterialsCost;
    }
}


