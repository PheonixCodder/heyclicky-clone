using System;

namespace HeyClicky.Core.Tools;

public sealed record AgentThreadInfo(string ThreadId, string? Title, string Preview, DateTimeOffset? UpdatedAt, string Status, string? ClickySlug);
