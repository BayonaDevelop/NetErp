using Commons.Mediator;
using Identity.Application.Dto.Responses;

namespace Identity.Application.Queries;

public record GetUserByIdQuery(long CompanyId, long UserId) : IQuery<UserResponseDto>;
