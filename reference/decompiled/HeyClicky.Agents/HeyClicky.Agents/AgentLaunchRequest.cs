using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents;

public sealed record AgentLaunchRequest
{
	public required string Prompt { get; init; }

	public string? DisplayPrompt { get; init; }

	public IReadOnlyList<byte[]>? ImagesJpeg { get; init; }

	public IReadOnlyList<string>? AttachedFilePaths { get; init; }

	public string? ClickySlug { get; init; }

	public string? JobId { get; init; }

	public string? TargetThreadId { get; init; }

	public bool IsHudFollowUp { get; init; }

	public bool StartsNewJobConversation { get; init; }

	public string? NewClickyName { get; init; }

	public string NewClickyRole { get; init; } = "Helper";

	public string? NewClickyOneLiner { get; init; }

	public string? LaunchContext { get; init; }

	public IReadOnlyList<string>? BuildsOnJobIds { get; init; }

	public string LaunchSource { get; init; } = "voice";

	public string UsageContext { get; init; } = "companion";

	public bool IsGeneratedTaskBrief { get; init; }

	public IReadOnlyList<(string User, string Assistant)>? PriorCompanionTurns { get; init; }

	public bool AutoApproveExtraUsage { get; init; }

	public long? ProactiveSuggestionId { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	private AgentLaunchRequest(AgentLaunchRequest original)
	{
		Prompt = original.Prompt;
		DisplayPrompt = original.DisplayPrompt;
		ImagesJpeg = original.ImagesJpeg;
		AttachedFilePaths = original.AttachedFilePaths;
		ClickySlug = original.ClickySlug;
		JobId = original.JobId;
		TargetThreadId = original.TargetThreadId;
		IsHudFollowUp = original.IsHudFollowUp;
		StartsNewJobConversation = original.StartsNewJobConversation;
		NewClickyName = original.NewClickyName;
		NewClickyRole = original.NewClickyRole;
		NewClickyOneLiner = original.NewClickyOneLiner;
		LaunchContext = original.LaunchContext;
		BuildsOnJobIds = original.BuildsOnJobIds;
		LaunchSource = original.LaunchSource;
		UsageContext = original.UsageContext;
		IsGeneratedTaskBrief = original.IsGeneratedTaskBrief;
		PriorCompanionTurns = original.PriorCompanionTurns;
		AutoApproveExtraUsage = original.AutoApproveExtraUsage;
		ProactiveSuggestionId = original.ProactiveSuggestionId;
	}

	public AgentLaunchRequest()
	{
	}
}
