using System.Diagnostics.CodeAnalysis;
using Gateway.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.Test.Security;

/// <summary>
/// UseHsts solo agrega el header Strict-Transport-Security en respuestas
/// HTTPS, asi que las pruebas de UseKestrelHardening fuerzan el esquema
/// https en el HttpClient de TestServer (TestServer no usa Kestrel real,
/// asi que ConfigureKestrel/ConfigureHttpsDefaults no son observables aqui).
/// Se usa un host distinto de localhost/127.0.0.1 porque HstsMiddleware
/// excluye explicitamente esos hosts para no bloquear el desarrollo local.
/// </summary>
[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public sealed class KestrelHardeningExtensionsTests
{
  [Fact]
  public void AddKestrelHardening_RegistersHstsOptionsWithExpectedValues()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();

    builder.AddKestrelHardening();

    using WebApplication app = builder.Build();
    HstsOptions options = app.Services.GetRequiredService<IOptions<HstsOptions>>().Value;

    Assert.True(options.Preload);
    Assert.True(options.IncludeSubDomains);
    Assert.Equal(TimeSpan.FromDays(365), options.MaxAge);
  }

  private static async Task<(WebApplication App, HttpClient Client)> CreateAppAsync(string environmentName)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();
    builder.AddKestrelHardening();

    WebApplication app = builder.Build();
    app.UseKestrelHardening();
    app.MapGet("/", () => Results.Ok());

    await app.StartAsync(CancellationToken.None).ConfigureAwait(false);

    HttpClient client = app.GetTestClient();
    client.BaseAddress = new Uri("https://gateway.tests.local/");
    return (app, client);
  }

  [Fact]
  public async Task UseKestrelHardening_WhenEnvironmentIsDevelopment_DoesNotAddHstsHeader()
  {
    (WebApplication app, HttpClient client) = await CreateAppAsync(Environments.Development);
    await using (app)
    {
      HttpResponseMessage response = await client.GetAsync("/");

      Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }
  }

  [Fact]
  public async Task UseKestrelHardening_WhenEnvironmentIsProduction_AddsHstsHeader()
  {
    (WebApplication app, HttpClient client) = await CreateAppAsync(Environments.Production);
    await using (app)
    {
      HttpResponseMessage response = await client.GetAsync("/");

      Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }
  }
}
