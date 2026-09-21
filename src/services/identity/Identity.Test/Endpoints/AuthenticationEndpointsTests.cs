using System.Net;
using System.Net.Http.Json;
using System.Text;
using Commons.Mediator;
using Identity.Api.Endpoints;
using Identity.Application.Commands;
using Identity.Application.Dto.Requests;
using Identity.Application.Dto.Responses;
using Identity.Application.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Identity.Test.Endpoints;

public sealed class AuthenticationEndpointsTests : IAsyncLifetime
{
  private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
  private WebApplication _app = null!;
  private HttpClient _client = null!;

  public async Task InitializeAsync()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder();
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();
    builder.Services.AddSingleton(_dispatcher);

    _app = builder.Build();
    AuthenticationEndpoits.MapEndpoints(_app);

    await _app.StartAsync(CancellationToken.None).ConfigureAwait(false);
    _client = _app.GetTestClient();
  }

  public async Task DisposeAsync()
  {
    _client.Dispose();
    await _app.StopAsync(CancellationToken.None).ConfigureAwait(false);
    await _app.DisposeAsync().ConfigureAwait(false);
  }

  [Fact]
  public async Task Login_WhenCredentialsAreValid_ReturnsOkWithTokens()
  {
    LoginRequestDto request = new(1, "user@test.com", "P@ssw0rd");
    LoginResponseDto expected = new("access-token", "refresh-token", "Bearer", 3600);
    _dispatcher.SendAsync(Arg.Any<LoginQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/auth/login", request, CancellationToken.None);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    LoginResponseDto? body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(CancellationToken.None);
    Assert.Equal(expected, body);
  }

  [Fact]
  public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorized()
  {
    LoginRequestDto request = new(1, "user@test.com", "wrong-password");
    LoginResponseDto empty = new(string.Empty, string.Empty, string.Empty, 0);
    _dispatcher.SendAsync(Arg.Any<LoginQuery>(), Arg.Any<CancellationToken>()).Returns(empty);

    HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/auth/login", request, CancellationToken.None);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    string body = await response.Content.ReadAsStringAsync();
    Assert.Empty(body);
  }

  [Fact]
  public async Task Login_ForwardsSubmittedCredentialsToDispatcher()
  {
    LoginRequestDto request = new(7, "someone@test.com", "secret");
    _dispatcher.SendAsync(Arg.Any<LoginQuery>(), Arg.Any<CancellationToken>())
      .Returns(new LoginResponseDto("token", "refresh", "Bearer", 60));

    await _client.PostAsJsonAsync("api/v1/auth/login", request, CancellationToken.None);

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<LoginQuery>(q =>
        q.Request.CompanyId.CompareTo(request.CompanyId) == 0 &&
        q.Request.Email.CompareTo(request.Email) == 0 &&
        q.Request.Password.CompareTo(request.Password) == 0),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Login_WhenBodyIsMalformed_ReturnsBadRequestAndDoesNotCallDispatcher()
  {
    using StringContent content = new("{ not-valid-json", Encoding.UTF8, "application/json");

    HttpResponseMessage response = await _client.PostAsync("api/v1/auth/login", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    await _dispatcher.DidNotReceive().SendAsync(Arg.Any<LoginQuery>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task CreateUser_WhenDispatcherSucceeds_ReturnsOkTrue()
  {
    CreateUserRequestDto request = new(1, "new@test.com", "P@ssw0rd", "Admin");
    _dispatcher.SendAsync(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>()).Returns(true);

    HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/auth/create-user", request, CancellationToken.None);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    bool body = await response.Content.ReadFromJsonAsync<bool>(CancellationToken.None);
    Assert.True(body);
  }

  [Fact]
  public async Task CreateUser_WhenDispatcherFails_ReturnsOkFalse()
  {
    CreateUserRequestDto request = new(1, "dup@test.com", "P@ssw0rd", "Admin");
    _dispatcher.SendAsync(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>()).Returns(false);

    HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/auth/create-user", request, CancellationToken.None);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    bool body = await response.Content.ReadFromJsonAsync<bool>(CancellationToken.None);
    Assert.False(body);
  }

  [Fact]
  public async Task CreateUser_ForwardsSubmittedDataToDispatcher()
  {
    CreateUserRequestDto request = new(3, "another@test.com", "pwd", "Editor");
    _dispatcher.SendAsync(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>()).Returns(true);

    await _client.PostAsJsonAsync("api/v1/auth/create-user", request, CancellationToken.None);

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<CreateUserCommand>(c =>
        c.Request.CompanyId.CompareTo(request.CompanyId) == 0 &&
        c.Request.Email.CompareTo(request.Email) == 0 &&
        c.Request.Password.CompareTo(request.Password) == 0 &&
        c.Request.Role.CompareTo(request.Role) == 0),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task CreateUser_WhenBodyIsMalformed_ReturnsBadRequestAndDoesNotCallDispatcher()
  {
    using StringContent content = new("{ not-valid-json", Encoding.UTF8, "application/json");

    HttpResponseMessage response = await _client.PostAsync("api/v1/auth/create-user", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    await _dispatcher.DidNotReceive().SendAsync(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>());
  }
}
