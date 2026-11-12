using System.Collections.Generic;

namespace HeyClicky.Agents.Runtime;

public abstract record CustomConnectorTransport
{
	public sealed record Remote(string Url, string? BearerTokenEnvironmentVariable, IReadOnlyDictionary<string, string> HeaderEnvironmentVariables) : CustomConnectorTransport();

	public sealed record Stdio(string Command, IReadOnlyList<string> Arguments, IReadOnlyList<string> PassthroughEnvironmentVariables, string SearchPath) : CustomConnectorTransport();
}
