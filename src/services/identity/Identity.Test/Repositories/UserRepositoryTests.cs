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
    await context.SaveChangesAsync();

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
    await context.SaveChangesAsync();

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
      await seedContext.SaveChangesAsync();
    }

    await using SqlServerDbContext context = CreateInMemoryContext(dbName);
    UserRepository sut = new(context);

    UserCreationStatus result = await sut.CreateUSerAsync(10, "created@test.com", "pwd", "Admin", CancellationToken.None);

    Assert.Equal(UserCreationStatus.CREATED, result);

    User createdUser = await context.Users.Include(u => u.Roles).SingleAsync(u => u.Email == "created@test.com");
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
    await context.SaveChangesAsync();

    UserRepository sut = new(context);

    await sut.CreateLogginAttemptAsync(user, true, "127.0.0.1", CancellationToken.None);

    LoginAttempt attempt = await context.LoginAttempts.SingleAsync();
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
    await context.SaveChangesAsync();

    UserRepository sut = new(context);
    DateTime before = DateTime.UtcNow;

    string token = await sut.IssueRefreshTokenAsync(user, "10.0.0.1", "token-hash", CancellationToken.None);

    Assert.Equal("token-hash", token);

    RefreshToken stored = await context.RefreshTokens.SingleAsync();
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
}
