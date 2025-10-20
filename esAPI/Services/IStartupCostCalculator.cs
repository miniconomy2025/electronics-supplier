namespace esAPI.Services;

public class StartupCosts
{
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalMachineCost { get; set; }
    public decimal TotalLoanAmount => TotalMaterialCost + TotalMachineCost;
}
