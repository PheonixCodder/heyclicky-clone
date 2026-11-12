using System;
using System.Collections.Generic;

namespace HeyClicky.App.Settings;

public sealed class CustomConnectorSecrets
{
	public string? BearerToken { get; set; }

	public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

	public string? RefreshToken { get; set; }

	public DateTimeOffset? ExpiresAt { get; set; }

	public string? TokenEndpoint { get; set; }

	public string? RegisteredClientId { get; set; }

	public string? RegisteredClientSecret { get; set; }

	public string? RegisteredClientRedirectUri { get; set; }
}
