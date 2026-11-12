namespace HeyClicky.Agents;

public sealed record AgentTaskEvent(string TaskId, AgentTaskEventKind Kind, string? Text = null);
