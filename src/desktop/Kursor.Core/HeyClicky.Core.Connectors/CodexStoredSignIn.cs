using System;

namespace HeyClicky.Core.Connectors;

public sealed record CodexStoredSignIn(string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt, string? ClientId, string? ClientSecret);
