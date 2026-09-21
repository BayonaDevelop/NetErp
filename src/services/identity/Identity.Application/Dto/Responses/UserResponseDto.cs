namespace Identity.Application.Dto.Responses;

public record UserResponseDto(
  long Id,
  string Email,
  bool IsActive,
  DateTime CreatedAt,
  List<string> Roles
);
