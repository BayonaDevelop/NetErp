using Commons.Mediator;
using Identity.Application.Dto.Requests;

namespace Identity.Application.Commands;

public record LogoutCommand(RefreshTokenRequestDto Request) : ICommand<bool>;
