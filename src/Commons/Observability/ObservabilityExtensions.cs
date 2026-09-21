using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.OpenTelemetry;

namespace Commons.Observability;

/// <summary>
/// Logging distribuido (Serilog) y trazado/metricas (OpenTelemetry SDK) para
/// todos los servicios, ambos apuntando al mismo OpenTelemetry Collector via
/// OTLP/gRPC. El Collector reenvia a los backends configurados (Seq, Jaeger,
/// Prometheus, etc.); los servicios nunca hablan directamente con ellos.
///
/// Los logs de Serilog se correlacionan con las trazas mediante
/// Enrich.WithSpan(), que agrega TraceId/SpanId al contexto de log activo.
/// </summary>
public static class ObservabilityExtensions
{
  public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, string serviceName)
  {
    var settings = builder.Configuration
      .GetSection(ObservabilitySettings.SectionName)
      .Get<ObservabilitySettings>() ?? new ObservabilitySettings();

    var otlpEndpoint = new Uri(settings.OtlpEndpoint);

    builder.Host.UseSerilog((ctx, services, configuration) => configuration
      .ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .Enrich.WithEnvironmentName()
      .Enrich.WithSpan()
      .Enrich.WithProperty("service.name", serviceName)
      .WriteTo.Console()
      .WriteTo.OpenTelemetry(otlp =>
      {
        otlp.Endpoint = settings.OtlpEndpoint;
        otlp.Protocol = OtlpProtocol.Grpc;
        otlp.ResourceAttributes = new Dictionary<string, object>
        {
          ["service.name"] = serviceName,
          ["service.version"] = settings.ServiceVersion
        };
      }));

    builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource.AddService(
        serviceName: serviceName,
        serviceVersion: settings.ServiceVersion,
        serviceInstanceId: Environment.MachineName))
      .WithTracing(tracing => tracing
        // AlwaysOnSampler explicito: se quiere el 100% de las trazas
        // (sin muestreo) independientemente de OTEL_TRACES_SAMPLER u
        // otra configuracion externa que pudiera bajar la tasa.
        .SetSampler(new AlwaysOnSampler())
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(otlp => otlp.Endpoint = otlpEndpoint))
      .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(otlp => otlp.Endpoint = otlpEndpoint));

    return builder;
  }

  public static WebApplication UseObservability(this WebApplication app)
  {
    app.UseSerilogRequestLogging();
    return app;
  }
}
