using System;

namespace HeyClicky;

public sealed record AgentTaskSnapshot(string TaskId, string? ThreadId, string Title, AgentTaskStatus Status, string LatestCommentary, string? FinalResponseMarkdown, DateTimeOffset StartedAt);
