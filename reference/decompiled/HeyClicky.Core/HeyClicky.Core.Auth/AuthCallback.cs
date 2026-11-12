namespace HeyClicky.Core.Auth;

public sealed record AuthCallback(string AccessToken, string? RefreshToken, int? ExpiresIn, string? ReferralCode, string? State = null);
