using Commons.Mediator;
using Identity.Application.Commands;
using Identity.Application.Dto.Responses;
using Identity.Application.Settings;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Handlers;

public class RefreshTokenHandler(IUserRepository repository, IOptions<Jwt> options) : ICommandHandler<RefreshTokenCommand, LoginResponseDto>
{
  private readonly IUserRepository _repository = repository;

  private static string HashToken(string token) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

  private static (string token, int expiresIn) IssueAccessToken(User user, string signingKey, string issuer, string audience)
  {
    var credentials = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.NormalizedName)));

    var expires = DateTime.UtcNow.AddHours(1);
    var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);

    return (new JwtSecurityTokenHandler().WriteToken(token), 3600);
  }

  private async Task<string> IssueRefreshTokenAsync(User user, string ipAddress, CancellationToken cancellationToken)
  {
    var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    RefreshToken refreshToken = new()
    {
      UserId = user.Id,
      TokenHash = HashToken(rawToken),
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      CreatedByIp = ipAddress,
    };

    await _repository.SaveRefreshTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);
    return rawToken;
  }

  public async Task<LoginResponseDto> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    Jwt settings = options.Value;
    var tokenHash = HashToken(command.Request.RefreshToken);
    var existing = await _repository.GetRefreshTokenAsync(tokenHash, cancellationToken).ConfigureAwait(false);

    if (existing is null || !existing.IsActive)
      return new LoginResponseDto( string.Empty, string.Empty, string.Empty, 0 );

    existing.RevokedAt = DateTime.UtcNow;
    var newRefreshToken = await IssueRefreshTokenAsync(existing.User, command.IpAddress, cancellationToken).ConfigureAwait(false);
    var (accessToken, _) = IssueAccessToken(existing.User, settings.SigningKey!, settings.Issuer!, settings.Audience!);

    return new LoginResponseDto(accessToken, newRefreshToken, "Bearer", settings.DurationInMinutes);
  }
}
