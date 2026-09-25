using Commons.AppServices;
using Commons.EndPoints;
using Commons.I18n;
using Commons.Observability;
using Gateway.Endpoints;
using Gateway.Security;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

string serviceName = builder.Configuration.GetValue<string>("ServiceName") ?? string.Empty;
string httpClientName = builder.Configuration.GetValue<string>("HttpClientName") ?? string.Empty;
IConfigurationSection proxyRoutes = builder.Configuration.GetSection("ReverseProxy");

builder.AddObservability(serviceName);
builder.AddKestrelHardening();
builder.AddJwtAuthentication();
builder.AddMultiLanguageSupport();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services
  .AddReverseProxy()
  .LoadFromConfig(proxyRoutes);

builder.Services.AddHttpClient(httpClientName);
LoginEndpoints.AddIdentityApiHttpClient(builder);

var app = builder.Build();

app.UseObservability();
app.UseKestrelHardening();
app.UseLocalization("es-MX", "es-MX", "en-US");
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.RequireAuthorizationForPath("/swagger", JwtAuthenticationExtensions.SwaggerPolicy);
  app.MapEndpointsFromAssembly(typeof(Program).Assembly);

  app.UseSwaggerUI(options =>
  {
    options.SwaggerEndpoint("/openapi/identity.json", "Identity API");
    options.SwaggerEndpoint("/openapi/address.json", "Address API");
    options.RoutePrefix = "swagger";
  });

  app.MapScalarApiReference(options =>
  {
    // Sin routePattern explicito: Scalar arma la URL del documento con su
    // propio OpenApiRoutePattern ("/openapi/{documentName}.json"), asi que
    // el nombre de cada documento debe coincidir con la ruta expuesta por YARP.
    options.AddDocument("identity", "Identity API", isDefault: true);
    options.AddDocument("address", "Address API");
  }).RequireAuthorization(JwtAuthenticationExtensions.SwaggerPolicy);
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapReverseProxy();

app.Run();

public partial class Program { }
