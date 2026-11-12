using System.Collections.Generic;

namespace HeyClicky.Agents.Harness;

public sealed record AgentHarnessMcpServer(string Name, string Url, string? BearerToken)
{
	public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

	public bool SendsTurnIdentity { get; init; }

	public AgentHarnessLocalMcpCommand? LocalCommand { get; init; }
}
