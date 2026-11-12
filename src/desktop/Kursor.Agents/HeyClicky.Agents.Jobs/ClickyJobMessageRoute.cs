namespace HeyClicky.Agents.Jobs;

public sealed record ClickyJobMessageRoute(string? TargetThreadId, string? NewRunFollowsThreadId, bool ResumesFinishedRun = false);
