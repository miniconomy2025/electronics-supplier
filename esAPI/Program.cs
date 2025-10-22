using esAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure console logging with colors
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure all services using extension methods to reduce coupling
builder.Services
    .AddExternalApiClients(builder.Configuration)
    .AddDatabaseContext(builder.Configuration)
    .AddApiDocumentation()
    .AddControllersWithValidation()
    .AddApplicationServices()
    .AddHealthChecksConfiguration(builder.Configuration)
    .AddBackgroundServices()
    .AddCorsConfiguration(builder.Configuration)
    .AddServiceOptions(builder.Configuration)
    .AddHttpClient()
    .AddAwsServicesConditionally(builder.Environment);

var app = builder.Build();

// Configure the HTTP request pipeline using extension method
app.ConfigureRequestPipeline();

app.Run();

public partial class Program { }
