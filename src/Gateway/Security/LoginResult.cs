namespace Gateway.Security;

internal sealed record LoginResult(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
