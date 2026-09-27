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

builder.Services.AddSharedOpenApi();

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
