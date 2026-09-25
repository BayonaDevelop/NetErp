using Commons.AppServices;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure;

public static class RegistryOfServices
{
  private sealed class AssemblyMarker;

  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IOptions<ConnectionStrings> databaseSettings)
  {
    string connectionString = databaseSettings.Value.Default;

    services.AddDbContext<SqlServerDbContext>((serviceProvider, options) 
      => { options.UseSqlServer(connectionString).EnableDetailedErrors(); });

    RegistrationOfInfrastructureServices<AssemblyMarker>.RepositoryRegistry(services);

    return services;
  }
}
