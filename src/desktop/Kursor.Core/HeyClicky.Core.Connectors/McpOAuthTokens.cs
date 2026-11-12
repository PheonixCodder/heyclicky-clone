using System;

namespace HeyClicky.Core.Connectors;

public sealed record McpOAuthTokens(string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt);
