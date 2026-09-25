namespace Identity.Application.Settings;

public record Jwt
{
  public string? SigningKey { get; set; }
  public string? Issuer { get; set; }
  public string? Audience { get; set; }
  public int DurationInMinutes { get; set; }
}
