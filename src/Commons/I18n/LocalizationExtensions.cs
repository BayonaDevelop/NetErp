using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Commons.I18n;

/// <summary>
/// Conecta la resolucion de idioma por request (espanol/ingles) para todos
/// los servicios. El idioma se puede elegir de dos formas (en este orden):
///   1. Querystring: GET /pedidos?culture=en
///   2. Header estandar: Accept-Language: en-US,en;q=0.9
/// Si ninguna coincide, se usa <see cref="DefaultCulture"/> (es).
/// </summary>
public static class LocalizationExtensions
{
  public const string DefaultCulture = "es";
  public static readonly string[] SupportedCultures = ["es", "en"];

  public static WebApplicationBuilder AddMultiLanguageSupport(this WebApplicationBuilder builder)
  {
    builder.Services.AddLocalization();
    return builder;
  }

  public static WebApplication UseMultiLanguageSupport(this WebApplication app)
  {
    var cultures = SupportedCultures.Select(c => new CultureInfo(c)).ToList();

    var options = new RequestLocalizationOptions
    {
      DefaultRequestCulture = new RequestCulture(DefaultCulture),
      SupportedCultures = cultures,
      SupportedUICultures = cultures,
      // Orden de resolucion: querystring "culture" -> Accept-Language.
      // Se fijan explicitamente para no incluir el CookieRequestCultureProvider
      // que RequestLocalizationOptions agrega por defecto.
      RequestCultureProviders =
      [
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
      ]
    };

    app.UseRequestLocalization(options);
    return app;
  }
}
