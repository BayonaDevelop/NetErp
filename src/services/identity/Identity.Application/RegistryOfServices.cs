using Commons.AppServices;
using Identity.Application.Settings;
using Identity.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Application;

public static class RegistryOfServices
{
  private sealed class AssemblyMarker;

  public static IServiceCollection AddApplication(this IServiceCollection services, IOptions<Jwt> jwtSettings)
  {
    services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
    services.AddSingleton(jwtSettings);

    return RegistrationOfApplicationServices<AssemblyMarker>.AddApplication(services);
  }
}
