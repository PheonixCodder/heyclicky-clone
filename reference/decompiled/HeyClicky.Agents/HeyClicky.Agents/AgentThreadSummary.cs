using System;

namespace HeyClicky.Agents;

public sealed record AgentThreadSummary(string ThreadId, string? Name, string Preview, DateTimeOffset? CreatedAt, DateTimeOffset? UpdatedAt, string Status, string? Cwd, string? ClickySlug);
