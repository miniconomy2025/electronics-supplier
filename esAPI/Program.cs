using Microsoft.EntityFrameworkCore;
using Npgsql;

using esAPI.Data;
using esAPI.Clients;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Configuration;
using esAPI.Middleware;
using Amazon.SQS;

var builder = WebApplication.CreateBuilder(args);

// Configure HTTP clients
builder.Services.AddHttpClient("commercial-bank", client =>
{
    client.BaseAddress = new Uri("https://commercial-bank-api.subspace.site/api");
    client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
});

builder.Services.AddHttpClient("bulk-logistics", client =>
{
    client.BaseAddress = new Uri("https://team7-todo.xyz/api");
    client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
});

builder.Services.AddHttpClient("thoh", client =>
{
    client.BaseAddress = new Uri("https://ec2-13-244-65-62.af-south-1.compute.amazonaws.com");
    client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
});

builder.Services.AddHttpClient("recycler", client =>
{
    client.BaseAddress = new Uri("https://api.recycler.susnet.co.za");
    client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
});

//--------------------------------------------------------------------------

var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection")!
);

var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource)
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddHttpClient(); 

// Ensure all SimulationEngine dependencies are registered for DI
builder.Services.AddScoped<IElectronicsService, ElectronicsService>();
builder.Services.AddScoped<IBulkLogisticsClient, BulkLogisticsClient>();
builder.Services.AddScoped<IMaterialOrderService, MaterialOrderService>();
builder.Services.AddScoped<ISupplyService, SupplyService>();
builder.Services.AddScoped<ICommercialBankClient, CommercialBankClient>();
builder.Services.AddScoped<ThohApiClient>();
builder.Services.AddScoped<RecyclerApiClient>();
builder.Services.AddScoped<IBulkLogisticsClient, BulkLogisticsClient>();

builder.Services.AddScoped<BankAccountService>();
builder.Services.AddScoped<BankService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<SimulationStartupService>();
builder.Services.AddScoped<ElectronicsMachineDetailsService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IMachineAcquisitionService, MachineAcquisitionService>();

builder.Services.AddScoped<IMaterialSourcingService, MaterialSourcingService>();
builder.Services.AddScoped<IMaterialAcquisitionService, MaterialAcquisitionService>();

builder.Services.AddScoped<ISupplierApiClient, RecyclerApiClient>();

builder.Services.AddScoped<IStartupCostCalculator, StartupCostCalculator>();

builder.Services.AddScoped<OrderExpirationService>();
builder.Services.Configure<InventoryConfig>(
    builder.Configuration.GetSection(InventoryConfig.SectionName)
);

builder.Services.AddScoped<SimulationDayOrchestrator>();
builder.Services.AddSingleton<ISimulationStateService, SimulationStateService>();

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
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(2);
        var response = client.GetAsync("http://169.254.169.254/latest/meta-data/iam/security-credentials/").Result;
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}

// builder.Services.AddHostedService<InventoryManagementService>(); // Disabled inventory management system temporarily
builder.Services.AddHostedService<SimulationAutoAdvanceService>();
builder.Services.AddSingleton<OrderExpirationBackgroundService>();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSwagger", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowSwagger");

// Use Client-Id authentication
app.UseClientIdentification();

app.MapControllers();

app.Run();

public partial class Program { }
