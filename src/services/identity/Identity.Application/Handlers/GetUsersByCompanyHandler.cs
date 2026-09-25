using Commons.Mediator;
using Identity.Application.Dto.Responses;
using Identity.Application.Queries;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Mapster;

namespace Identity.Application.Handlers;

public class GetUsersByCompanyHandler(IUserRepository repository) : IQueryHandler<GetUsersByCompanyQuery, List<UserResponseDto>>
{
  private readonly IUserRepository _repository = repository;

  public async Task<List<UserResponseDto>> HandleAsync(GetUsersByCompanyQuery query, CancellationToken cancellationToken)
  {
    List<User> entities = await _repository
      .GetAllUsersByCompanyIdAsync(query.CompanyId, cancellationToken)
      .ConfigureAwait(false);

    return entities.Adapt<List<User>, List<UserResponseDto>>();
  }
}
