using Identity.Application.Commands;
using Identity.Application.Dto.Requests;
using Identity.Application.Handlers;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Test.Handlers;

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class LogoutHandlerTests
{
  [Fact]
  public async Task HandleAsync_WhenTokenDoesNotExist_ReturnsTrueWithoutUpdatingAnything()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    repository.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

    LogoutHandler sut = new(repository);
    LogoutCommand command = new(new RefreshTokenRequestDto("unknown-token"));

    bool result = await sut.HandleAsync(command, CancellationToken.None);

    Assert.True(result);
    await repository.DidNotReceiveWithAnyArgs().UpdateRefreshTokenAsync(default!, default);
  }

  [Fact]
  public async Task HandleAsync_WhenTokenExists_RevokesItAndReturnsTrue()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    RefreshToken existing = new() { UserId = 1, TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddDays(1) };
    repository.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);

    LogoutHandler sut = new(repository);
    LogoutCommand command = new(new RefreshTokenRequestDto("valid-token"));

    bool result = await sut.HandleAsync(command, CancellationToken.None);

    Assert.True(result);
    Assert.NotNull(existing.RevokedAt);
    await repository.Received(1).UpdateRefreshTokenAsync(existing, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_WhenTokenIsAlreadyRevoked_StillReturnsTrueAndUpdatesAgain()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    RefreshToken alreadyRevoked = new()
    {
      UserId = 1,
      TokenHash = "hash",
      ExpiresAt = DateTime.UtcNow.AddDays(1),
      RevokedAt = DateTime.UtcNow.AddMinutes(-5)
    };
    repository.GetRefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(alreadyRevoked);

    LogoutHandler sut = new(repository);
    LogoutCommand command = new(new RefreshTokenRequestDto("already-revoked-token"));

    bool result = await sut.HandleAsync(command, CancellationToken.None);

    Assert.True(result);
    await repository.Received(1).UpdateRefreshTokenAsync(alreadyRevoked, Arg.Any<CancellationToken>());
  }
}
