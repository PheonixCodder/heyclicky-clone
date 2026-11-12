using System.Collections.Generic;
using HeyClicky.Agents.Backend;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.Agents;

public sealed record AgentTaskDetail(AgentTaskSnapshot Snapshot, string? ClickySlug, string? ClickyName, string? WorkspacePath, string? TurnId, string ProgressSummary, string? ThinkingHeading, string StreamingResponse, string StreamingCommentary, IReadOnlyList<AgentActivityItem> Activity, IReadOnlyList<string> UserPrompts, string? Summary, string? DoneTitle, IReadOnlyList<string> NextActions, IReadOnlyList<string> Artifacts, string? FailureMessage, CompanionError? Error, bool RequiresExtraUsageApproval, bool DidRefundEarlyStop, AgentTurnLease? Lease, long InputTokens, long CachedInputTokens, long OutputTokens, bool IsRoutineRun, bool IsDemo = false, string LaunchSource = "voice", string? JobId = null, IReadOnlyList<AgentPlanStep>? Plan = null, ClickyAnswer? Answer = null, ClickyJobUserStep? PendingUserStep = null, bool IsHarnessTurn = false, bool IsNewJobRun = false, bool IsSentFromItsChat = false, IReadOnlyList<string>? SitesRead = null, string? FailureDiagnostic = null, bool InterruptedUnexpectedly = false);
