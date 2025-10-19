using esAPI.Data;
using esAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Middleware;

public class ClientIdentificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ClientIdentificationMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip middleware for certain paths like Swagger, health checks, etc.
        // Also skip for all GET requests as they are purely informational
        if (ShouldSkipClientValidation(context.Request.Path, context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check for Client-Id header
        if (!context.Request.Headers.TryGetValue("Client-Id", out var clientIdValues) || 
            string.IsNullOrWhiteSpace(clientIdValues.FirstOrDefault()))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing or empty Client-Id header");
            return;
        }

        var clientId = clientIdValues.First()!;

        // Look up the client in the database
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyName == clientId);
        
        if (company == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync($"Unknown client: {clientId}");
            return;
        }

        // Store the company information in HttpContext for controllers to use
        context.Items["CurrentCompany"] = company;
        context.Items["ClientId"] = clientId;

        await _next(context);
    }

    private static bool ShouldSkipClientValidation(PathString path, string method)
    {
        var pathValue = path.Value?.ToLower() ?? string.Empty;
        
        // Skip for all GET requests as they are purely informational
        if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // Skip for POST /simulation endpoint
        if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) && 
            pathValue.StartsWith("/simulation"))
        {
            return true;
        }
        
        return pathValue.StartsWith("/swagger") ||
               pathValue.StartsWith("/health") ||
               pathValue.StartsWith("/api/docs") ||
               pathValue.StartsWith("/payments") ||  // External payment notifications
               pathValue == "/" ||
               pathValue == "/favicon.ico";
    }
}

public static class ClientIdentificationMiddlewareExtensions
{
    public static IApplicationBuilder UseClientIdentification(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ClientIdentificationMiddleware>();
    }
}