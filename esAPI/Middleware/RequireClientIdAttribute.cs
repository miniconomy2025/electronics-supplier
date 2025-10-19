using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using esAPI.Services;

namespace esAPI.Middleware
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireClientIdAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var clientContext = context.HttpContext.RequestServices.GetService(typeof(IClientContext)) as IClientContext;
            if (clientContext?.CompanyId == null)
            {
                context.Result = new BadRequestObjectResult(new { error = "Missing or invalid Client-Id header." });
                return;
            }

            await next();
        }
    }
}


