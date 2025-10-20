namespace esAPI.Configuration
{
    public class CorsConfig
    {
        public const string SectionName = "Cors";

        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
        public bool AllowCredentials { get; set; } = false;
        public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };
        public string[] AllowedHeaders { get; set; } = { "Content-Type", "Authorization", "Client-Id" };
    }
}
