using System.Security.Cryptography.X509Certificates;

using Microsoft.EntityFrameworkCore;
using Npgsql;

using esAPI.Data;
using esAPI.Clients;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure TLS settings
var tlsUtil = new TLSUtil(builder);

tlsUtil.AddSecureHttpClient(builder.Services, "commercial-bank", "https://commercial-bank-api.projects.bbdgrad.com");
tlsUtil.AddSecureHttpClient(builder.Services, "bulk-logistics", "https://bulk-logistics-api.projects.bbdgrad.com");

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

builder.Services.AddScoped<IElectronicsService, ElectronicsService>();
builder.Services.AddScoped<IMaterialOrderService, MaterialOrderService>();
builder.Services.AddScoped<ISupplyService, SupplyService>();
builder.Services.AddScoped<ICommercialBankClient, CommercialBankClient>();
builder.Services.AddScoped<BankAccountService>();
builder.Services.AddScoped<BankService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IMachineAcquisitionService, MachineAcquisitionService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IMaterialAcquisitionService, MaterialAcquisitionService>();
builder.Services.AddScoped<SimulationDayOrchestrator>();
builder.Services.AddScoped<OrderExpirationService>();
builder.Services.AddScoped<SimulatedRecyclerApiClient>();
builder.Services.AddScoped<SimulatedThohApiClient>();
builder.Services.AddScoped<SupplierApiClientFactory>();

builder.Services.Configure<InventoryConfig>(
    builder.Configuration.GetSection(InventoryConfig.SectionName)
);

// Singleton Design Pattern!!
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

app.Use(async (context, next) =>
{
    var cert = await context.Connection.GetClientCertificateAsync();
    if (cert != null)
    {
        Console.WriteLine("Client cert CN: " + cert.GetNameInfo(X509NameType.SimpleName, false));
        Console.WriteLine("Issuer: " + cert.Issuer);
        Console.WriteLine("Thumbprint: " + cert.Thumbprint);
    }
    else
    {
        Console.WriteLine("❌ No client cert received.");
    }

    await next();
});

// Use CORS
app.UseCors("AllowSwagger");

app.MapControllers();

app.Run();

public partial class Program { }