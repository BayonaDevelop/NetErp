using Address.Infrastructure.Data;
using Address.Infrastructure.Settings;
using Commons.AppServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Address.Infrastructure;

public static class RegistryOfServices
{
  private sealed class AssemblyMarker;

  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IOptions<ConnectionStrings> databaseSettings)
  {
    string connectionString = databaseSettings.Value.Default;

    services.AddDbContext<DatabaseContext>((serviceProvider, options) =>
    {
      options.UseSqlServer(connectionString).EnableDetailedErrors();
    });

    RegistrationOfInfrastructureServices<AssemblyMarker>.RepositoryRegistry(services);

    return services;
  }
}
