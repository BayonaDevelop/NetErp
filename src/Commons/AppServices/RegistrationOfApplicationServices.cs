using Commons.ExceptionHandlers;
using Commons.Mediator;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Reflection;

namespace Commons.AppServices;

public static class RegistrationOfApplicationServices<TAssemblyMarker>
{
  private static void RegisterOpenGenericInterfaces(IServiceCollection services, Assembly assembly, Type openGenericInterface, ServiceLifetime lifetime)
  {
    var types = assembly
      .GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract);

    foreach (var type in types)
    {
      var interfaces = type.GetInterfaces()
        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

      foreach (var @interface in interfaces)
        services.Add(new ServiceDescriptor(@interface, type, lifetime));
    }
  }

  /// <summary>
  /// Registra los servicios de la capa de aplicación (Application) en el contenedor de dependencias.<br/>
  /// * dispatcher<br/>
  /// * handlers<br/>
  /// * validadores<br/>
  /// * decoradores de excepciones/validacion<br/>
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
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
}
