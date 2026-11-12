using System;

namespace HeyClicky.Core.Billing;

public sealed record AgentTurnUsageRecord(string TaskId, string? ThreadId, string? TurnId, string? ClickySlug, string? ClickyName, DateTimeOffset FinishedAt, string Model, string ReasoningEffort, long InputTokens, long CachedInputTokens, long OutputTokens, string? LeaseId = null, double? BilledCostUsd = null);
