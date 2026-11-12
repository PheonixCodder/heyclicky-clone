namespace HeyClicky.Agents;

public sealed record AgentRuntimeTimings(long RuntimeReadyMs, long LastLaunchToTurnStartedMs, long LastLaunchToFirstTokenMs, long LastLaunchToFinishedMs);
