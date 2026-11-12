using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents.Backend;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.Agents.Turns;

internal sealed class AgentTask
{
	public readonly object Gate = new object();

	private readonly Dictionary<string, int> toolActivityIndex = new Dictionary<string, int>();

	private readonly HashSet<string> completedCommentaryItemIds = new HashSet<string>();

	public long InputTokens;

	public long CachedInputTokens;

	public long OutputTokens;

	public string Id { get; } = Guid.NewGuid().ToString("D");

	public Guid NumericId => Guid.Parse(Id);

	public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;

	public string? ClickySlug { get; set; }

	public string? ClickyName { get; set; }

	public string? JobId { get; set; }

	public string? WorkspacePath { get; set; }

	public string? ThreadId { get; set; }

	public string? TurnId { get; set; }

	public string Title { get; set; } = "HeyClicky Agent Task";

	public AgentTaskStatus Status { get; set; }

	public string ProgressSummary { get; set; } = "Launching Agent Mode...";

	public string? ThinkingHeading { get; set; }

	public bool IsRoutineRun { get; set; }

	public bool IsSentFromItsChat { get; set; }

	public List<string> SitesReadThisTurn { get; } = new List<string>();

	public string LaunchSource { get; set; } = "voice";

	public bool IsDemo { get; set; }

	public long? ProactiveSuggestionId { get; set; }

	public string? PendingInitialMessageLog { get; set; }

	public List<string> UserPrompts { get; } = new List<string>();

	public List<ActivityRow> Activity { get; } = new List<ActivityRow>();

	public StringBuilder StreamingCommentary { get; } = new StringBuilder();

	public StringBuilder ResponseAccumulator { get; } = new StringBuilder();

	public string? CurrentMessagePhase { get; set; }

	public string? CurrentNarrationItemId { get; set; }

	public int? ReasoningSummaryIndex { get; set; }

	public StringBuilder ReasoningSummaryText { get; } = new StringBuilder();

	public string LatestCommentary { get; set; } = "";

	public List<string> CompletedReplies { get; } = new List<string>();

	public IReadOnlyList<AgentPlanStep> Plan { get; set; } = Array.Empty<AgentPlanStep>();

	public string? FinalResponseMarkdown { get; set; }

	public string? Summary { get; set; }

	public string? DoneTitle { get; set; }

	public IReadOnlyList<string> NextActions { get; set; } = Array.Empty<string>();

	public IReadOnlyList<string> Artifacts { get; set; } = Array.Empty<string>();

	public ClickyAnswer? Answer { get; set; }

	public ClickyJobUserStep? PendingUserStep { get; set; }

	public IReadOnlyList<string> LearnedHowTos { get; set; } = Array.Empty<string>();

	public string? PendingComputerUseRequest { get; set; }

	public string? FailureMessage { get; set; }

	public string? FailureDiagnostic { get; set; }

	public bool InterruptedUnexpectedly { get; set; }

	public CompanionError? Error { get; set; }

	public AgentTurnLease? Lease { get; set; }

	public (string Model, string Effort)? DeveloperModel { get; set; }

	public DateTimeOffset? LeaseRecordedAt { get; set; }

	public bool RequiresExtraUsageApproval { get; set; }

	public bool DidRefundEarlyStop { get; set; }

	public HashSet<string> IntegrationsUsedThisTurn { get; } = new HashSet<string>();

	public bool NeedsChromeConnection { get; set; }

	public bool FoundChromeNotConnectedThisTurn { get; set; }

	public bool IsNewJobRun { get; set; }

	public TaskCompletionSource<string>? TurnCompletion { get; set; }

	public CancellationTokenSource? TurnCts { get; set; }

	public bool HoldsSlot { get; set; }

	public long LaunchTimestamp { get; set; }

	public long TurnStartedTimestamp { get; set; }

	public long FirstTokenTimestamp { get; set; }

	public bool IsActive
	{
		get
		{
			AgentTaskStatus status = Status;
			if ((uint)status <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public void NoteSitesRead(IEnumerable<string> hosts)
	{
		foreach (string host in hosts)
		{
			string text = host.Trim().ToLowerInvariant();
			string text2;
			if (!text.StartsWith("www.", StringComparison.Ordinal))
			{
				text2 = text;
			}
			else
			{
				string text3 = text;
				text2 = text3.Substring(4, text3.Length - 4);
			}
			string text4 = text2;
			if (text4.Length != 0 && text4.Contains('.') && !SitesReadThisTurn.Contains(text4) && SitesReadThisTurn.Count < 60)
			{
				SitesReadThisTurn.Add(text4);
			}
		}
	}

	public void ResetForNewTurn(string prompt)
	{
		UserPrompts.Add(prompt);
		TurnId = null;
		Status = AgentTaskStatus.Starting;
		ProgressSummary = "Launching Agent Mode...";
		ThinkingHeading = null;
		StreamingCommentary.Clear();
		ResponseAccumulator.Clear();
		CurrentMessagePhase = null;
		CurrentNarrationItemId = null;
		ReasoningSummaryIndex = null;
		ReasoningSummaryText.Clear();
		CompletedReplies.Clear();
		SitesReadThisTurn.Clear();
		Plan = Array.Empty<AgentPlanStep>();
		FinalResponseMarkdown = null;
		Summary = null;
		DoneTitle = null;
		NextActions = Array.Empty<string>();
		Artifacts = Array.Empty<string>();
		Answer = null;
		PendingUserStep = null;
		LearnedHowTos = Array.Empty<string>();
		FailureMessage = null;
		FailureDiagnostic = null;
		InterruptedUnexpectedly = false;
		Error = null;
		RequiresExtraUsageApproval = false;
		DidRefundEarlyStop = false;
		IntegrationsUsedThisTurn.Clear();
		NeedsChromeConnection = false;
		FoundChromeNotConnectedThisTurn = false;
		FirstTokenTimestamp = 0L;
		TurnStartedTimestamp = 0L;
	}

	public string? FinishStreamingCommentary(string? itemId, string? fallbackText = null)
	{
		if (itemId != null && completedCommentaryItemIds.Contains(itemId))
		{
			return null;
		}
		string text = fallbackText?.Trim() ?? "";
		string text2 = CitationMarkers.Strip((text.Length == 0) ? StreamingCommentary.ToString() : text).Trim();
		StreamingCommentary.Clear();
		if (text2.Length == 0)
		{
			return null;
		}
		Activity.Add(new ActivityRow(Guid.NewGuid().ToString("N"), AgentActivityKind.Commentary, text2, null, DateTimeOffset.Now));
		if (itemId != null)
		{
			completedCommentaryItemIds.Add(itemId);
		}
		LatestCommentary = text2;
		return text2;
	}

	public void RecordToolActivity(string? itemId, string text, bool isRunning)
	{
		if (itemId != null && toolActivityIndex.TryGetValue(itemId, out var value) && value < Activity.Count)
		{
			Activity[value] = Activity[value]with
			{
				Text = text,
				IsRunning = isRunning
			};
			return;
		}
		Activity.Add(new ActivityRow(itemId ?? Guid.NewGuid().ToString("N"), AgentActivityKind.ToolCall, text, isRunning, DateTimeOffset.Now));
		if (itemId != null)
		{
			toolActivityIndex[itemId] = Activity.Count - 1;
		}
	}

	public void AddActivity(AgentActivityKind kind, string text)
	{
		if (Activity.Count > 0)
		{
			List<ActivityRow> activity = Activity;
			if (activity[activity.Count - 1].Kind == kind && kind == AgentActivityKind.ShellOutput)
			{
				List<ActivityRow> activity2 = Activity;
				int index = activity2.Count - 1;
				List<ActivityRow> activity3 = Activity;
				activity2[index] = activity3[activity3.Count - 1]with
				{
					Text = text,
					At = DateTimeOffset.Now
				};
				return;
			}
		}
		Activity.Add(new ActivityRow(Guid.NewGuid().ToString("N"), kind, text, null, DateTimeOffset.Now));
		if (Activity.Count > 400)
		{
			Activity.RemoveRange(0, Activity.Count - 400);
		}
	}

	public AgentTaskSnapshot Snapshot()
	{
		string text = ((StreamingCommentary.Length > 0) ? CitationMarkers.Strip(StreamingCommentary.ToString()) : LatestCommentary);
		if (text.Length == 0)
		{
			text = ProgressSummary;
		}
		string title = ((Status == AgentTaskStatus.Finished && DoneTitle != null) ? DoneTitle : Title);
		string finalResponseMarkdown = FinalResponseMarkdown ?? ((ResponseAccumulator.Length > 0 && Status == AgentTaskStatus.Running) ? StructuredFinalReply.Parse(ResponseAccumulator.ToString()).Text : null);
		return new AgentTaskSnapshot(Id, ThreadId, title, Status, text, finalResponseMarkdown, StartedAt);
	}

	public AgentTaskDetail Detail()
	{
		return new AgentTaskDetail(Snapshot(), ClickySlug, ClickyName, WorkspacePath, TurnId, ProgressSummary, ThinkingHeading, CitationMarkers.Strip(ResponseAccumulator.ToString()), CitationMarkers.Strip(StreamingCommentary.ToString()), Activity.Select((ActivityRow a) => new AgentActivityItem(a.Id, a.Kind, a.Text, a.IsRunning, a.At)).ToList(), UserPrompts.ToList(), Summary, DoneTitle, NextActions, Artifacts, FailureMessage, Error, RequiresExtraUsageApproval, DidRefundEarlyStop, Lease, InputTokens, CachedInputTokens, OutputTokens, IsRoutineRun, IsDemo, LaunchSource, JobId, Plan, Answer, PendingUserStep, TurnId?.StartsWith("harness-turn-", StringComparison.Ordinal) ?? false, IsNewJobRun, IsSentFromItsChat, SitesReadThisTurn.ToList(), FailureDiagnostic, InterruptedUnexpectedly);
	}
}
