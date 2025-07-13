using System.Security.Cryptography.X509Certificates;

using Microsoft.EntityFrameworkCore;
using Npgsql;

using esAPI.Data;
using esAPI.Clients;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Configuration;
using Amazon.SQS;

var builder = WebApplication.CreateBuilder(args);

// Configure TLS settings
var tlsUtil = new TLSUtil(builder);

tlsUtil.AddSecureHttpClient(builder.Services, "commercial-bank", "https://commercial-bank-api.projects.bbdgrad.com");
tlsUtil.AddSecureHttpClient(builder.Services, "bulk-logistics", "https://bulk-logistics-api.projects.bbdgrad.com/api");
tlsUtil.AddSecureHttpClient(builder.Services, "thoh", "https://thoh-api.projects.bbdgrad.com");
tlsUtil.AddSecureHttpClient(builder.Services, "recycler", "https://recycler-api.projects.bbdgrad.com");

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

builder.Services.AddScoped<IElectronicsService, ElectronicsService>();
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
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IMachineAcquisitionService, MachineAcquisitionService>();

builder.Services.AddScoped<IMaterialSourcingService, MaterialSourcingService>();
builder.Services.AddScoped<IMaterialAcquisitionService, MaterialAcquisitionService>();

builder.Services.AddScoped<IThohMachineApiClient, ThohApiClient>();
builder.Services.AddScoped<ISupplierApiClient, ThohApiClient>();
builder.Services.AddScoped<ISupplierApiClient, RecyclerApiClient>();

builder.Services.AddScoped<IStartupCostCalculator, StartupCostCalculator>();

builder.Services.AddScoped<OrderExpirationService>();
builder.Services.Configure<InventoryConfig>(
    builder.Configuration.GetSection(InventoryConfig.SectionName)
);

builder.Services.AddScoped<SimulationDayOrchestrator>();
builder.Services.AddSingleton<ISimulationStateService, SimulationStateService>();

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddSingleton<RetryQueuePublisher>();
builder.Services.AddHostedService<RetryJobDispatcher>();

// Register all retry handlers
builder.Services.AddScoped<IRetryHandler<BankAccountRetryJob>, BankAccountRetryHandler>();
builder.Services.AddScoped<IRetryHandler<BankBalanceRetryJob>, BankBalanceRetryHandler>();

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

app.Use(async (context, next) =>
{
    // Check for client certificate info from ALB headers
    var clientCertHeader = context.Request.Headers["X-Forwarded-Client-Cert"].FirstOrDefault();
    if (!string.IsNullOrEmpty(clientCertHeader))
    {
        Console.WriteLine("🔧 ALB Client cert header: " + clientCertHeader);
        // Parse the certificate info from the header
        // ALB forwards the certificate in a specific format
    }
    else
    {
        // Fallback to direct connection (for development)
        var cert = await context.Connection.GetClientCertificateAsync();
        if (cert != null)
        {
            Console.WriteLine("Client cert CN: " + cert.GetNameInfo(X509NameType.SimpleName, false));
            Console.WriteLine("Issuer: " + cert.Issuer);
            Console.WriteLine("Thumbprint: " + cert.Thumbprint);
        }
        else
        {
            Console.WriteLine("❌ No client cert received (direct connection).");
        }
    }

    await next();
});

// Use CORS
app.UseCors("AllowSwagger");

app.MapControllers();

app.Run();

public partial class Program { }
