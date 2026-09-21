using Commons.I18n;
using Commons.Mediator;
using Identity.Application.Commands;
using Identity.Core.Constants;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Handlers;

public class CreateUserHandler(
  ILogger<CreateUserHandler> logger, 
  IUserRepository repository, 
  IPasswordHasher<User> hasher
) : ICommandHandler<CreateUserCommand, bool>
{
  private readonly ILogger<CreateUserHandler> _logger = logger;
  private readonly IUserRepository _repository = repository;
  private readonly IPasswordHasher<User> _hasher = hasher;
  private readonly ResxLocalizer _messages = new("Identity.Api.Resources.UserMessages", typeof(CreateUserHandler).Assembly);

  public async Task<bool> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
  {
    User user = new() { Email = command.Request.Email };
    user.PasswordHash = _hasher.HashPassword(user, command.Request.Password);

    UserCreationStatus result = await _repository
      .CreateUSerAsync(command.Request.CompanyId, command.Request.Email, user.PasswordHash, command.Request.Role, cancellationToken)
      .ConfigureAwait(false);

    switch (result)
    {
      case UserCreationStatus.CREATED:
        if (_logger.IsEnabled(LogLevel.Information))
        {
          _logger.LogInformation("{Message}", _messages.Get("CREATED"));
        }
        return true;

      case UserCreationStatus.EMAIL_ALREADY_EXISTS:
        if (_logger.IsEnabled(LogLevel.Information))
        {
          _logger.LogInformation("{Message}", _messages.Get("EMAIL_ALREADY_EXISTS"));
        }
        return false;

      default:
        if (_logger.IsEnabled(LogLevel.Information))
        {
          _logger.LogInformation("{Message}", _messages.Get("ROLE_NOT_FOUND"));
        }
        return false;
    }
  }
}
