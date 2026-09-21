using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace Commons.AppServices;

/// <summary>
/// Registro generico de configuracion del pipeline HTTP (WebApplication),
/// compartido por todos los servicios.
/// </summary>
public static class AppBuilderRegistry
{
  /// <summary>
  /// Configura la localizacion de requests: idioma por defecto y culturas
  /// soportadas se reciben como codigos de cultura (p. ej. "es-MX", "en-US"),
  /// por lo que cada servicio decide las suyas en vez de tenerlas fijas.
  /// El header Accept-Language se resuelve antes que el resto de proveedores
  /// por defecto (querystring, cookie).
  /// </summary>
  public static WebApplication UseLocalization(this WebApplication app, string defaultCulture, params string[] supportedCultures)
  {
    var cultures = supportedCultures
      .Select(c => new CultureInfo(c))
      .ToList();

    var options = new RequestLocalizationOptions
    {
      DefaultRequestCulture = new RequestCulture(defaultCulture),
      SupportedCultures = cultures,
      SupportedUICultures = cultures
    };

    options.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());

    app.UseRequestLocalization(options);
    return app;
  }
}
