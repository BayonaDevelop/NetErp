using Microsoft.Extensions.DependencyInjection;

namespace Commons.AppServices;

public static class RegistrationOfInfrastructureServices<TAssemblyMarker>
{
  /// <summary>
  /// Registra los repositorios de la capa de infraestructura (Infrastructure) en el contenedor de dependencias.<br/>
  /// </summary>
  /// <param name="services"></param>
  /// <param name="lifetime"></param>
  /// <returns></returns>
  public static IServiceCollection RepositoryRegistry(IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
  {
    var infrastructureAssembly = typeof(TAssemblyMarker).Assembly;

    var repositoryTypes = infrastructureAssembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository"));

    foreach (var type in repositoryTypes)
    {
      var mainInterface = type.GetInterfaces().FirstOrDefault(i => i.Name.CompareTo($"I{type.Name}") == 0);
      if (mainInterface != null)
      {
        services.Add(new ServiceDescriptor(mainInterface, type, lifetime));
      }
    }

    return services;
  }
}
