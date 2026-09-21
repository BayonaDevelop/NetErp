using Identity.Core.Constants;
using Identity.Core.Entities;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Identity.Test.Repositories;

public class UserRepositoryTests
{
  private static SqlServerDbContext CreateInMemoryContext(string? dbName = null)
  {
    DbContextOptions<SqlServerDbContext> options = new DbContextOptionsBuilder<SqlServerDbContext>()
      .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
      .Options;

    return new SqlServerDbContext(options);
  }

  [Fact]
  public async Task GetByUserNameAsync_WhenUserExists_ReturnsUserWithRoles()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    Role role = new() { Name = "Admin" };
    User user = new() { CompanyId = 1, Email = "user@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
    user.Roles.Add(role);
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    UserRepository sut = new(context);

    User result = await sut.GetByUserNameAsync("user@test.com", CancellationToken.None);

    Assert.Equal("user@test.com", result.Email);
    Assert.Single(result.Roles);
    Assert.Equal("Admin", result.Roles.First().Name);
  }

  [Fact]
  public async Task GetByUserNameAsync_WhenUserDoesNotExist_ReturnsEmptyUser()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    UserRepository sut = new(context);

    User result = await sut.GetByUserNameAsync("missing@test.com", CancellationToken.None);

    Assert.Null(result.Email);
  }

  [Fact]
  public async Task CreateUSerAsync_WhenEmailAlreadyExists_ReturnsEmailAlreadyExists()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    context.Users.Add(new User { CompanyId = 1, Email = "dup@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow });
    await context.SaveChangesAsync(CancellationToken.None);

    UserRepository sut = new(context);

    UserCreationStatus result = await sut.CreateUSerAsync(1, "dup@test.com", "pwd", "Admin", CancellationToken.None);

    Assert.Equal(UserCreationStatus.EMAIL_ALREADY_EXISTS, result);
  }

  [Fact]
  public async Task CreateUSerAsync_WhenRoleNotFound_ReturnsRoleNotFound()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    UserRepository sut = new(context);

    UserCreationStatus result = await sut.CreateUSerAsync(1, "new@test.com", "pwd", "Missing", CancellationToken.None);

    Assert.Equal(UserCreationStatus.ROLE_NOT_FOUND, result);
  }

  [Fact]
  public async Task CreateUSerAsync_WhenDataIsValid_CreatesUserAndReturnsCreated()
  {
    string dbName = Guid.NewGuid().ToString();
    await using (SqlServerDbContext seedContext = CreateInMemoryContext(dbName))
    {
      seedContext.Roles.Add(new Role { Name = "Admin" });
      await seedContext.SaveChangesAsync(CancellationToken.None);
    }

    await using SqlServerDbContext context = CreateInMemoryContext(dbName);
    UserRepository sut = new(context);

    UserCreationStatus result = await sut.CreateUSerAsync(10, "created@test.com", "pwd", "Admin", CancellationToken.None);

    Assert.Equal(UserCreationStatus.CREATED, result);

    User createdUser = await context.Users.Include(u => u.Roles).SingleAsync(u => u.Email.CompareTo("created@test.com") == 0, CancellationToken.None);
    Assert.Equal(10, createdUser.CompanyId);
    Assert.Equal("pwd", createdUser.PasswordHash);
    Assert.True(createdUser.IsActive);
    Assert.Single(createdUser.Roles);
    Assert.Equal("Admin", createdUser.Roles.First().Name);
  }

  [Fact]
  public async Task CreateLogginAttemptAsync_PersistsAttempt()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    User user = new() { CompanyId = 1, Email = "user@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    UserRepository sut = new(context);

    await sut.CreateLogginAttemptAsync(user, true, "127.0.0.1", CancellationToken.None);

    LoginAttempt attempt = await context.LoginAttempts.SingleAsync(CancellationToken.None);
    Assert.Equal(user.Email, attempt.Email);
    Assert.Equal(user.Id, attempt.UserId);
    Assert.True(attempt.Success);
    Assert.Equal("127.0.0.1", attempt.IpAddress);
  }

  [Fact]
  public async Task IssueRefreshTokenAsync_PersistsTokenAndReturnsIt()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    User user = new() { CompanyId = 1, Email = "user@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    UserRepository sut = new(context);
    DateTime before = DateTime.UtcNow;

    string token = await sut.IssueRefreshTokenAsync(user, "10.0.0.1", "token-hash", CancellationToken.None);

    Assert.Equal("token-hash", token);

    RefreshToken stored = await context.RefreshTokens.SingleAsync(CancellationToken.None);
    Assert.Equal(user.Id, stored.UserId);
    Assert.Equal("token-hash", stored.TokenHash);
    Assert.Equal("10.0.0.1", stored.CreatedByIp);
    Assert.InRange(stored.ExpiresAt, before.AddDays(7).AddSeconds(-5), before.AddDays(7).AddSeconds(5));
  }

  [Fact]
  public async Task CreateLogginAttemptAsync_WhenSaveChangesFails_PropagatesException()
  {
    DbContextOptions<SqlServerDbContext> options = new DbContextOptionsBuilder<SqlServerDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    SqlServerDbContext substituteContext = Substitute.ForPartsOf<SqlServerDbContext>(options);
    substituteContext.SaveChangesAsync(Arg.Any<CancellationToken>())
      .ThrowsAsync(new DbUpdateException("Simulated database failure"));

    User user = new() { CompanyId = 1, Email = "user@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
    UserRepository sut = new(substituteContext);

    await Assert.ThrowsAsync<DbUpdateException>(() =>
      sut.CreateLogginAttemptAsync(user, true, "127.0.0.1", CancellationToken.None));
  }

  [Fact]
  public async Task GetAllUsersByCompanyId_ReturnsOnlyUsersForThatCompanyWithRoles()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    Role role = new() { Name = "Admin" };
    User companyUser = new() { CompanyId = 1, Email = "company1@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
    companyUser.Roles.Add(role);
    User otherCompanyUser = new() { CompanyId = 2, Email = "company2@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
    context.Users.AddRange(companyUser, otherCompanyUser);
    await context.SaveChangesAsync(CancellationToken.None);

    UserRepository sut = new(context);

    List<User> result = await sut.GetAllUsersByCompanyId(1, CancellationToken.None);

    User onlyResult = Assert.Single(result);
    Assert.Equal("company1@test.com", onlyResult.Email);
    Assert.Single(onlyResult.Roles);
    Assert.Equal("Admin", onlyResult.Roles.First().Name);
  }

  [Fact]
  public async Task GetAllUsersByCompanyId_WhenNoUsersMatch_ReturnsEmptyList()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    UserRepository sut = new(context);

    List<User> result = await sut.GetAllUsersByCompanyId(999, CancellationToken.None);

    Assert.Empty(result);
  }

  [Fact]
  public async Task GetUserByIdAsync_WhenUserExists_ReturnsUserWithRoles()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    Role role = new() { Name = "Editor" };
    User user = new() { CompanyId = 5, Email = "byid@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
    user.Roles.Add(role);
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    UserRepository sut = new(context);

    User result = await sut.GetUserByIdAsync(5, user.Id, CancellationToken.None);

    Assert.Equal("byid@test.com", result.Email);
    Assert.Single(result.Roles);
    Assert.Equal("Editor", result.Roles.First().Name);
  }

  [Fact]
  public async Task GetUserByIdAsync_WhenCompanyIdDoesNotMatch_ReturnsEmptyUser()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    User user = new() { CompanyId = 5, Email = "byid2@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    UserRepository sut = new(context);

    User result = await sut.GetUserByIdAsync(999, user.Id, CancellationToken.None);

    Assert.Null(result.Email);
  }

  [Fact]
  public async Task GetUserByIdAsync_WhenIdDoesNotExist_ReturnsEmptyUser()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    UserRepository sut = new(context);

    User result = await sut.GetUserByIdAsync(1, 12345, CancellationToken.None);

    Assert.Null(result.Email);
  }

  [Fact]
  public async Task UpdateUserRoles_ReplacesExistingRolesWithMatchingOnesAndIgnoresUnknownNames()
  {
    string dbName = Guid.NewGuid().ToString();
    int userId;
    await using (SqlServerDbContext seedContext = CreateInMemoryContext(dbName))
    {
      Role admin = new() { Name = "Admin" };
      Role editor = new() { Name = "Editor" };
      seedContext.Roles.AddRange(admin, editor);
      User user = new() { CompanyId = 1, Email = "roles@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
      user.Roles.Add(admin);
      seedContext.Users.Add(user);
      await seedContext.SaveChangesAsync(CancellationToken.None);
      userId = user.Id;
    }

    await using SqlServerDbContext context = CreateInMemoryContext(dbName);
    UserRepository sut = new(context);

    await sut.UpdateUserRoles(1, userId, ["Editor", "DoesNotExist"], CancellationToken.None);

    await using SqlServerDbContext verifyContext = CreateInMemoryContext(dbName);
    User updated = await verifyContext.Users.Include(u => u.Roles).SingleAsync(u => u.Id == userId, CancellationToken.None);
    Role onlyRole = Assert.Single(updated.Roles);
    Assert.Equal("Editor", onlyRole.Name);
  }

  [Fact]
  public async Task UpdateUserRoles_WithEmptyRoleList_ClearsAllRoles()
  {
    string dbName = Guid.NewGuid().ToString();
    int userId;
    await using (SqlServerDbContext seedContext = CreateInMemoryContext(dbName))
    {
      Role admin = new() { Name = "Admin" };
      seedContext.Roles.Add(admin);
      User user = new() { CompanyId = 1, Email = "clear@test.com", PasswordHash = "hash", IsActive = true, CreatedAt = DateTime.UtcNow };
      user.Roles.Add(admin);
      seedContext.Users.Add(user);
      await seedContext.SaveChangesAsync(CancellationToken.None);
      userId = user.Id;
    }

    await using SqlServerDbContext context = CreateInMemoryContext(dbName);
    UserRepository sut = new(context);

    await sut.UpdateUserRoles(1, userId, [], CancellationToken.None);

    await using SqlServerDbContext verifyContext = CreateInMemoryContext(dbName);
    User updated = await verifyContext.Users.Include(u => u.Roles).SingleAsync(u => u.Id == userId, CancellationToken.None);
    Assert.Empty(updated.Roles);
  }

  /// <summary>
  /// Documenta el comportamiento actual: GetUserByIdAsync nunca devuelve null
  /// (devuelve un User "vacio" via `entity ?? new()`), por lo que el
  /// `?? throw new InvalidOperationException("User not found")` en
  /// UpdateUserRoles es inalcanzable. Con un usuario inexistente la llamada
  /// no lanza excepcion y no persiste ningun cambio (el User vacio nunca
  /// queda adjunto al DbContext).
  /// </summary>
  [Fact]
  public async Task UpdateUserRoles_WhenUserDoesNotExist_DoesNotThrowAndPersistsNoChanges()
  {
    await using SqlServerDbContext context = CreateInMemoryContext();
    UserRepository sut = new(context);

    Exception? exception = await Record.ExceptionAsync(() =>
      sut.UpdateUserRoles(1, 999, ["Admin"], CancellationToken.None));

    Assert.Null(exception);
    Assert.Empty(context.Users);
  }
}
