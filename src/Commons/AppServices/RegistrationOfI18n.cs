using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace Commons.AppServices;

public static class RegistrationOfI18n
{
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
