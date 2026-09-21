using Commons.Mediator;
using Identity.Application.Settings;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Test;

/// <summary>
/// Cubre el proceso de arranque/composicion de Identity.Api.Program: lectura y
/// validacion de configuracion (ConnectionStrings/Jwt) y registro de servicios
/// en el contenedor de DI. No ejercita logica de negocio real (no hay SQL
/// Server disponible en el entorno de pruebas).
/// </summary>
public sealed class ProgramTests
{
  private const string ValidConnectionString = "Server=(local);Database=IdentityTestDb;Trusted_Connection=True;TrustServerCertificate=True;";
  private const string ValidSigningKey = "unit-test-signing-key-0123456789-please-ignore";

  private static WebApplicationFactory<Program> CreateFactory(
    string? connectionString = ValidConnectionString,
    string? signingKey = ValidSigningKey)
  {
    SetOrRemoveEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    SetOrRemoveEnvironmentVariable("ConnectionStrings__Default", connectionString);
    SetOrRemoveEnvironmentVariable("Jwt__SigningKey", signingKey);

    return new WebApplicationFactory<Program>();
  }

  private static void SetOrRemoveEnvironmentVariable(string name, string? value) =>
    Environment.SetEnvironmentVariable(name, value);

  [Fact]
  public void Build_WhenConnectionStringsSectionIsMissing_Throws()
  {
    using WebApplicationFactory<Program> factory = CreateFactory(connectionString: null);

    Exception exception = Assert.ThrowsAny<Exception>(() => factory.Services);

    Assert.Contains("ConnectionStrings", FullMessage(exception));
  }

  [Fact]
  public void Build_WhenConfigurationIsValid_RegistersJwtOptionsFromConfiguration()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();

    Jwt jwt = factory.Services.GetRequiredService<IOptions<Jwt>>().Value;

    Assert.Equal(ValidSigningKey, jwt.SigningKey);
    Assert.Equal("netErp.identity", jwt.Issuer);
    Assert.Equal("netErp.clients", jwt.Audience);
    Assert.Equal(60, jwt.DurationInMinutes);
  }

  [Fact]
  public void Build_WhenConfigurationIsValid_RegistersApplicationAndInfrastructureServices()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();
    using IServiceScope scope = factory.Services.CreateScope();

    Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDispatcher>());
    Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserRepository>());
    Assert.NotNull(factory.Services.GetRequiredService<IPasswordHasher<User>>());
  }

  [Fact]
  public void Build_WhenConfigurationIsValid_MapsAuthenticationRoutes()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();

    IEnumerable<RouteEndpoint> endpoints = factory.Services
      .GetRequiredService<EndpointDataSource>()
      .Endpoints
      .OfType<RouteEndpoint>();

    Assert.Contains(endpoints, e => e.RoutePattern.RawText!.CompareTo("api/v1/auth/login") == 0);
    Assert.Contains(endpoints, e => e.RoutePattern.RawText!.CompareTo("api/v1/auth/create-user") == 0);
  }

  private static string FullMessage(Exception exception) =>
    string.Join(" | ", Unwrap(exception).Select(e => e.Message));

  private static IEnumerable<Exception> Unwrap(Exception exception)
  {
    for (Exception? current = exception; current is not null; current = current.InnerException)
      yield return current;
  }
}
