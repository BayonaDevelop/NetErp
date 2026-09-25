namespace Gateway.Security;

public sealed class JwtSettings
{
  public const string SectionName = "Jwt";

  public string? SigningKey { get; set; }

  public string? Issuer { get; set; }

  public string? Audience { get; set; }
}
