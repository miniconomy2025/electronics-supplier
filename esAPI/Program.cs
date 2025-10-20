using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using esAPI.Configuration;
using esAPI.Middleware;
using esAPI.Services;
using Amazon.SQS;

var builder = WebApplication.CreateBuilder(args);

// Configure services using extension methods to reduce coupling
builder.Services.AddExternalApiClients(builder.Configuration);
builder.Services.AddDatabaseContext(builder.Configuration);

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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

// Add controllers with validation
builder.Services.AddControllers(options =>
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
            errors = errors
        });
    };
});

// Add application services
builder.Services.AddApplicationServices();
builder.Services.AddHealthChecksConfiguration(builder.Configuration);
builder.Services.AddBackgroundServices();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddHttpClient();

// Configure service options
builder.Services.Configure<InventoryConfig>(
    builder.Configuration.GetSection(InventoryConfig.SectionName)
);
builder.Services.Configure<ExternalApiConfig>(
    builder.Configuration.GetSection(ExternalApiConfig.SectionName)
);

// Optional AWS services with graceful fallback
try
{
    builder.Services.AddAwsServices();
    Console.WriteLine("✅ AWS services configured");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ AWS services not available: {ex.Message}");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
// Add global exception handling first
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS - secure policy for production, permissive for development
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentCors");
}
else
{
    app.UseCors("SecureCorsPolicy");
}

// Use Client-Id authentication
app.UseClientIdentification();

app.MapControllers();

app.Run();

public partial class Program { }
