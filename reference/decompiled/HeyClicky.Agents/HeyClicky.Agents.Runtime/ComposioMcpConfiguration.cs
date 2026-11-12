using System.Collections.Generic;

namespace HeyClicky.Agents.Runtime;

public sealed record ComposioMcpConfiguration(string Url, IReadOnlyList<string> Toolkits, IReadOnlyList<string> MultiAccountLines);
