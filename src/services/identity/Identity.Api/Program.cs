using Commons.AppServices;
using Commons.EndPoints;
using Commons.ExceptionHandlers;
using Commons.I18n;
using Commons.Observability;
using Identity.Api.Security;
using Identity.Application;
using Identity.Application.Mappers;
using Identity.Application.Settings;
using Identity.Infrastructure;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Options;

MappingConfig.RegisterMappings();

var builder = WebApplication.CreateBuilder(args);
string serviceName = builder.Configuration.GetValue<string>("ServiceName")!;

var databaseSettings = builder.Configuration
  .GetSection(nameof(ConnectionStrings))
  .Get<ConnectionStrings>()
  ?? throw new InvalidOperationException($"Missing '{nameof(ConnectionStrings)}' configuration section.");

var jwtSettings = builder.Configuration
  .GetSection(nameof(Jwt))
  .Get<Jwt>()
  ?? throw new InvalidOperationException($"Missing '{nameof(Jwt)}' configuration section.");

builder.AddMultiLanguageSupport();
builder.AddObservability(serviceName);
builder.AddKestrelHardening();

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(Options.Create(databaseSettings));
builder.Services.AddApplication(Options.Create(jwtSettings));
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
  serverOptions.ListenAnyIP(8080);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseObservability();
app.UseLocalization("es-MX", "es-MX", "en-US");
app.UseKestrelHardening();

app.MapOpenApi();

app.MapEndpointsFromAssembly(typeof(Program).Assembly);

app.Run();

public partial class Program { }
