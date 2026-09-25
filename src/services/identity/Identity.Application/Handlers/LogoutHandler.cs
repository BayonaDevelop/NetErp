using Commons.Mediator;
using Identity.Application.Commands;
using Identity.Application.Settings;
using Identity.Core.Repositories;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Handlers;

public class LogoutHandler(IUserRepository repository) : ICommandHandler<LogoutCommand, bool>
{
  private readonly IUserRepository _repository = repository;

  private static string HashToken(string token) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

  public async Task<bool> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
  {
    var tokenHash = HashToken(command.Request.RefreshToken);
    var existing = await _repository.GetRefreshTokenAsync(tokenHash, cancellationToken).ConfigureAwait(false);

    if (existing is not null)
    {
      existing.RevokedAt = DateTime.UtcNow;
      await _repository.UpdateRefreshTokenAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    return true;
  }
}
