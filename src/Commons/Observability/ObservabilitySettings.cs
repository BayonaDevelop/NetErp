namespace Commons.Observability;

/// <summary>
/// Configuracion del OpenTelemetry Collector compartida por todos los
/// servicios. Se enlaza desde la seccion "Observability" de appsettings.
/// </summary>
public sealed class ObservabilitySettings
{
  public const string SectionName = "Observability";
  public string OtlpEndpoint { get; set; } = "http://localhost:4317";

  public string ServiceVersion { get; set; } = "1.0.0";
}
