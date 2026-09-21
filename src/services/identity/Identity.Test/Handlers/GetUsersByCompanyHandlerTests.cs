using Identity.Application.Dto.Responses;
using Identity.Application.Handlers;
using Identity.Application.Mappers;
using Identity.Application.Queries;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;

namespace Identity.Test.Handlers;

/// <summary>
/// El mapeo User -&gt; UserResponseDto (Roles: List&lt;Role&gt; -&gt; List&lt;string&gt;, usando
/// Role.NormalizedName, no Role.Name) depende de que
/// MappingConfig.RegisterMappings() se haya ejecutado (lo hace
/// Identity.Api.Program al arrancar). Se invoca aqui para reproducir esa
/// composicion en un test aislado, igual que hace el proceso real.
/// </summary>

[SuppressMessage("Style", "IDE0079:Remove unnecessary suppression", Justification = "La supresión CRR0029 es necesaria porque se aplica en Testing")]
[SuppressMessage("Async", "CRR0029:ConfigureAwait unnecessary", Justification = "En el caso de Testing no es necesario especificar el valor de ConfigureAwait.")]
public class GetUsersByCompanyHandlerTests
{
  static GetUsersByCompanyHandlerTests() => MappingConfig.RegisterMappings();

  [Fact]
  public async Task HandleAsync_MapsUsersToResponseDtosUsingNormalizedRoleNames()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    User user = new()
    {
      Id = 3,
      CompanyId = 5,
      Email = "company-user@test.com",
      PasswordHash = "hash",
      IsActive = true,
      CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
      Roles = [new Role { Name = "Admin", NormalizedName = "ROLE_ADMIN" }, new Role { Name = "Editor", NormalizedName = "ROLE_EDITOR" }]
    };
    repository.GetAllUsersByCompanyIdAsync(5, Arg.Any<CancellationToken>()).Returns([user]);

    GetUsersByCompanyHandler sut = new(repository);
    GetUsersByCompanyQuery query = new(5);

    List<UserResponseDto> result = await sut.HandleAsync(query, CancellationToken.None);

    UserResponseDto dto = Assert.Single(result);
    Assert.Equal(user.Id, dto.Id);
    Assert.Equal(user.Email, dto.Email);
    Assert.True(dto.IsActive);
    Assert.Equal(user.CreatedAt, dto.CreatedAt);
    Assert.Equal(["ROLE_ADMIN", "ROLE_EDITOR"], dto.Roles);
  }

  [Fact]
  public async Task HandleAsync_WhenNoUsersExist_ReturnsEmptyList()
  {
    IUserRepository repository = Substitute.For<IUserRepository>();
    repository.GetAllUsersByCompanyIdAsync(99, Arg.Any<CancellationToken>()).Returns([]);

    GetUsersByCompanyHandler sut = new(repository);
    GetUsersByCompanyQuery query = new(99);

    List<UserResponseDto> result = await sut.HandleAsync(query, CancellationToken.None);

    Assert.Empty(result);
  }
}
