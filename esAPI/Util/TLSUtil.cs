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
        if (errors == SslPolicyErrors.RemoteCertificateNotAvailable)
        {
            return false;
        }
        
        if (errors == SslPolicyErrors.RemoteCertificateChainErrors)
        {
            Console.WriteLine($"⚠️ Certificate chain validation failed, but checking thumbprint anyway...");
        }
        else if (errors != SslPolicyErrors.None)
        {
            return false;
        }

        try
        {
            // Try to build the chain with our root CA
            chain.ChainPolicy.ExtraStore.Add(rootCA);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            var isValid = chain.Build(cert);
            
            if (chain.ChainElements.Count > 0)
            {
                var actualRoot = chain.ChainElements[^1].Certificate;
                
                var thumbprintMatch = actualRoot.Thumbprint == rootCA.Thumbprint;
                
                // If chain validation failed but thumbprint matches, accept it
                if (!isValid && thumbprintMatch)
                {
                    return true;
                }
                
                return isValid && thumbprintMatch;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    // Single method to create secure HttpClient: send our client cert, validate server cert with shared root
    public void AddSecureHttpClient(IServiceCollection services, string name, string baseUrl)
    {
        services.AddHttpClient(name, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();

            // Present our client cert when calling other APIs
            handler.ClientCertificates.Add(ClientCert);

            // Validate server cert (their server cert) with shared root CA
            handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
            {
                return ValidateCertificateWithRoot(cert, chain, errors, SharedRootCA);
            };

            return handler;
        });
    }
}
