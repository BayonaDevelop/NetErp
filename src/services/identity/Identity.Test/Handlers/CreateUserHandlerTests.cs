using Identity.Application.Commands;
using Identity.Application.Dto.Requests;
using Identity.Application.Handlers;
using Identity.Core.Constants;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Identity.Test.Handlers;

public class CreateUserHandlerTests
{
  private static (IUserRepository repository, IPasswordHasher<User> hasher, ILogger<CreateUserHandler> logger) CreateDependencies()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    IPasswordHasher<User> hasher = Substitute.For<IPasswordHasher<User>>();
    ILogger<CreateUserHandler> logger = Substitute.For<ILogger<CreateUserHandler>>();
    hasher.HashPassword(Arg.Any<User>(), Arg.Any<string>()).Returns(ci => $"hashed:{ci.Arg<string>()}");
    return (repository, hasher, logger);
  }

  [Theory]
  [InlineData(UserCreationStatus.CREATED, true)]
  [InlineData(UserCreationStatus.EMAIL_ALREADY_EXISTS, false)]
  [InlineData(UserCreationStatus.ROLE_NOT_FOUND, false)]
  public async Task HandleAsync_ReturnsExpectedResultForEachCreationStatus(UserCreationStatus status, bool expected)
  {
    (IUserRepository repository, IPasswordHasher<User> hasher, ILogger<CreateUserHandler> logger) = CreateDependencies();
    repository
      .CreateUSerAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(status);

    CreateUserHandler sut = new(logger, repository, hasher);
    CreateUserCommand command = new(new CreateUserRequestDto(1, "new@test.com", "P@ssw0rd!", "Admin"), "127.0.0.1");

    bool result = await sut.HandleAsync(command, CancellationToken.None);

    Assert.Equal(expected, result);
  }

  [Fact]
  public async Task HandleAsync_HashesPasswordAndForwardsHashToRepository()
  {
    (IUserRepository repository, IPasswordHasher<User> hasher, ILogger<CreateUserHandler> logger) = CreateDependencies();
    repository
      .CreateUSerAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(UserCreationStatus.CREATED);

    CreateUserHandler sut = new(logger, repository, hasher);
    CreateUserRequestDto request = new(9, "hash-me@test.com", "plain-password", "Editor");
    CreateUserCommand command = new(request, "10.0.0.5");

    await sut.HandleAsync(command, CancellationToken.None);

    hasher.Received(1).HashPassword(Arg.Is<User>(u => u.Email == request.Email), request.Password);
    await repository.Received(1).CreateUSerAsync(
      request.CompanyId, request.Email, "hashed:plain-password", request.Role, Arg.Any<CancellationToken>());
    Assert.Equal("10.0.0.5", command.IpAddress);
  }

  [Theory]
  [InlineData(UserCreationStatus.CREATED)]
  [InlineData(UserCreationStatus.EMAIL_ALREADY_EXISTS)]
  [InlineData(UserCreationStatus.ROLE_NOT_FOUND)]
  public async Task HandleAsync_WhenInformationLoggingIsEnabled_LogsForEachCreationStatus(UserCreationStatus status)
  {
    (IUserRepository repository, IPasswordHasher<User> hasher, ILogger<CreateUserHandler> logger) = CreateDependencies();
    logger.IsEnabled(LogLevel.Information).Returns(true);
    repository
      .CreateUSerAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(status);

    CreateUserHandler sut = new(logger, repository, hasher);
    CreateUserCommand command = new(new CreateUserRequestDto(1, "new@test.com", "P@ssw0rd!", "Admin"), "127.0.0.1");

    await sut.HandleAsync(command, CancellationToken.None);

    logger.Received(1).IsEnabled(LogLevel.Information);
  }
}
