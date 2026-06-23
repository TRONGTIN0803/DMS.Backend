namespace DMS.Application.Auth;

public sealed record LoginRequest(string UserName, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserResponse User);

public sealed record AuthenticatedUserResponse(
    long Id,
    string UserName,
    string FullName,
    string Role);
