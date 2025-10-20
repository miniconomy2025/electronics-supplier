namespace esAPI.DTOs.Startup
{
    public class StartupCosts
    {
        public decimal TotalMaterialCost { get; set; }
        public decimal TotalMachineCost { get; set; }
        public decimal TotalLoanAmount => TotalMaterialCost + TotalMachineCost;
    }

    public class StartupPlan
    {
        public required string MachineName { get; set; }
        public decimal MachineCost { get; set; }
        public decimal MaterialsCost { get; set; }
        public decimal TotalCost => MachineCost + MaterialsCost;
    }
}
