using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Net.Security;
using System.Security.Authentication;

namespace Gateway.Security;

public static class KestrelHardeningExtensions
{
  public static WebApplicationBuilder AddKestrelHardening(this WebApplicationBuilder builder)
  {
    var requireClientCert = builder.Configuration.GetValue("Kestrel:RequireClientCertificate", false);

    builder.WebHost.ConfigureKestrel(options =>
    {
      options.AddServerHeader = false;

      options.ConfigureHttpsDefaults(https =>
      {
        https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        if (requireClientCert)
        {
          https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
          https.ClientCertificateValidation = (cert, chain, errors) =>
              errors == SslPolicyErrors.None;
        }
      });

      options.Limits.MaxRequestBodySize = 1 * 1024 * 1024;
      options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
    });

    builder.Services.AddHsts(o =>
    {
      o.Preload = true;
      o.IncludeSubDomains = true;
      o.MaxAge = TimeSpan.FromDays(365);
    });

    return builder;
  }

  public static WebApplication UseKestrelHardening(this WebApplication app)
  {
    if (!app.Environment.IsDevelopment())
    {
      app.UseHsts();
    }

    return app;
  }
}
