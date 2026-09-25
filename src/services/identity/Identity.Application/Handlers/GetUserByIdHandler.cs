using Commons.Mediator;
using Identity.Application.Dto.Responses;
using Identity.Application.Queries;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Identity.Core.Types;
using Mapster;

namespace Identity.Application.Handlers;

public class GetUserByIdHandler(IUserRepository repository) : IQueryHandler<GetUserByIdQuery, UserResponseDto>
{
  private readonly IUserRepository _repository = repository;

  public async Task<UserResponseDto> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
  {
    Optional<User> result = await _repository.GetUserByIdAsync(query.CompanyId, query.UserId, cancellationToken).ConfigureAwait(false);

    if (!result.HasValue)
      return new UserResponseDto(0, string.Empty, false, DateTime.MinValue, []);

    return result.Value.Adapt<UserResponseDto>();
  }
}
