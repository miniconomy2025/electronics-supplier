using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

using esAPI.Data;
using esAPI.Clients;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;
using esAPI.Configuration;
using esAPI.Middleware;
using esAPI.Simulation;
using Amazon.SQS;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure External API settings
var externalApiConfig = new ExternalApiConfig();
builder.Configuration.GetSection(ExternalApiConfig.SectionName).Bind(externalApiConfig);

// Validate configuration
if (string.IsNullOrEmpty(externalApiConfig.CommercialBank) ||
    string.IsNullOrEmpty(externalApiConfig.BulkLogistics) ||
    string.IsNullOrEmpty(externalApiConfig.THOH) ||
    string.IsNullOrEmpty(externalApiConfig.Recycler))
{
    throw new InvalidOperationException("External API configuration is incomplete. Please check appsettings.json or environment variables.");
}

// Log the configured endpoints (without sensitive info)
Console.WriteLine("🔗 External API Configuration:");
Console.WriteLine($"  Commercial Bank: {externalApiConfig.CommercialBank}");
Console.WriteLine($"  Bulk Logistics: {externalApiConfig.BulkLogistics}");
Console.WriteLine($"  THOH: {externalApiConfig.THOH}");
Console.WriteLine($"  Recycler: {externalApiConfig.Recycler}");
Console.WriteLine($"  Client ID: {externalApiConfig.ClientId}");

// Configure HTTP clients using configuration
builder.Services.AddHttpClient("commercial-bank", client =>
{
    client.BaseAddress = new Uri(externalApiConfig.CommercialBank);
    client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
});

builder.Services.AddHttpClient("bulk-logistics", client =>
{
    client.BaseAddress = new Uri(externalApiConfig.BulkLogistics);
    client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
});

builder.Services.AddHttpClient("thoh", client =>
{
    client.BaseAddress = new Uri(externalApiConfig.THOH);
    client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
});

builder.Services.AddHttpClient("recycler", client =>
{
    client.BaseAddress = new Uri(externalApiConfig.Recycler);
    client.DefaultRequestHeaders.Add("Client-Id", externalApiConfig.ClientId);
});

//--------------------------------------------------------------------------

// Build database connection string with environment variable support
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
connectionString += dbPassword;

Console.WriteLine($"🗄️ Database: Connecting to {connectionString.Replace(dbPassword, "***")}");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource)
);

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

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"));

builder.Services.AddHttpClient();

// API Clients
builder.Services.AddScoped<ICommercialBankClient, CommercialBankClient>();
builder.Services.AddScoped<IBulkLogisticsClient, BulkLogisticsClient>();
builder.Services.AddScoped<IThohApiClient, ThohApiClient>();
builder.Services.AddScoped<IRecyclerApiClient, RecyclerApiClient>();
builder.Services.AddScoped<ISupplierApiClient, RecyclerApiClient>();

// Core Services
builder.Services.AddScoped<IElectronicsService, ElectronicsService>();
builder.Services.AddScoped<IMaterialOrderService, MaterialOrderService>();
builder.Services.AddScoped<ISupplyService, SupplyService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IMachineAcquisitionService, MachineAcquisitionService>();
builder.Services.AddScoped<IMaterialSourcingService, MaterialSourcingService>();
builder.Services.AddScoped<IMaterialAcquisitionService, MaterialAcquisitionService>();
builder.Services.AddScoped<esAPI.Interfaces.Services.IStartupCostCalculator, StartupCostCalculator>();

// Business Services  
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<SimulationStartupService>();
builder.Services.AddScoped<ElectronicsMachineDetailsService>();
builder.Services.AddScoped<OrderExpirationService>();
builder.Services.AddScoped<SimulationDayOrchestrator>();
builder.Services.AddSingleton<ISimulationStateService, SimulationStateService>();

// Simulation Engine Services
builder.Services.AddScoped<ISimulationStartupService, SimulationDayStartupService>();
builder.Services.AddScoped<IMaterialOrderingService, MaterialOrderingService>();
builder.Services.AddScoped<IMachineManagementService, MachineManagementService>();
builder.Services.AddScoped<ISimulationDayService, SimulationDayService>();
builder.Services.AddScoped<SimulationEngine>();

// Configuration
builder.Services.Configure<InventoryConfig>(
    builder.Configuration.GetSection(InventoryConfig.SectionName)
);
builder.Services.Configure<ExternalApiConfig>(
    builder.Configuration.GetSection(ExternalApiConfig.SectionName)
);

// Configure AWS services only if credentials are available
var awsServicesEnabled = false;
try
{
    // Check if we have AWS configuration
    var queueUrl = builder.Configuration["Retry:QueueUrl"];
    var region = builder.Configuration["AWS:Region"];

    if (!string.IsNullOrEmpty(queueUrl) && !string.IsNullOrEmpty(region))
    {
        // Try to check for AWS credentials before registering services
        var credentialsAvailable = CheckAWSCredentialsAvailable();

        if (credentialsAvailable)
        {
            builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
            builder.Services.AddAWSService<IAmazonSQS>();
            builder.Services.AddSingleton<RetryQueuePublisher>();
            builder.Services.AddHostedService<RetryJobDispatcher>();

            // Register all retry handlers
            builder.Services.AddScoped<IRetryHandler<BankAccountRetryJob>, BankAccountRetryHandler>();
            builder.Services.AddScoped<IRetryHandler<BankBalanceRetryJob>, BankBalanceRetryHandler>();

            awsServicesEnabled = true;
            Console.WriteLine("✅ AWS SQS services configured successfully");
        }
        else
        {
            Console.WriteLine("⚠️ AWS credentials not available, running without retry services");
        }
    }
    else
    {
        Console.WriteLine("⚠️ AWS SQS configuration not found, running without retry services");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ AWS services not available, running without retry functionality: {ex.Message}");
}

// Register null implementation if AWS services are not enabled
if (!awsServicesEnabled)
{
    builder.Services.AddSingleton<RetryQueuePublisher>(provider => null!);
}

static bool CheckAWSCredentialsAvailable()
{
    // Check environment variables
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
    {
        return true;
    }

    // Check if running on EC2 with IAM role
    try
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
        var response = client.GetAsync("http://169.254.169.254/latest/meta-data/iam/security-credentials/").Result;
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}

// Background Services
// builder.Services.AddHostedService<InventoryManagementService>(); // Disabled inventory management system temporarily
builder.Services.AddHostedService<SimulationAutoAdvanceService>();
builder.Services.AddHostedService<OrderExpirationBackgroundService>();

// Configure CORS
var corsConfig = new CorsConfig();
builder.Configuration.GetSection(CorsConfig.SectionName).Bind(corsConfig);

builder.Services.AddCors(options =>
{
    options.AddPolicy("SecureCorsPolicy", policy =>
    {
        var origins = corsConfig.AllowedOrigins.Length > 0
            ? corsConfig.AllowedOrigins
            : new[] { "http://localhost:3000", "https://localhost:7000" }; // Fallback for development

        policy.WithOrigins(origins)
              .WithMethods(corsConfig.AllowedMethods)
              .WithHeaders(corsConfig.AllowedHeaders);

        if (corsConfig.AllowCredentials)
        {
            policy.AllowCredentials();
        }
    });

    // Keep a permissive policy for development/Swagger only
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("DevelopmentCors", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
});

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
