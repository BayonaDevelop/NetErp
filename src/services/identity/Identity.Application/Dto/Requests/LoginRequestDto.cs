namespace Identity.Application.Dto.Requests;

public record LoginRequestDto(long CompanyId, string Email, string Password);
