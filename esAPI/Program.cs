using Microsoft.EntityFrameworkCore;
using Npgsql;

using esAPI.Data;
using esAPI.Clients;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Configuration;
using esAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure named HttpClients
builder.Services.AddHttpClient("commercial-bank", client =>
{
    client.BaseAddress = new Uri("https://commercial-bank-api.projects.bbdgrad.com");
    client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
})
// Basic resiliency policies (timeouts handled by HttpClient default; retry few times)
;
builder.Services.AddHttpClient("bulk-logistics", client =>
{
    client.BaseAddress = new Uri("https://bulk-logistics-api.projects.bbdgrad.com");
    client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
})
;
builder.Services.AddHttpClient("thoh", client =>
{
    client.BaseAddress = new Uri("https://thoh-api.projects.bbdgrad.com");
    client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
})
;
builder.Services.AddHttpClient("recycler", client =>
{
    client.BaseAddress = new Uri("https://recycler-api.projects.bbdgrad.com");
    client.DefaultRequestHeaders.Add("Client-Id", "electronics-supplier");
})
;

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
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IMachineAcquisitionService, MachineAcquisitionService>();

builder.Services.AddScoped<IMaterialSourcingService, MaterialSourcingService>();
builder.Services.AddScoped<IMaterialAcquisitionService, MaterialAcquisitionService>();

builder.Services.AddScoped<IThohMachineApiClient, ThohApiClient>();
builder.Services.AddScoped<ISupplierApiClient, ThohApiClient>();
builder.Services.AddScoped<ISupplierApiClient, RecyclerApiClient>();

builder.Services.AddScoped<IStartupCostCalculator, StartupCostCalculator>();

builder.Services.AddScoped<IClientContext, ClientContext>();

builder.Services.AddScoped<OrderExpirationService>();
builder.Services.Configure<InventoryConfig>(
    builder.Configuration.GetSection(InventoryConfig.SectionName)
);

builder.Services.Configure<BankConfig>(
    builder.Configuration.GetSection(BankConfig.SectionName)
);

builder.Services.AddScoped<SimulationDayOrchestrator>();
builder.Services.AddSingleton<ISimulationStateService, SimulationStateService>();

builder.Services.AddHostedService<InventoryManagementService>();
builder.Services.AddHostedService<SimulationAutoAdvanceService>();
builder.Services.AddHostedService<OrderExpirationBackgroundService>();

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

app.UseMiddleware<ClientIdMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }
