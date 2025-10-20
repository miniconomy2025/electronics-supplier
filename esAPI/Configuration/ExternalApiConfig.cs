namespace esAPI.Configuration
{
    public class ExternalApiConfig
    {
        public const string SectionName = "ExternalApis";

        public string CommercialBank { get; set; } = string.Empty;
        public string BulkLogistics { get; set; } = string.Empty;
        public string THOH { get; set; } = string.Empty;
        public string Recycler { get; set; } = string.Empty;
        public string ClientId { get; set; } = "electronics-supplier";
    }
}
