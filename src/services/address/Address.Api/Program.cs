using Address.Application;
using Address.Infrastructure;
using Address.Infrastructure.Settings;
using Commons.AppServices;
using Commons.EndPoints;
using Commons.ExceptionHandlers;
using Commons.Observability;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

string serviceName = builder.Configuration.GetValue<string>("ServiceName")!;

builder.AddObservability(serviceName);

var databaseSettings = builder.Configuration
  .GetSection(nameof(ConnectionStrings))
  .Get<ConnectionStrings>()
  ?? throw new InvalidOperationException($"Missing '{nameof(ConnectionStrings)}' configuration section.");

builder.Services.AddOpenApi(options =>
{
  // Sin este transformer, el documento incluye un "servers" armado con el
  // Host interno (address.api:8080) que YARP usa para llegar a este
  // servicio, no con el del Gateway. Swagger/Scalar (servidos desde el
  // Gateway) usan ese "servers" para el boton "Try it" y el navegador no
  // puede resolver ese host interno. Un array vacio no es suficiente: el
  // swagger-client embebido en Swagger UI no siempre cae de vuelta al
  // origen actual y arma una URL relativa mal formada ("Failed to fetch:
  // URL scheme must be http or https"). Un servidor relativo explicito
  // ("/") si lo resuelven ambas UI, contra el origen del Gateway.
  options.AddDocumentTransformer((document, context, cancellationToken) =>
  {
    document.Servers = [new() { Url = "/" }];
    return Task.CompletedTask;
  });
});

builder.Services.AddInfrastructure(Options.Create(databaseSettings));
builder.Services.AddApplication();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
  serverOptions.ListenAnyIP(8080);
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseObservability();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseLocalization("es-MX", "es-MX", "en-US");

app.MapHealthChecks("/health").AllowAnonymous();

app.MapEndpointsFromAssembly(typeof(Program).Assembly);

await app.RunAsync(CancellationToken.None).ConfigureAwait(false);
