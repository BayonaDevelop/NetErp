using Commons.Mediator;
using Identity.Application.Dto.Requests;
using Identity.Application.Dto.Responses;

namespace Identity.Application.Commands;

public record RefreshTokenCommand(RefreshTokenRequestDto Request, string IpAddress) : ICommand<LoginResponseDto>;
