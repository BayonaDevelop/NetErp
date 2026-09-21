using Commons.AppServices;
using Commons.EndPoints;
using Commons.I18n;
using Commons.Observability;
using Identity.Api.Security;
using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Settings;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
string serviceName = builder.Configuration.GetValue<string>("ServiceName")!;
var databaseSettings = builder.Configuration
  .GetSection(nameof(ConnectionStrings))
  .Get<ConnectionStrings>()
  ?? throw new InvalidOperationException($"Missing '{nameof(ConnectionStrings)}' configuration section.");

builder.AddMultiLanguageSupport();
builder.AddObservability(serviceName);
builder.AddKestrelHardening();

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(Options.Create(databaseSettings));
builder.Services.AddApplication();

var app = builder.Build();

app.UseLocalization("es-MX", "es-MX", "en-US");
app.UseKestrelHardening();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.MapEndpointsFromAssembly(typeof(Program).Assembly);

app.Run();
