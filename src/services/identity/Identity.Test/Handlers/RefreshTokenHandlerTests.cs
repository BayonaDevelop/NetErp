using Identity.Application.Commands;
using Identity.Application.Dto.Requests;
using Identity.Application.Dto.Responses;
using Identity.Application.Handlers;
using Identity.Application.Settings;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Identity.Test.Handlers;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class RefreshTokenHandlerTests
{
  private static Jwt CreateJwtSettings() => new()
  {
    SigningKey = "unit-test-signing-key-0123456789-please-ignore",
    Issuer = "netErp.identity.tests",
    Audience = "netErp.clients.tests",
    DurationInMinutes = 45
  };

  private static User CreateUser() => new()
  {
    Id = 7,
    CompanyId = 1,
    Email = "refresh@test.com",
    PasswordHash = "hash",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    Roles = [new Role { Name = "Admin", NormalizedName = "ADMIN" }]
  };

  private static RefreshTokenHandler CreateSut(IUserRepository repository, Jwt? jwt = null) =>
    new(repository, Options.Create(jwt ?? CreateJwtSettings()));

  [Fact]
  public async Task HandleAsync_WhenTokenDoesNotExist_ReturnsEmptyResponseWithoutSavingAnything()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    repository.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

    RefreshTokenHandler sut = CreateSut(repository);
    RefreshTokenCommand command = new(new RefreshTokenRequestDto("unknown-token"), "127.0.0.1");

    LoginResponseDto result = await sut.HandleAsync(command, CancellationToken.None);

    Assert.Equal(new LoginResponseDto(string.Empty, string.Empty, string.Empty, 0), result);
    await repository.DidNotReceiveWithAnyArgs().SaveRefreshTokenAsync(default!, default);
  }

  [Fact]
  public async Task HandleAsync_WhenTokenIsRevoked_ReturnsEmptyResponseWithoutSavingAnything()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    RefreshToken revoked = new()
    {
      UserId = 7,
      TokenHash = "hash",
      ExpiresAt = DateTime.UtcNow.AddDays(1),
      RevokedAt = DateTime.UtcNow.AddMinutes(-1),
      User = CreateUser()
    };
    repository.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(revoked);

    RefreshTokenHandler sut = CreateSut(repository);
    RefreshTokenCommand command = new(new RefreshTokenRequestDto("revoked-token"), "127.0.0.1");

    LoginResponseDto result = await sut.HandleAsync(command, CancellationToken.None);

    Assert.Equal(new LoginResponseDto(string.Empty, string.Empty, string.Empty, 0), result);
    await repository.DidNotReceiveWithAnyArgs().SaveRefreshTokenAsync(default!, default);
  }

  [Fact]
  public async Task HandleAsync_WhenTokenIsExpired_ReturnsEmptyResponseWithoutSavingAnything()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    RefreshToken expired = new()
    {
      UserId = 7,
      TokenHash = "hash",
      ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
      RevokedAt = null,
      User = CreateUser()
    };
    repository.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expired);

    RefreshTokenHandler sut = CreateSut(repository);
    RefreshTokenCommand command = new(new RefreshTokenRequestDto("expired-token"), "127.0.0.1");

    LoginResponseDto result = await sut.HandleAsync(command, CancellationToken.None);

    Assert.Equal(new LoginResponseDto(string.Empty, string.Empty, string.Empty, 0), result);
    await repository.DidNotReceiveWithAnyArgs().SaveRefreshTokenAsync(default!, default);
  }

  [Fact]
  public async Task HandleAsync_WhenTokenIsValid_ReturnsNewSignedTokensAndRevokesTheOldOne()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    User user = CreateUser();
    RefreshToken existing = new()
    {
      UserId = user.Id,
      TokenHash = "old-hash",
      ExpiresAt = DateTime.UtcNow.AddDays(1),
      RevokedAt = null,
      User = user
    };
    repository.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);

    Jwt jwtSettings = CreateJwtSettings();
    RefreshTokenHandler sut = CreateSut(repository, jwtSettings);
    RefreshTokenCommand command = new(new RefreshTokenRequestDto("valid-raw-token"), "10.0.0.9");

    LoginResponseDto result = await sut.HandleAsync(command, CancellationToken.None);

    Assert.NotEmpty(result.AccessToken);
    Assert.NotEmpty(result.RefreshToken);
    Assert.Equal("Bearer", result.TokenType);
    Assert.Equal(jwtSettings.DurationInMinutes, result.ExpiresIn);

    JwtSecurityTokenHandler handler = new();
    JwtSecurityToken token = handler.ReadJwtToken(result.AccessToken);
    Assert.Equal(jwtSettings.Issuer, token.Issuer);
    Assert.Contains(jwtSettings.Audience, token.Audiences);
    Assert.Equal(user.Id.ToString(), token.Claims.Single(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).Value);
    Assert.Contains(token.Claims, c => c.Type.Equals(ClaimTypes.Role) && c.Value.Equals("Admin"));

    Assert.NotNull(existing.RevokedAt);

    await repository.Received(1).SaveRefreshTokenAsync(
      Arg.Is<RefreshToken>(rt =>
        rt.UserId.Equals(user.Id) &&
        rt.CreatedByIp!.Equals("10.0.0.9") &&
        !string.IsNullOrEmpty(rt.TokenHash) &&
        !rt.TokenHash.Equals(existing.TokenHash)),
      Arg.Any<CancellationToken>());
  }
}
