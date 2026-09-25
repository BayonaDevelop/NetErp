using Commons.Mediator;
using Identity.Application.Dto.Responses;

namespace Identity.Application.Queries;

public record GetUsersByCompanyQuery(long CompanyId) : IQuery<List<UserResponseDto>>;
