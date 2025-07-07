using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using Npgsql;
using esAPI.Clients;
using FactoryApi.Clients;
using esAPI.Services;
using esAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<esAPI.Services.IElectronicsService, esAPI.Services.ElectronicsService>();
builder.Services.AddScoped<esAPI.Services.IMaterialOrderService, esAPI.Services.MaterialOrderService>();
builder.Services.AddScoped<esAPI.Services.ISupplyService, esAPI.Services.SupplyService>();
builder.Services.Configure<InventoryConfig>(
    builder.Configuration.GetSection(InventoryConfig.SectionName)
);
builder.Services.AddScoped<SimulatedRecyclerApiClient>();
builder.Services.AddScoped<SimulatedThohApiClient>();
builder.Services.AddScoped<SupplierApiClientFactory>();

builder.Services.AddHostedService<InventoryManagementService>();

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

app.MapControllers();

app.Run();

public partial class Program { }
