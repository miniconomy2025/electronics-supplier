namespace esAPI.DTOs.Orders
{
    public class ElectronicsOrderResponse
    {
        public int OrderId { get; set; }
        public int Quantity { get; set; }
        public decimal AmountDue { get; set; }
        public string BankNumber { get; set; } = string.Empty;
    }
}
