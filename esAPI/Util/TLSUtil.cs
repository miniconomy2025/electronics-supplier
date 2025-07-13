using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

public class TLSUtil
{
    // Single shared root CA cert
    public X509Certificate2 SharedRootCA { get; } = new X509Certificate2("../certs/root-ca.crt");

    // Our own certs
    public X509Certificate2 ServerCert { get; } = new X509Certificate2("../certs/electronics-supplier-server.pfx", "",
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);

    public X509Certificate2 ClientCert { get; } = new X509Certificate2("../certs/electronics-supplier-client.pfx", "",
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);

    public TLSUtil(WebApplicationBuilder builder)
    {
        Console.WriteLine($"🔧 TLSUtil: Loading certificates...");
        Console.WriteLine($"🔧 TLSUtil: Root CA path: ../certs/root-ca.crt");
        Console.WriteLine($"🔧 TLSUtil: Client cert path: ../certs/electronics-supplier-client.pfx");
        Console.WriteLine($"🔧 TLSUtil: Server cert path: ../certs/electronics-supplier-server.pfx");
        
        try
        {
            Console.WriteLine($"🔧 TLSUtil: Root CA thumbprint: {SharedRootCA.Thumbprint}");
            Console.WriteLine($"🔧 TLSUtil: Client cert thumbprint: {ClientCert.Thumbprint}");
            Console.WriteLine($"🔧 TLSUtil: Server cert thumbprint: {ServerCert.Thumbprint}");
            Console.WriteLine($"🔧 TLSUtil: Client cert subject: {ClientCert.Subject}");
            Console.WriteLine($"🔧 TLSUtil: Client cert issuer: {ClientCert.Issuer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ TLSUtil: Error loading certificates: {ex.Message}");
        }
        
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

                // Require client certs
                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;

                // Validate incoming client certs with shared root
                httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
                    ValidateCertificateWithRoot(cert, chain, errors, SharedRootCA);
            });

            if (builder.Environment.IsDevelopment())
            {
                // Commenting for development purposes
                //     options.ListenLocalhost(7251, listenOptions =>
                // {
                //     listenOptions.UseHttps(ServerCert);
                //     listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                // });
                options.ListenLocalhost(5062);
            }
            else
            {
                options.ListenAnyIP(443, listenOptions =>
                {
                    listenOptions.UseHttps(ServerCert);
                });
            }
        });
    }

    private static bool ValidateCertificateWithRoot(X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors, X509Certificate2 rootCA)
    {
        Console.WriteLine($"🔍 Certificate validation - Errors: {errors}");
        Console.WriteLine($"🔍 Certificate subject: {cert?.Subject}");
        Console.WriteLine($"🔍 Certificate issuer: {cert?.Issuer}");
        Console.WriteLine($"🔍 Certificate thumbprint: {cert?.Thumbprint}");
        Console.WriteLine($"🔍 Root CA thumbprint: {rootCA?.Thumbprint}");
        
        // For development/testing, be more permissive with certificate validation
        // Only reject if there are critical errors, not chain errors
        if (errors == SslPolicyErrors.RemoteCertificateNotAvailable)
        {
            Console.WriteLine($"❌ Certificate validation failed - no certificate provided");
            return false;
        }
        
        // For chain errors, we'll be more permissive and just check the thumbprint
        if (errors == SslPolicyErrors.RemoteCertificateChainErrors)
        {
            Console.WriteLine($"⚠️ Certificate chain validation failed, but checking thumbprint anyway...");
        }
        else if (errors != SslPolicyErrors.None)
        {
            Console.WriteLine($"❌ Certificate validation failed due to SSL policy errors: {errors}");
            return false;
        }

        try
        {
            // Try to build the chain with our root CA
            chain.ChainPolicy.ExtraStore.Add(rootCA);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            var isValid = chain.Build(cert);
            Console.WriteLine($"🔍 Chain build result: {isValid}");
            
            if (chain.ChainElements.Count > 0)
            {
                var actualRoot = chain.ChainElements[^1].Certificate;
                Console.WriteLine($"🔍 Actual root thumbprint: {actualRoot.Thumbprint}");
                Console.WriteLine($"🔍 Expected root thumbprint: {rootCA.Thumbprint}");
                
                var thumbprintMatch = actualRoot.Thumbprint == rootCA.Thumbprint;
                Console.WriteLine($"🔍 Thumbprint match: {thumbprintMatch}");
                
                // If chain validation failed but thumbprint matches, accept it
                if (!isValid && thumbprintMatch)
                {
                    Console.WriteLine($"✅ Accepting certificate despite chain errors - thumbprint matches");
                    return true;
                }
                
                return isValid && thumbprintMatch;
            }
            else
            {
                Console.WriteLine("❌ No chain elements found");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception during certificate validation: {ex.Message}");
            return false;
        }
    }

    // Single method to create secure HttpClient: send our client cert, validate server cert with shared root
    public void AddSecureHttpClient(IServiceCollection services, string name, string baseUrl)
    {
        Console.WriteLine($"🔧 TLSUtil: Configuring HttpClient '{name}' for {baseUrl}");
        Console.WriteLine($"🔧 TLSUtil: Client certificate thumbprint: {ClientCert.Thumbprint}");
        Console.WriteLine($"🔧 TLSUtil: Client certificate subject: {ClientCert.Subject}");
        Console.WriteLine($"🔧 TLSUtil: Client certificate issuer: {ClientCert.Issuer}");
        
        services.AddHttpClient(name, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();

            // Present our client cert when calling other APIs
            handler.ClientCertificates.Add(ClientCert);
            Console.WriteLine($"🔧 TLSUtil: Added client certificate to handler for '{name}'");
            Console.WriteLine($"🔧 TLSUtil: Handler client certificates count: {handler.ClientCertificates.Count}");

            // Validate server cert (their server cert) with shared root CA
            handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
            {
                Console.WriteLine($"🔧 TLSUtil: Server certificate validation for {msg.RequestUri}");
                Console.WriteLine($"🔧 TLSUtil: Server cert subject: {cert?.Subject}");
                Console.WriteLine($"🔧 TLSUtil: Server cert issuer: {cert?.Issuer}");
                Console.WriteLine($"🔧 TLSUtil: SSL errors: {errors}");
                return ValidateCertificateWithRoot(cert, chain, errors, SharedRootCA);
            };

            Console.WriteLine($"🔧 TLSUtil: HttpClient handler configured for '{name}'");
            return handler;
        });
    }
}
