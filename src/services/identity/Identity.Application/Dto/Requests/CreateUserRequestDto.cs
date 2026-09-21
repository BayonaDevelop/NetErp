namespace Identity.Application.Dto.Requests;

public record CreateUserRequestDto(long CompanyId, string Email, string Password, string Role);
