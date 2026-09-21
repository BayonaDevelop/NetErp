using Commons.Mediator;
using Identity.Application.Dto.Responses;

namespace Identity.Application.Query;

public record LoginQuery(string Email, string Password) : IQuery<LoginResponseDto>;
