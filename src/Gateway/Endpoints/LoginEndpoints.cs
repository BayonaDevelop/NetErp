using System.Net;
using Commons.EndPoints;
using Gateway.Security;

namespace Gateway.Endpoints;

public class LoginEndpoints : IEndpoint
{
  public const string IdentityHttpClientName = "identity-api";

  private const string SwaggerReturnUrl = "/swagger/index.html";
  private const string ScalarReturnUrl = "/scalar/identity";

  public static WebApplicationBuilder AddIdentityApiHttpClient(WebApplicationBuilder builder)
  {
    string address = builder.Configuration
      .GetValue<string>("ReverseProxy:Clusters:identity-cluster:Destinations:identity-api-destination1:Address")
      ?? throw new InvalidOperationException("Missing 'identity-cluster' destination address in ReverseProxy configuration.");

    builder.Services.AddHttpClient(IdentityHttpClientName, client => client.BaseAddress = new Uri(address));

    return builder;
  }

  public static void MapEndpoints(IEndpointRouteBuilder app)
  {
    app.MapGet(
      "/login",
      (string? returnUrl, string? error) =>
        Results.Content(BuildLoginPage(returnUrl ?? SwaggerReturnUrl, error is not null), "text/html")
    );

    app.MapPost(
      "/login",
      async (
        HttpRequest request,
        IHttpClientFactory httpClientFactory,
        ILogger<LoginEndpoints> logger,
        CancellationToken cancellationToken) =>
      {
        IFormCollection form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        string returnUrl = form["returnUrl"].FirstOrDefault() ?? SwaggerReturnUrl;
        string successReturnUrl = form["destination"].FirstOrDefault() == "scalar" ? ScalarReturnUrl : SwaggerReturnUrl;

        if (!long.TryParse(form["companyId"], out long companyId))
          return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");

        string email = form["email"].ToString();
        string password = form["password"].ToString();

        HttpClient client = httpClientFactory.CreateClient(IdentityHttpClientName);

        HttpResponseMessage response = await client
          .PostAsJsonAsync("api/v1/auth/login", new { companyId, email, password }, cancellationToken)
          .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
          string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
          logger.LogWarning(
            "Login rechazado por Identity.Api ({StatusCode}) para {Email}/company {CompanyId}: {Body}",
            (int)response.StatusCode, email, companyId, body);
          return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        LoginResult? result = await response.Content
          .ReadFromJsonAsync<LoginResult>(cancellationToken)
          .ConfigureAwait(false);

        if (result is null || string.IsNullOrEmpty(result.AccessToken))
        {
          logger.LogWarning(
            "Identity.Api devolvio 200 sin AccessToken para {Email}/company {CompanyId}.", email, companyId);
          return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        request.HttpContext.Response.Cookies.Append(
          JwtAuthenticationExtensions.SwaggerCookieName,
          result.AccessToken,
          new CookieOptions
          {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = request.IsHttps,
            Path = "/",
            MaxAge = TimeSpan.FromSeconds(result.ExpiresIn)
          }
        );

        return Results.Redirect(successReturnUrl);
      }
    );

    app.MapGet(
      "/logout",
      (HttpContext http) =>
      {
        http.Response.Cookies.Delete(JwtAuthenticationExtensions.SwaggerCookieName);
        return Results.Redirect("/login");
      }
    );
  }

  private const string LoginPageTemplate = """
    <!DOCTYPE html>
    <html lang="es">
    <head>
      <meta charset="utf-8" />
      <title>Iniciar sesion</title>
      <style>
        body { font-family: system-ui, sans-serif; max-width: 360px; margin: 80px auto; }
        label { display: block; margin-top: 12px; }
        input, select { width: 100%; padding: 8px; box-sizing: border-box; }
        button { margin-top: 20px; width: 100%; padding: 10px; }
      </style>
    </head>
    <body>
      <h2>Acceso a documentacion API</h2>
      __ERROR__
      <form method="post" action="/login">
        <input type="hidden" name="returnUrl" value="__RETURN_URL__" />
        <label>Compania (Id)
          <input type="number" name="companyId" required />
        </label>
        <label>Email
          <input type="email" name="email" required />
        </label>
        <label>Password
          <input type="password" name="password" required />
        </label>
        <label>Documentacion
          <select name="destination">
            <option value="swagger" __SWAGGER_SELECTED__>Swagger</option>
            <option value="scalar" __SCALAR_SELECTED__>Scalar</option>
          </select>
        </label>
        <button type="submit">Entrar</button>
      </form>
    </body>
    </html>
    """;

  private static string BuildLoginPage(string returnUrl, bool showError)
  {
    string errorHtml = showError
      ? "<p style=\"color:#b00020\">Credenciales invalidas o sin permiso de acceso.</p>"
      : string.Empty;
    bool scalarSelected = returnUrl.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase);

    return LoginPageTemplate
      .Replace("__ERROR__", errorHtml)
      .Replace("__RETURN_URL__", WebUtility.HtmlEncode(returnUrl))
      .Replace("__SWAGGER_SELECTED__", scalarSelected ? string.Empty : "selected")
      .Replace("__SCALAR_SELECTED__", scalarSelected ? "selected" : string.Empty);
  }
}
