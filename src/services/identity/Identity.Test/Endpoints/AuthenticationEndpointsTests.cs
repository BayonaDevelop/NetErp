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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Identity.Test.Endpoints;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
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

  [Fact]
  public async Task RefreshToken_WhenTokenIsValid_ReturnsOkWithTokens()
  {
    RefreshTokenRequestDto request = new("valid-refresh-token");
    LoginResponseDto expected = new("new-access-token", "new-refresh-token", "Bearer", 3600);
    _dispatcher.SendAsync(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>()).Returns(expected);

    HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/auth/refresh-token", request, CancellationToken.None);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    LoginResponseDto? body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(CancellationToken.None);
    Assert.Equal(expected, body);
  }

  [Fact]
  public async Task RefreshToken_WhenTokenIsInvalid_ReturnsUnauthorized()
  {
    RefreshTokenRequestDto request = new("expired-or-unknown-token");
    LoginResponseDto empty = new(string.Empty, string.Empty, string.Empty, 0);
    _dispatcher.SendAsync(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>()).Returns(empty);

    HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/auth/refresh-token", request, CancellationToken.None);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    string body = await response.Content.ReadAsStringAsync();
    Assert.Empty(body);
  }

  [Fact]
  public async Task RefreshToken_ForwardsSubmittedTokenToDispatcher()
  {
    RefreshTokenRequestDto request = new("some-raw-token");
    _dispatcher.SendAsync(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
      .Returns(new LoginResponseDto("token", "refresh", "Bearer", 60));

    await _client.PostAsJsonAsync("api/v1/auth/refresh-token", request, CancellationToken.None);

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<RefreshTokenCommand>(c => c.Request.RefreshToken.Equals(request.RefreshToken)),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task RefreshToken_WhenBodyIsMalformed_ReturnsBadRequestAndDoesNotCallDispatcher()
  {
    using StringContent content = new("{ not-valid-json", Encoding.UTF8, "application/json");

    HttpResponseMessage response = await _client.PostAsync("api/v1/auth/refresh-token", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    await _dispatcher.DidNotReceive().SendAsync(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetCompanyUsers_WhenCompanyIdIsPositive_ReturnsOkWithUsers()
  {
    List<UserResponseDto> expected =
    [
      new UserResponseDto(1, "user1@test.com", true, DateTime.UtcNow, ["Admin"])
    ];
    _dispatcher.SendAsync(Arg.Any<GetUsersByCompanyQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    HttpResponseMessage response = await _client.GetAsync("api/v1/auth/get-company-users?companyId=5");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    List<UserResponseDto>? body = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>(CancellationToken.None);
    Assert.NotNull(body);
    Assert.Single(body);
    Assert.Equal("user1@test.com", body[0].Email);

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<GetUsersByCompanyQuery>(q => q.CompanyId == 5),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetCompanyUsers_WhenNoUsersExist_ReturnsOkWithEmptyList()
  {
    _dispatcher.SendAsync(Arg.Any<GetUsersByCompanyQuery>(), Arg.Any<CancellationToken>()).Returns([]);

    HttpResponseMessage response = await _client.GetAsync("api/v1/auth/get-company-users?companyId=5");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    List<UserResponseDto>? body = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>(CancellationToken.None);
    Assert.Empty(body!);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public async Task GetCompanyUsers_WhenCompanyIdIsNotPositive_ReturnsBadRequestAndDoesNotCallDispatcher(long companyId)
  {
    HttpResponseMessage response = await _client.GetAsync($"api/v1/auth/get-company-users?companyId={companyId}");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    await _dispatcher.DidNotReceive().SendAsync(Arg.Any<GetUsersByCompanyQuery>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetCompanyUsers_WhenCompanyIdIsNotPositive_ReturnsMessageBody()
  {
    HttpResponseMessage response = await _client.GetAsync("api/v1/auth/get-company-users?companyId=0");

    string body = await response.Content.ReadAsStringAsync();
    Assert.Contains("This field only accepts numbers greater than 0", body);
  }

  [Fact]
  public async Task GetCompanyUsers_WhenCompanyIdIsMissing_ReturnsBadRequestAndDoesNotCallDispatcher()
  {
    HttpResponseMessage response = await _client.GetAsync("api/v1/auth/get-company-users");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    await _dispatcher.DidNotReceive().SendAsync(Arg.Any<GetUsersByCompanyQuery>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetCompanyUsers_WhenCompanyIdIsMissing_ReturnsFieldRequiredMessageBody()
  {
    HttpResponseMessage response = await _client.GetAsync("api/v1/auth/get-company-users");

    string body = await response.Content.ReadAsStringAsync();
    Assert.Contains("This field is required", body);
  }

  [Fact]
  public async Task Logout_WhenDispatched_ReturnsNoContent()
  {
    RefreshTokenRequestDto request = new("some-refresh-token");
    _dispatcher.SendAsync(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>()).Returns(true);

    HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/auth/logout", request, CancellationToken.None);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }

  [Fact]
  public async Task Logout_ForwardsSubmittedTokenToDispatcher()
  {
    RefreshTokenRequestDto request = new("token-to-revoke");
    _dispatcher.SendAsync(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>()).Returns(true);

    await _client.PostAsJsonAsync("api/v1/auth/logout", request, CancellationToken.None);

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<LogoutCommand>(c => c.Request.RefreshToken.Equals(request.RefreshToken)),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Logout_WhenBodyIsMalformed_ReturnsBadRequestAndDoesNotCallDispatcher()
  {
    using StringContent content = new("{ not-valid-json", Encoding.UTF8, "application/json");

    HttpResponseMessage response = await _client.PostAsync("api/v1/auth/logout", content);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    await _dispatcher.DidNotReceive().SendAsync(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetUser_WhenFound_ReturnsOkWithUser()
  {
    DateTime createdAt = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    UserResponseDto expected = new(9, "found@test.com", true, createdAt, ["Admin"]);
    _dispatcher.SendAsync(Arg.Any<GetUserByIdQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    HttpResponseMessage response = await _client.GetAsync("api/v1/auth/get-user?companyId=5&userId=9");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    UserResponseDto? body = await response.Content.ReadFromJsonAsync<UserResponseDto>(CancellationToken.None);
    Assert.NotNull(body);
    Assert.Equal(expected.Id, body.Id);
    Assert.Equal(expected.Email, body.Email);
    Assert.Equal(expected.IsActive, body.IsActive);
    Assert.Equal(expected.CreatedAt, body.CreatedAt);
    Assert.Equal(expected.Roles, body.Roles);

    await _dispatcher.Received(1).SendAsync(
      Arg.Is<GetUserByIdQuery>(q => q.CompanyId == 5 && q.UserId == 9),
      Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData("", "userId=9")]
  [InlineData("companyId=0", "userId=9")]
  [InlineData("companyId=5", "")]
  [InlineData("companyId=5", "userId=0")]
  public async Task GetUser_WhenCompanyIdOrUserIdAreMissingOrNotPositive_ReturnsBadRequestAndDoesNotCallDispatcher(string companyIdPart, string userIdPart)
  {
    string query = string.Join("&", new[] { companyIdPart, userIdPart }.Where(p => p.Length > 0));

    HttpResponseMessage response = await _client.GetAsync($"api/v1/auth/get-user?{query}");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    await _dispatcher.DidNotReceive().SendAsync(Arg.Any<GetUserByIdQuery>(), Arg.Any<CancellationToken>());
  }
}
