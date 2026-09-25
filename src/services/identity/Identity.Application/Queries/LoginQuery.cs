using Commons.Mediator;
using Identity.Application.Dto.Requests;
using Identity.Application.Dto.Responses;

namespace Identity.Application.Queries;

public record LoginQuery(LoginRequestDto Request, string IpAddress) : IQuery<LoginResponseDto>;
