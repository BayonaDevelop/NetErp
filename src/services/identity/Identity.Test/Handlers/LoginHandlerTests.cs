using Identity.Application.Dto.Requests;
using Identity.Application.Dto.Responses;
using Identity.Application.Handlers;
using Identity.Application.Queries;
using Identity.Application.Settings;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Identity.Test.Handlers;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class LoginHandlerTests
{
  private static Jwt CreateJwtSettings() => new()
  {
    SigningKey = "unit-test-signing-key-0123456789-please-ignore",
    Issuer = "netErp.identity.tests",
    Audience = "netErp.clients.tests",
    DurationInMinutes = 45
  };

  private static User CreateFoundUser() => new()
  {
    Id = 42,
    CompanyId = 1,
    Email = "user@test.com",
    PasswordHash = "hashed-password",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    Roles = [new Role { Name = "Admin", NormalizedName = "ADMIN" }]
  };

  [Fact]
  public async Task HandleAsync_WhenUserDoesNotExist_ReturnsEmptyResponseWithoutCheckingPasswordOrLoggingAttempt()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    IPasswordHasher<User> hasher = Substitute.For<IPasswordHasher<User>>();
    repository.GetByUserNameAsync("missing@test.com", Arg.Any<CancellationToken>()).Returns(new User());

    LoginHandler sut = new(repository, hasher, Options.Create(CreateJwtSettings()));
    LoginQuery query = new(new LoginRequestDto(1, "missing@test.com", "whatever"), "127.0.0.1");

    LoginResponseDto result = await sut.HandleAsync(query, CancellationToken.None);

    Assert.Equal(new LoginResponseDto(string.Empty, string.Empty, string.Empty, 0), result);
    hasher.DidNotReceiveWithAnyArgs().VerifyHashedPassword(default!, default!, default!);
    await repository.DidNotReceiveWithAnyArgs().CreateLogginAttemptAsync(default!, default, default!, default);
  }

  [Fact]
  public async Task HandleAsync_WhenPasswordIsInvalid_ReturnsEmptyResponseAndRecordsFailedAttempt()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    IPasswordHasher<User> hasher = Substitute.For<IPasswordHasher<User>>();
    User user = CreateFoundUser();
    repository.GetByUserNameAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
    hasher.VerifyHashedPassword(user, user.PasswordHash, "wrong-password").Returns(PasswordVerificationResult.Failed);

    LoginHandler sut = new(repository, hasher, Options.Create(CreateJwtSettings()));
    LoginQuery query = new(new LoginRequestDto(user.CompanyId, user.Email, "wrong-password"), "10.0.0.1");

    LoginResponseDto result = await sut.HandleAsync(query, CancellationToken.None);

    Assert.Equal(new LoginResponseDto(string.Empty, string.Empty, string.Empty, 0), result);
    await repository.Received(1).CreateLogginAttemptAsync(user, false, "10.0.0.1", Arg.Any<CancellationToken>());
    await repository.DidNotReceiveWithAnyArgs().IssueRefreshTokenAsync(default!, default!, default!, default);
  }

  [Fact]
  public async Task HandleAsync_WhenCredentialsAreValid_ReturnsSignedTokensAndRecordsSuccessfulAttempt()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    IPasswordHasher<User> hasher = Substitute.For<IPasswordHasher<User>>();
    User user = CreateFoundUser();
    Jwt jwtSettings = CreateJwtSettings();
    repository.GetByUserNameAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
    hasher.VerifyHashedPassword(user, user.PasswordHash, "correct-password").Returns(PasswordVerificationResult.Success);
    repository.IssueRefreshTokenAsync(user, "10.0.0.1", Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns("stored-refresh-token-hash");

    LoginHandler sut = new(repository, hasher, Options.Create(jwtSettings));
    LoginQuery query = new(new LoginRequestDto(user.CompanyId, user.Email, "correct-password"), "10.0.0.1");

    LoginResponseDto result = await sut.HandleAsync(query, CancellationToken.None);

    Assert.NotEmpty(result.AccessToken);
    Assert.Equal("stored-refresh-token-hash", result.RefreshToken);
    Assert.Equal("Bearer", result.TokenType);
    Assert.Equal(jwtSettings.DurationInMinutes, result.ExpiresIn);

    JwtSecurityTokenHandler handler = new();
    JwtSecurityToken token = handler.ReadJwtToken(result.AccessToken);
    Assert.Equal(jwtSettings.Issuer, token.Issuer);
    Assert.Contains(jwtSettings.Audience, token.Audiences);
    Assert.Equal(user.Id.ToString(), token.Claims.Single(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).Value);
    Assert.Equal(user.Email, token.Claims.Single(c => c.Type.Equals(JwtRegisteredClaimNames.Email)).Value);
    Assert.Contains(token.Claims, c => c.Type.Equals(ClaimTypes.Role) && c.Value.Equals("Admin"));

    await repository.Received(1).CreateLogginAttemptAsync(user, true, "10.0.0.1", Arg.Any<CancellationToken>());
    await repository.Received(1).IssueRefreshTokenAsync(user, "10.0.0.1", Arg.Any<string>(), Arg.Any<CancellationToken>());
  }
}
