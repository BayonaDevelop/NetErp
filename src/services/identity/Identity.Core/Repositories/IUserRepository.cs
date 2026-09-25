using Identity.Core.Constants;
using Identity.Core.Entities;
using Identity.Core.Types;

namespace Identity.Core.Repositories;

public interface IUserRepository
{
  Task<UserCreationStatus> CreateUSerAsync(long companyId, string Email, string Password, string? Role, CancellationToken cancellationToken);

  Task<Optional<User>> GetByUserNameAsync(long companyId, string email, CancellationToken cancellationToken);

  Task CreateLogginAttemptAsync(User user, bool success, string ipAddress, CancellationToken cancellationToken);

  Task<string> IssueRefreshTokenAsync(User user, string ip, string token, CancellationToken cancellationToken);

  Task SaveRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);

  Task UpdateRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);

  Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken);

  Task<List<User>> GetAllUsersByCompanyIdAsync(long companyId, CancellationToken cancellationToken);

  Task<Optional<User>> GetUserByIdAsync(long companyId, long id, CancellationToken cancellationToken);

  Task UpdateUserRolesAsync(long companyId, long userId, List<string> roles, CancellationToken cancellationToken);
}
