namespace Identity.Application.Dto.Responses;

public record LoginResponseDto(
  string AccessToken, 
  string RefreshToken, 
  string TokenType, 
  int ExpiresIn
);
