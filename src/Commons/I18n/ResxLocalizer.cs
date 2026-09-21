using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Commons.I18n;

/// <summary>
/// Envoltorio simple sobre <see cref="ResourceManager"/> para leer mensajes de
/// log localizados desde archivos .resx embebidos, sin depender de clases
/// Designer autogeneradas (mas portable entre editores/CI).
///
/// Cada servicio crea UNA instancia apuntando a su propio recurso:
///
///   var textos = new ResxLocalizer("ServiceA.Resources.LogMessages", typeof(Program).Assembly);
///   textos.Get("PedidoCreado", id, monto);
///
/// El idioma se resuelve segun <see cref="CultureInfo.CurrentUICulture"/>,
/// que ASP.NET Core setea por request via UseRequestLocalization
/// (ver <see cref="LocalizationExtensions"/>).
/// </summary>
public sealed class ResxLocalizer(string resourceBaseName, Assembly assembly)
{
  private readonly ResourceManager _resourceManager = new (resourceBaseName, assembly);
  private readonly string _resourceBaseName = resourceBaseName;

  /// <summary>
  /// Devuelve el texto localizado para <paramref name="key"/> en el idioma
  /// actual del request (CurrentUICulture), formateado con los argumentos
  /// dados. Si la clave no existe en ningun resx, devuelve un marcador
  /// visible en vez de lanzar excepcion (para no tumbar el servicio por un
  /// texto faltante).
  /// </summary>
  public string Get(string key, params object[] args)
  {
    var culture = CultureInfo.CurrentUICulture;
    string? template;

    try
    {
      template = _resourceManager.GetString(key, culture);
    }
    catch (MissingManifestResourceException)
    {
      return $"[[missing-resx:{_resourceBaseName}]]";
    }

    if (template is null)
    {
      return $"[[missing-key:{key}]]";
    }

    return args.Length > 0 ? string.Format(culture, template, args) : template;
  }
}
