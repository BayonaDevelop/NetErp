using System.Diagnostics.CodeAnalysis;
using System.Net;
using Gateway.Endpoints;
using Gateway.Security;
using Gateway.Test.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gateway.Test.Endpoints;

/// <summary>
/// Ejercita LoginEndpoints.MapEndpoints directamente sobre un WebApplication
/// con TestServer (mismo patron que AuthenticationEndpointsTests en
/// Identity.Test), sustituyendo el HttpMessageHandler del HttpClient nombrado
/// "identity-api" para no depender de que Identity.Api este corriendo.
/// </summary>
[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public sealed class LoginEndpointsTests : IAsyncLifetime
{
  private HttpResponseMessage _identityResponse = new(HttpStatusCode.OK);
  private StubHttpMessageHandler _identityHandler = null!;
  private WebApplication _app = null!;
  private HttpClient _client = null!;

  public async Task InitializeAsync()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();

    _identityHandler = new StubHttpMessageHandler(_ => _identityResponse);

    builder.Services
      .AddHttpClient(LoginEndpoints.IdentityHttpClientName, client => client.BaseAddress = new Uri("http://identity-api.test/"))
      .ConfigurePrimaryHttpMessageHandler(() => _identityHandler);

    _app = builder.Build();
    LoginEndpoints.MapEndpoints(_app);

    await _app.StartAsync(CancellationToken.None).ConfigureAwait(false);
    _client = _app.GetTestClient();
  }

  public async Task DisposeAsync()
  {
    _client.Dispose();
    await _app.StopAsync(CancellationToken.None).ConfigureAwait(false);
    await _app.DisposeAsync().ConfigureAwait(false);
  }

  private static HttpRequestMessage BuildLoginFormRequest(
    string companyId,
    string email = "user@test.com",
    string password = "P@ssw0rd",
    string returnUrl = "/swagger/index.html",
    string destination = "swagger")
  {
    Dictionary<string, string> fields = new()
    {
      ["companyId"] = companyId,
      ["email"] = email,
      ["password"] = password,
      ["returnUrl"] = returnUrl,
      ["destination"] = destination
    };

    return new HttpRequestMessage(HttpMethod.Post, "/login") { Content = new FormUrlEncodedContent(fields) };
  }

  private static string JsonLoginResult(string accessToken, string refreshToken = "refresh", string tokenType = "Bearer", int expiresIn = 3600) =>
    $$"""{"AccessToken":"{{accessToken}}","RefreshToken":"{{refreshToken}}","TokenType":"{{tokenType}}","ExpiresIn":{{expiresIn}}}""";

  [Fact]
  public async Task GetLogin_WhenNoQueryParameters_ReturnsHtmlWithSwaggerSelectedAndNoError()
  {
    HttpResponseMessage response = await _client.GetAsync("/login");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
    string html = await response.Content.ReadAsStringAsync();
    Assert.DoesNotContain("Credenciales invalidas", html);
    Assert.Contains("""<option value="swagger" selected>""", html);
    Assert.DoesNotContain("""<option value="scalar" selected>""", html);
  }

  [Fact]
  public async Task GetLogin_WhenErrorQueryParameterIsPresent_ReturnsHtmlWithErrorMessage()
  {
    HttpResponseMessage response = await _client.GetAsync("/login?error=1");

    string html = await response.Content.ReadAsStringAsync();
    Assert.Contains("Credenciales invalidas", html);
  }

  [Fact]
  public async Task GetLogin_WhenReturnUrlPointsToScalar_SelectsScalarOption()
  {
    HttpResponseMessage response = await _client.GetAsync($"/login?returnUrl={Uri.EscapeDataString("/scalar/identity")}");

    string html = await response.Content.ReadAsStringAsync();
    Assert.Contains("""<option value="scalar" selected>""", html);
    Assert.DoesNotContain("""<option value="swagger" selected>""", html);
  }

  [Fact]
  public async Task GetLogin_EncodesReturnUrlToPreventHtmlInjection()
  {
    const string maliciousReturnUrl = "/x\"><script>alert(1)</script>";

    HttpResponseMessage response = await _client.GetAsync($"/login?returnUrl={Uri.EscapeDataString(maliciousReturnUrl)}");

    string html = await response.Content.ReadAsStringAsync();
    Assert.DoesNotContain("<script>alert(1)</script>", html);
    Assert.Contains(System.Net.WebUtility.HtmlEncode(maliciousReturnUrl), html);
  }

  [Fact]
  public async Task PostLogin_WhenCompanyIdIsNotANumber_RedirectsToLoginWithErrorAndDoesNotCallIdentityApi()
  {
    using HttpRequestMessage request = BuildLoginFormRequest(companyId: "not-a-number", returnUrl: "/scalar/identity");

    using HttpResponseMessage response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Equal($"/login?error=1&returnUrl={Uri.EscapeDataString("/scalar/identity")}", response.Headers.Location!.OriginalString);
    Assert.Null(_identityHandler.LastRequest);
  }

  [Fact]
  public async Task PostLogin_WhenIdentityApiRejectsCredentials_RedirectsToLoginWithErrorAndDoesNotSetCookie()
  {
    _identityResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
    using HttpRequestMessage request = BuildLoginFormRequest(companyId: "1");

    using HttpResponseMessage response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Equal($"/login?error=1&returnUrl={Uri.EscapeDataString("/swagger/index.html")}", response.Headers.Location!.OriginalString);
    Assert.False(response.Headers.Contains("Set-Cookie"));
  }

  [Fact]
  public async Task PostLogin_WhenIdentityApiReturnsOkWithoutAccessToken_RedirectsToLoginWithError()
  {
    _identityResponse = new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(JsonLoginResult(accessToken: string.Empty), System.Text.Encoding.UTF8, "application/json")
    };
    using HttpRequestMessage request = BuildLoginFormRequest(companyId: "1");

    using HttpResponseMessage response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Equal($"/login?error=1&returnUrl={Uri.EscapeDataString("/swagger/index.html")}", response.Headers.Location!.OriginalString);
    Assert.False(response.Headers.Contains("Set-Cookie"));
  }

  [Fact]
  public async Task PostLogin_WhenIdentityApiReturnsMalformedBody_RedirectsToLoginWithError()
  {
    _identityResponse = new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
    };
    using HttpRequestMessage request = BuildLoginFormRequest(companyId: "1");

    using HttpResponseMessage response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Contains("error=1", response.Headers.Location!.OriginalString);
  }

  [Fact]
  public async Task PostLogin_WhenCredentialsAreValidAndDestinationIsSwagger_SetsCookieAndRedirectsToSwagger()
  {
    _identityResponse = new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(JsonLoginResult("issued-access-token", expiresIn: 1800), System.Text.Encoding.UTF8, "application/json")
    };
    using HttpRequestMessage request = BuildLoginFormRequest(companyId: "1", destination: "swagger");

    using HttpResponseMessage response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Equal("/swagger/index.html", response.Headers.Location!.OriginalString);

    string setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
    Assert.Contains($"{JwtAuthenticationExtensions.SwaggerCookieName}=issued-access-token", setCookie);
    Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("max-age=1800", setCookie, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public async Task PostLogin_WhenCredentialsAreValidAndDestinationIsScalar_RedirectsToScalar()
  {
    _identityResponse = new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(JsonLoginResult("issued-access-token"), System.Text.Encoding.UTF8, "application/json")
    };
    using HttpRequestMessage request = BuildLoginFormRequest(companyId: "1", destination: "scalar");

    using HttpResponseMessage response = await _client.SendAsync(request);

    Assert.Equal("/scalar/identity", response.Headers.Location!.OriginalString);
  }

  [Fact]
  public async Task PostLogin_ForwardsSubmittedCredentialsToIdentityApi()
  {
    _identityResponse = new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(JsonLoginResult("token"), System.Text.Encoding.UTF8, "application/json")
    };
    using HttpRequestMessage request = BuildLoginFormRequest(companyId: "42", email: "someone@test.com", password: "secret");

    await _client.SendAsync(request);

    Assert.NotNull(_identityHandler.LastRequest);
    Assert.Equal(HttpMethod.Post, _identityHandler.LastRequest!.Method);
    Assert.Equal("/api/v1/auth/login", _identityHandler.LastRequest.RequestUri!.AbsolutePath);
    Assert.Contains("\"companyId\":42", _identityHandler.LastRequestBody);
    Assert.Contains("\"email\":\"someone@test.com\"", _identityHandler.LastRequestBody);
    Assert.Contains("\"password\":\"secret\"", _identityHandler.LastRequestBody);
  }

  [Fact]
  public async Task GetLogout_DeletesSwaggerCookieAndRedirectsToLogin()
  {
    using HttpRequestMessage request = new(HttpMethod.Get, "/logout");
    request.Headers.Add("Cookie", $"{JwtAuthenticationExtensions.SwaggerCookieName}=some-existing-token");

    using HttpResponseMessage response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Equal("/login", response.Headers.Location!.OriginalString);

    IEnumerable<string> setCookieHeaders = response.Headers.GetValues("Set-Cookie");
    Assert.Contains(setCookieHeaders, h => h.StartsWith($"{JwtAuthenticationExtensions.SwaggerCookieName}="));
  }
}
