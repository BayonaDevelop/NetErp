using Commons.AppServices;
using Identity.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class RegistryOfServices
{
  private sealed class AssemblyMarker;

  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

    return RegistrationOfApplicationServices<AssemblyMarker>.AddApplication(services);
  }
}
