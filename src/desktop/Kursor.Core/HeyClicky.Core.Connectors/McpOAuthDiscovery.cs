using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Connectors;

public sealed record McpOAuthDiscovery(Uri AuthorizationEndpoint, Uri TokenEndpoint, Uri? RegistrationEndpoint, IReadOnlyList<string> Scopes, string Resource)
{
	public IReadOnlyList<string> ResourceScopes { get; set; } = Array.Empty<string>();
}
