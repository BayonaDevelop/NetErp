using Microsoft.AspNetCore.Routing;
using System.Reflection;

namespace Commons.EndPoints;

public static class EndpointRouteBuilderExtensions
{
  public static IEndpointRouteBuilder MapEndpointsFromAssembly(this IEndpointRouteBuilder app, Assembly assembly)
  {
    var endpointTypes = assembly.GetTypes()
        .Where(t => typeof(IEndpoint).IsAssignableFrom(t)
                 && !t.IsInterface
                 && !t.IsAbstract);

    foreach (var type in endpointTypes)
    {
      // Invoca el método estático MapEndpoints de cada clase encontrada
      type.GetMethod(nameof(IEndpoint.MapEndpoints))?.Invoke(null, [app]);
    }

    return app;
  }
}

