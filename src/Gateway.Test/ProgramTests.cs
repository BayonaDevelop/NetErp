using System.Diagnostics.CodeAnalysis;
using Gateway.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Test;

/// <summary>
/// Cubre el arranque/composicion de Gateway.Program: lectura de configuracion
/// (Jwt, ReverseProxy), registro del HttpClient hacia Identity.Api y el hecho
/// de que /login, /logout y la proteccion de /swagger solo se mapean en
/// Development (ver el `if (app.Environment.IsDevelopment())` de Program.cs).
/// Usa ConfigureAppConfiguration en vez de variables de entorno de proceso
/// para no dejar estado global entre pruebas.
/// </summary>
[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public sealed class ProgramTests
{
  private const string ValidSigningKey = "unit-test-signing-key-0123456789-please-ignore";

  private static WebApplicationFactory<Program> CreateFactory(string environment = "Testing") =>
    new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
      builder.UseEnvironment(environment);
      builder.ConfigureAppConfiguration((_, config) =>
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Jwt:SigningKey"] = ValidSigningKey
        }));
    });

  [Fact]
  public void Build_WhenConfigurationIsValid_RegistersIdentityApiHttpClientWithConfiguredBaseAddress()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();

    HttpClient client = factory.Services
      .GetRequiredService<IHttpClientFactory>()
      .CreateClient(LoginEndpoints.IdentityHttpClientName);

    Assert.Equal(new Uri("http://identity.api:8080/"), client.BaseAddress);
  }

  [Fact]
  public void Build_WhenConfigurationIsValid_LoadsReverseProxyRoutesAndClustersFromConfiguration()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();

    IProxyConfigProvider configProvider = factory.Services.GetRequiredService<IProxyConfigProvider>();
    IProxyConfig config = configProvider.GetConfig();

    Assert.Contains(config.Routes, r => r.RouteId.CompareTo("identity-login") == 0);
    Assert.Contains(config.Clusters, c => c.ClusterId.CompareTo("identity-cluster") == 0);
  }

  [Fact]
  public void Build_WhenConfigurationIsValid_MapsHealthCheckEndpoint()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();

    IEnumerable<RouteEndpoint> endpoints = factory.Services
      .GetRequiredService<EndpointDataSource>()
      .Endpoints
      .OfType<RouteEndpoint>();

    Assert.Contains(endpoints, e => e.RoutePattern.RawText!.CompareTo("/health") == 0);
  }

  [Fact]
  public void Build_WhenEnvironmentIsDevelopment_MapsLoginAndLogoutEndpoints()
  {
    using WebApplicationFactory<Program> factory = CreateFactory(Environments.Development);

    IEnumerable<RouteEndpoint> endpoints = factory.Services
      .GetRequiredService<EndpointDataSource>()
      .Endpoints
      .OfType<RouteEndpoint>();

    Assert.Contains(endpoints, e => e.RoutePattern.RawText!.CompareTo("/login") == 0);
    Assert.Contains(endpoints, e => e.RoutePattern.RawText!.CompareTo("/logout") == 0);
  }

  [Fact]
  public void Build_WhenEnvironmentIsProduction_DoesNotMapLoginEndpoints()
  {
    using WebApplicationFactory<Program> factory = CreateFactory(Environments.Production);

    IEnumerable<RouteEndpoint> endpoints = factory.Services
      .GetRequiredService<EndpointDataSource>()
      .Endpoints
      .OfType<RouteEndpoint>();

    Assert.DoesNotContain(endpoints, e => e.RoutePattern.RawText!.CompareTo("/login") == 0);
    Assert.DoesNotContain(endpoints, e => e.RoutePattern.RawText!.CompareTo("/logout") == 0);
    Assert.Contains(endpoints, e => e.RoutePattern.RawText!.CompareTo("/health") == 0);
  }
}
