using Commons.Mediator;
using Identity.Application.Dto.Responses;
using Identity.Application.Queries;
using Identity.Application.Settings;
using Identity.Core.Entities;
using Identity.Core.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Handlers;

public class LoginHandler(IUserRepository repository, IPasswordHasher<User> hasher, IOptions<Jwt> options) : IQueryHandler<LoginQuery, LoginResponseDto>
{
  private readonly IUserRepository _repository = repository;
  private readonly IPasswordHasher<User> _hasher = hasher;

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
    claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name)));

    var expires = DateTime.UtcNow.AddHours(1);
    var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);

    return (new JwtSecurityTokenHandler().WriteToken(token), 3600);
  }

  public async Task<LoginResponseDto> HandleAsync(LoginQuery query, CancellationToken cancellationToken)
  {
    Jwt settings = options.Value;
    User user = await _repository.GetByUserNameAsync(query.Request.Email, cancellationToken).ConfigureAwait(false);

    if (user.Email == null)
    {
      return new LoginResponseDto(string.Empty, string.Empty, string.Empty, 0);
    }

    bool success =
      _hasher.VerifyHashedPassword(user, user.PasswordHash, query.Request.Password) == PasswordVerificationResult.Success;

    await _repository
      .CreateLogginAttemptAsync(user!, success, query.IpAddress, cancellationToken)
      .ConfigureAwait(false);

    if (success)
    {
      var (accessToken, _) = IssueAccessToken(user!, settings.SigningKey!, settings.Issuer!, settings.Audience!);
      string rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
      var refreshToken = await _repository.IssueRefreshTokenAsync(user!, query.IpAddress, HashToken(rawToken), cancellationToken).ConfigureAwait(false);


      return new LoginResponseDto(accessToken, refreshToken, "Bearer", settings.DurationInMinutes);
    }
    else
      return new LoginResponseDto(string.Empty, string.Empty, string.Empty, 0);
  }


}
