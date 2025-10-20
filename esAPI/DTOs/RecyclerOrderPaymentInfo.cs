namespace esAPI.DTOs
{
    /// <summary>
    /// Information about a recycler order payment
    /// </summary>
    public class RecyclerOrderPaymentInfo
    {
        public string MaterialName { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public decimal Total { get; set; }
        public string? AccountNumber { get; set; }
    }
}
