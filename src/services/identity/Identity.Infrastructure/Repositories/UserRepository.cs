using Identity.Core.Constants;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class UserRepository(SqlServerDbContext dbContext) : IUserRepository
{
  private readonly SqlServerDbContext _dbContext = dbContext;

  public async Task<UserCreationStatus> CreateUSerAsync(long companyId, string email, string password, string? role, CancellationToken cancellationToken)
  {
    User userAlreadyExist = await GetByUserNameAsync(email, cancellationToken).ConfigureAwait(false);

    if (userAlreadyExist.Email is not null)
      return UserCreationStatus.EMAIL_ALREADY_EXISTS;

    var roleEntity = await _dbContext.Roles
      .FirstOrDefaultAsync(r => r.Name.CompareTo(role) == 0, cancellationToken)
      .ConfigureAwait(false);

    if (roleEntity == null)
      return UserCreationStatus.ROLE_NOT_FOUND;

    User userEntity = new() { CompanyId = companyId, Email = email, PasswordHash = password, IsActive = true, CreatedAt = DateTime.Now };
    userEntity.Roles.Add(roleEntity!);

    _dbContext.Users.Add(userEntity);
    await _dbContext
      .SaveChangesAsync(cancellationToken)
      .ConfigureAwait(false);

    return UserCreationStatus.CREATED;
  }

  public async Task<User> GetByUserNameAsync(string email, CancellationToken cancellationToken)
  {
    User? entity = await _dbContext.Users
      .Include(i => i.Roles)
      .FirstOrDefaultAsync(i => i.Email.Equals(email), cancellationToken)
      .ConfigureAwait(false);

    return entity ?? new();
  }

  public async Task CreateLogginAttemptAsync(User user, bool success, string ipAddress, CancellationToken cancellationToken)
  {
    LoginAttempt entity = new()
    {
      Email = user.Email,
      User = user,
      IpAddress = ipAddress,
      Success = success,
      AttemptedAt = DateTime.Now
    };

    _dbContext.LoginAttempts.Add(entity);
    await _dbContext
      .SaveChangesAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<string> IssueRefreshTokenAsync(User user, string ip, string token, CancellationToken cancellationToken)
  {
    RefreshToken entity = new()
    {
      UserId = user.Id,
      TokenHash = token,
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      CreatedByIp = ip
    };

    _dbContext.RefreshTokens.Add(entity);
    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return token;
  }

  public async Task<List<User>> GetAllUsersByCompanyId(long companyId, CancellationToken cancellationToken)
  {
    List<User> entities = await _dbContext.Users
      .Include(i => i.Roles)
      .Where(i => i.CompanyId == companyId)
      .ToListAsync(cancellationToken);

    return entities;
  }

  public async Task<User> GetUserByIdAsync(long companyId, long id, CancellationToken cancellationToken)
  {
    User? entity = await _dbContext.Users
      .Include(i => i.Roles)
      .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Id == id, cancellationToken)
      .ConfigureAwait(false);

    return entity ?? new();
  }

  public async Task UpdateUserRoles(long companyId, long userId, List<string> roles, CancellationToken cancellationToken)
  {
    User? user = await GetUserByIdAsync(companyId, userId, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("User not found");
    
    user.Roles.Clear();

    foreach (string role in roles)
    {
      Role? roleEntity = await _dbContext.Roles
        .FirstOrDefaultAsync(r => r.Name.Equals(role), cancellationToken)
        .ConfigureAwait(false);

      if (roleEntity != null)
        user.Roles.Add(roleEntity);
    }

    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
