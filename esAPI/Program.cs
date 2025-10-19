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

// Use CORS
app.UseCors("AllowSwagger");

// Use Client-Id authentication
app.UseClientIdentification();

app.MapControllers();

app.Run();

public partial class Program { }
