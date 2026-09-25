using Identity.Application.Dto.Responses;
using Identity.Application.Handlers;
using Identity.Application.Mappers;
using Identity.Application.Queries;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Identity.Core.Types;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Test.Handlers;

/// <summary>
/// Igual que GetUsersByCompanyHandlerTests, registra MappingConfig aqui para
/// reproducir la composicion real (Identity.Api.Program la ejecuta al
/// arrancar) ya que el mapeo User -&gt; UserResponseDto depende de ella.
/// </summary>

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class GetUserByIdHandlerTests
{
  static GetUserByIdHandlerTests() => MappingConfig.RegisterMappings();

  [Fact]
  public async Task HandleAsync_WhenUserExists_ReturnsMappedUserWithNormalizedRoles()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    User user = new()
    {
      Id = 8,
      CompanyId = 5,
      Email = "byid@test.com",
      PasswordHash = "hash",
      IsActive = true,
      CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
      Roles = [new Role { Name = "Admin", NormalizedName = "ROLE_ADMIN" }]
    };
    repository.GetUserByIdAsync(5, 8, Arg.Any<CancellationToken>()).Returns(Optional<User>.Some(user));

    GetUserByIdHandler sut = new(repository);
    GetUserByIdQuery query = new(5, 8);

    UserResponseDto result = await sut.HandleAsync(query, CancellationToken.None);

    Assert.Equal(user.Id, result.Id);
    Assert.Equal(user.Email, result.Email);
    Assert.True(result.IsActive);
    Assert.Equal(user.CreatedAt, result.CreatedAt);
    Assert.Equal(["ROLE_ADMIN"], result.Roles);
  }

  [Fact]
  public async Task HandleAsync_WhenUserDoesNotExist_ReturnsEmptyResponseDto()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    repository.GetUserByIdAsync(1, 999, Arg.Any<CancellationToken>()).Returns(Optional<User>.None());

    GetUserByIdHandler sut = new(repository);
    GetUserByIdQuery query = new(1, 999);

    UserResponseDto result = await sut.HandleAsync(query, CancellationToken.None);

    Assert.Equal(0, result.Id);
    Assert.Equal(string.Empty, result.Email);
    Assert.False(result.IsActive);
    Assert.Equal(DateTime.MinValue, result.CreatedAt);
    Assert.Empty(result.Roles);
  }
}
