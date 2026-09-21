using Commons.ExceptionHandlers;
using Commons.Mediator;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Reflection;

namespace Commons.AppServices;

/// <summary>
/// Registro generico de la capa Application (dispatcher, handlers, validadores
/// y decoradores de excepciones/validacion) y de la capa Infrastructure
/// (repositorios por convencion), compartido por todos los servicios.
/// TAssemblyMarker es cualquier tipo publico definido en el assembly a
/// escanear (p. ej. la propia clase RegistryOfServices de ese proyecto).
/// </summary>
public static class ApplicationServiceRegistry<TAssemblyMarker>
{
  private static void RegisterOpenGenericInterfaces(IServiceCollection services, Assembly assembly, Type openGenericInterface, ServiceLifetime lifetime)
  {
    var types = assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract);

    foreach (var type in types)
    {
      var interfaces = type.GetInterfaces()
          .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

      foreach (var @interface in interfaces)
      {
        services.Add(new ServiceDescriptor(@interface, type, lifetime));
      }
    }
  }

  public static IServiceCollection AddApplication(IServiceCollection services)
  {
    var applicationAssembly = typeof(TAssemblyMarker).Assembly;

    services.AddScoped<IDispatcher, Dispatcher>();

    // Escanear IQueryHandler<,> y ICommandHandler<,>
    RegisterOpenGenericInterfaces(services, applicationAssembly, typeof(IQueryHandler<,>), ServiceLifetime.Scoped);
    RegisterOpenGenericInterfaces(services, applicationAssembly, typeof(ICommandHandler<,>), ServiceLifetime.Scoped);

    services.AddValidatorsFromAssembly(applicationAssembly);

    // TryDecorate no lanza si aun no hay ningun handler registrado del tipo
    // correspondiente; se aplica en cuanto el proyecto agregue uno.
    services.TryDecorate(typeof(ICommandHandler<,>), typeof(CommandUnhandledExceptionHandler<,>));
    services.TryDecorate(typeof(ICommandHandler<,>), typeof(CommandValidationExceptionHandler<,>));
    services.TryDecorate(typeof(IQueryHandler<,>), typeof(QueryUnhandledExceptionHandler<,>));
    services.TryDecorate(typeof(IQueryHandler<,>), typeof(QueryValidationExceptionHandler<,>));

    return services;
  }

  /// <summary>
  /// Escanea el assembly de TAssemblyMarker por convencion de nombres: toda
  /// clase "XxxRepository" se registra contra su interfaz "IXxxRepository".
  /// </summary>
  public static IServiceCollection RegisterRepositories(IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
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
