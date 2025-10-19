namespace esAPI.Services
{
    public interface IClientContext
    {
        int? CompanyId { get; set; }
    }

    public class ClientContext : IClientContext
    {
        public int? CompanyId { get; set; }
    }
}


