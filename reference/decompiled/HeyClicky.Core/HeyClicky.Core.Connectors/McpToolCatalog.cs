using System.Collections.Generic;

namespace HeyClicky.Core.Connectors;

public sealed record McpToolCatalog(string? ServerTitle, IReadOnlyList<string> ToolNames);
