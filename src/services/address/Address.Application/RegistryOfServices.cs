using Commons.AppServices;
using Microsoft.Extensions.DependencyInjection;

namespace Address.Application;

public static class RegistryOfServices
{
  private sealed class AssemblyMarker;

  public static IServiceCollection AddApplication(this IServiceCollection services) =>
    RegistrationOfApplicationServices<AssemblyMarker>.AddApplication(services);
}
