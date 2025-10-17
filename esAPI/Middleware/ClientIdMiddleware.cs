using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using esAPI.Services;

namespace esAPI.Middleware
{
    public class ClientIdMiddleware
    {
        private readonly RequestDelegate _next;

        public const string ClientIdHeaderName = "X-Client-ID";
        public const string HttpContextItemKey = "ClientId";

        public ClientIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IClientContext clientContext)
        {
            int? parsedId = null;

            if (context.Request.Headers.TryGetValue(ClientIdHeaderName, out var values))
            {
                var raw = values.ToString()?.Trim();
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out var companyId))
                {
                    parsedId = companyId;
                }
            }

            clientContext.CompanyId = parsedId;
            context.Items[HttpContextItemKey] = parsedId;

            await _next(context);
        }
    }
}


