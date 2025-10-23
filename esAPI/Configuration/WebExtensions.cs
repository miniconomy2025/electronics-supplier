using Microsoft.AspNetCore.Mvc;

namespace esAPI.Configuration
{
    public static class WebExtensions
    {
        public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new()
                {
                    Title = "Electronics Supplier API",
                    Version = "v1",
                    Description = "API for managing electronics supplier simulation and operations"
                });

                // Enable XML comments if available
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        public static IServiceCollection AddControllersWithValidation(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                // Enable automatic model validation
                options.ModelValidatorProviders.Clear();
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                // Customize validation error responses
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToArray();

                    return new BadRequestObjectResult(new
                    {
                        message = "Validation failed",
                        errors
                    });
                };
            });

            return services;
        }
    }
}
