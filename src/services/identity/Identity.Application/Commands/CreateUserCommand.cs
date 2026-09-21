using Commons.Mediator;
using Identity.Application.Dto.Requests;

namespace Identity.Application.Commands;

public record CreateUserCommand(
  CreateUserRequestDto Request,
  string ipAddress
) : ICommand<bool>;
