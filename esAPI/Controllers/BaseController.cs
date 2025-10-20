using esAPI.Data;
using esAPI.Models;

using Microsoft.AspNetCore.Mvc;

#nullable enable
namespace esAPI.Controllers
{
    public partial class BaseController(AppDbContext context) : ControllerBase
    {
        protected readonly AppDbContext _context = context;

        /// <summary>
        /// Gets the current company based on the Client-Id header.
        /// This information is populated by the ClientIdentificationMiddleware.
        /// </summary>
        /// <returns>The company associated with the current request, or null if not found.</returns>
        protected Company? GetCurrentCompany()
        {
            return HttpContext.Items["CurrentCompany"] as Company;
        }

        /// <summary>
        /// Gets the Client-Id from the current request.
        /// This information is populated by the ClientIdentificationMiddleware.
        /// </summary>
        /// <returns>The Client-Id header value, or null if not found.</returns>
        protected string? GetClientId()
        {
            return HttpContext.Items["ClientId"] as string;
        }
    }
}
