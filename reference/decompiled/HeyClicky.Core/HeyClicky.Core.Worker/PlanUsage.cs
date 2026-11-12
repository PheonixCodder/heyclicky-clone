namespace HeyClicky.Core.Worker;

public sealed record PlanUsage(PlanUsageBucket Messages, PlanUsageBucket Agents, PlanUsageBucket? Dictation, string? WindowResetsAt);
