using Microsoft.Extensions.DependencyInjection;

namespace Commons.AppServices;

public static class RegistrationOfOpenApi
{
  /// <summary>
  /// Registra el documento OpenAPI de un servicio sin "servers" apuntando
  /// al Host interno (p.ej. "identity.api:8080") con el que YARP lo alcanza.
  /// Swagger/Scalar se sirven desde el Gateway y usan ese "servers" para el
  /// boton "Try it"; el navegador no puede resolver el Host interno. Un
  /// array vacio no alcanza: el swagger-client embebido en Swagger UI no
  /// siempre cae de vuelta al origen actual y arma una URL relativa mal
  /// formada ("Failed to fetch: URL scheme must be http or https"). Un
  /// servidor relativo explicito ("/") si lo resuelven ambas UI, contra el
  /// origen del Gateway.
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  public static IServiceCollection AddSharedOpenApi(this IServiceCollection services)
  {
    return services.AddOpenApi(options =>
    {
      options.AddDocumentTransformer((document, context, cancellationToken) =>
      {
        document.Servers = [new() { Url = "/" }];
        return Task.CompletedTask;
      });
    });
  }
}
