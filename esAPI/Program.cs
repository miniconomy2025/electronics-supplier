using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using Npgsql;
using esAPI.Clients;
using FactoryApi.Clients;
using esAPI.Services;
using esAPI.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

//---------------------------- TLS Configuration ----------------------------
// var sharedRootCA = new X509Certificate2("../certs/miniconomy-root-ca.crt");
// var commercialBankClientCert = X509Certificate2.CreateFromPemFile("../certs/commercial-bank-client.pfx", "");

// Load other client certificates

// bool ValidateCertificateWithRoot(X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors, X509Certificate2 rootCA)
// {
//     if (errors != SslPolicyErrors.None)
//         return false;

//     chain.ChainPolicy.ExtraStore.Add(rootCA);
//     chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
//     chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

//     var isValid = chain.Build(cert);
//     var actualRoot = chain.ChainElements[^1].Certificate;

//     return isValid && actualRoot.Thumbprint == rootCA.Thumbprint;
// }

// Shared validation logic using the root CA
// Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> CreateServerCertValidator = (message, serverCert, chain, sslPolicyErrors) =>
//     ValidateCertificateWithRoot(serverCert, chain, sslPolicyErrors, sharedRootCA);

// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ConfigureHttpsDefaults(httpsOptions =>
//     {
//         httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

//         // Require client certificates
//         httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;

//         // Validate client cert using shared root
//         httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
//             ValidateCertificateWithRoot(cert, chain, errors, sharedRootCA);
//     });
// });

// Example: Commercial Bank HTTP Client Configuration
// builder.Services.AddHttpClient("commercial-bank", client =>
// {
//     client.BaseAddress = new Uri("https://commercial-bank.com");
// })
// .ConfigurePrimaryHttpMessageHandler(() =>
// {
//     var handler = new HttpClientHandler();
//     handler.ClientCertificates.Add(commercialBankClientCert);

//     handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
//         ValidateCertificateWithRoot(cert, chain, errors, sharedRootCA);
//     return handler;
// });
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
builder.Services.AddScoped<esAPI.Services.IElectronicsService, esAPI.Services.ElectronicsService>();
builder.Services.AddScoped<esAPI.Services.IMaterialOrderService, esAPI.Services.MaterialOrderService>();
builder.Services.AddScoped<esAPI.Services.ISupplyService, esAPI.Services.SupplyService>();
builder.Services.Configure<InventoryConfig>(
    builder.Configuration.GetSection(InventoryConfig.SectionName)
);
builder.Services.AddScoped<SimulatedRecyclerApiClient>();
builder.Services.AddScoped<SimulatedThohApiClient>();
builder.Services.AddScoped<SupplierApiClientFactory>();
builder.Services.AddSingleton<SimulationStateService>();

builder.Services.AddHostedService<InventoryManagementService>();
builder.Services.AddHostedService<SimulationAutoAdvanceService>();

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
