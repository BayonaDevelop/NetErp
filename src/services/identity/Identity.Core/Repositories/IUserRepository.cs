using Identity.Core.Constants;
using Identity.Core.Entities;

namespace Identity.Core.Repositories;

public interface IUserRepository
{
  Task<UserCreationStatus> CreateUSerAsync(long companyId, string Email, string Password, string? Role, CancellationToken cancellationToken);

  Task<User> GetByUserNameAsync(string email, CancellationToken cancellationToken);

  Task CreateLogginAttemptAsync(User user, bool success, string ipAddress, CancellationToken cancellationToken);

  Task<string> IssueRefreshTokenAsync(User user, string ip, string token, CancellationToken cancellationToken);
}
