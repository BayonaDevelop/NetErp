using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Gateway.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Test.Security;

/// <summary>
/// Cubre AddJwtAuthentication (validacion de configuracion, esquema Bearer,
/// policy "Swagger") y RequireAuthorizationForPath, el middleware que protege
/// /swagger y /scalar cuando UseSwaggerUI no pasa por endpoint routing. Los
/// tokens se generan a mano con la misma libreria que usa Identity.Api
/// (System.IdentityModel.Tokens.Jwt) para no depender de que Identity.Api
/// este disponible.
/// </summary>
[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public sealed class JwtAuthenticationExtensionsTests
{
  private const string Issuer = "netErp.identity.tests";
  private const string Audience = "netErp.clients.tests";
  private const string SigningKey = "unit-test-signing-key-0123456789-please-ignore";
  private const string ProtectedPathPrefix = "/swagger";

  private static string CreateToken(
    string? role = null,
    string issuer = Issuer,
    string audience = Audience,
    string signingKey = SigningKey,
    DateTime? expires = null)
  {
    SigningCredentials credentials = new(
      new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
      SecurityAlgorithms.HmacSha256);

    List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, "test-user")];
    if (role is not null)
      claims.Add(new Claim(ClaimTypes.Role, role));

    JwtSecurityToken token = new(issuer, audience, claims, expires: expires ?? DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  [Fact]
  public void AddJwtAuthentication_WhenJwtSectionIsMissing_Throws()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder();
    builder.Configuration.Sources.Clear();

    InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => builder.AddJwtAuthentication());

    Assert.Contains("Jwt", exception.Message);
  }

  private static async Task<(WebApplication App, HttpClient Client)> CreateProtectedAppAsync()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["Jwt:Issuer"] = Issuer,
      ["Jwt:Audience"] = Audience,
      ["Jwt:SigningKey"] = SigningKey
    });

    builder.AddJwtAuthentication();

    WebApplication app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.RequireAuthorizationForPath(ProtectedPathPrefix, JwtAuthenticationExtensions.SwaggerPolicy);

    app.MapGet("/swagger/index.html", () => Results.Text("swagger-ui"));
    app.MapGet("/public", () => Results.Text("public"));

    await app.StartAsync(CancellationToken.None).ConfigureAwait(false);
    return (app, app.GetTestClient());
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenPathIsOutsidePrefix_IsAccessibleWithoutAuthentication()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      HttpResponseMessage response = await client.GetAsync("/public");

      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenNoTokenAndClientDoesNotAcceptHtml_ReturnsUnauthorized()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      HttpResponseMessage response = await client.GetAsync("/swagger/index.html");

      Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenNoTokenAndClientAcceptsHtml_RedirectsToLoginPage()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      using HttpRequestMessage request = new(HttpMethod.Get, "/swagger/index.html");
      request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

      HttpResponseMessage response = await client.SendAsync(request);

      Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
      Assert.Equal(
        $"/login?error=1&returnUrl={Uri.EscapeDataString("/swagger/index.html")}",
        response.Headers.Location!.OriginalString);
    }
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenTokenIsExpired_ReturnsUnauthorized()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      using HttpRequestMessage request = new(HttpMethod.Get, "/swagger/index.html");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role: JwtAuthenticationExtensions.SwaggerRole, expires: DateTime.UtcNow.AddMinutes(-5)));

      HttpResponseMessage response = await client.SendAsync(request);

      Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenTokenIsSignedWithAnUnknownKey_ReturnsUnauthorized()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      using HttpRequestMessage request = new(HttpMethod.Get, "/swagger/index.html");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role: JwtAuthenticationExtensions.SwaggerRole, signingKey: "a-completely-different-signing-key-0123456789"));

      HttpResponseMessage response = await client.SendAsync(request);

      Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenTokenLacksSwaggerRoleAndClientDoesNotAcceptHtml_ReturnsForbidden()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      using HttpRequestMessage request = new(HttpMethod.Get, "/swagger/index.html");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role: "SOME_OTHER_ROLE"));

      HttpResponseMessage response = await client.SendAsync(request);

      Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenTokenLacksSwaggerRoleAndClientAcceptsHtml_ReturnsForbiddenWithMessage()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      using HttpRequestMessage request = new(HttpMethod.Get, "/swagger/index.html");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role: "SOME_OTHER_ROLE"));
      request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

      HttpResponseMessage response = await client.SendAsync(request);

      Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
      string body = await response.Content.ReadAsStringAsync();
      Assert.Contains("Acceso denegado", body);
      Assert.Contains(JwtAuthenticationExtensions.SwaggerRole, body);
    }
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenTokenHasSwaggerRoleInAuthorizationHeader_ReturnsOk()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      using HttpRequestMessage request = new(HttpMethod.Get, "/swagger/index.html");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role: JwtAuthenticationExtensions.SwaggerRole));

      HttpResponseMessage response = await client.SendAsync(request);

      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.Equal("swagger-ui", await response.Content.ReadAsStringAsync());
    }
  }

  [Fact]
  public async Task RequireAuthorizationForPath_WhenTokenHasSwaggerRoleInCookieAndNoAuthorizationHeader_ReturnsOk()
  {
    (WebApplication app, HttpClient client) = await CreateProtectedAppAsync();
    await using (app)
    {
      using HttpRequestMessage request = new(HttpMethod.Get, "/swagger/index.html");
      request.Headers.Add("Cookie", $"{JwtAuthenticationExtensions.SwaggerCookieName}={CreateToken(role: JwtAuthenticationExtensions.SwaggerRole)}");

      HttpResponseMessage response = await client.SendAsync(request);

      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
  }
}
