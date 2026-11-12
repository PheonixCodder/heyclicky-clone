using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyPendingAgentRequest
{
	[JsonConverter(typeof(JsonStringEnumConverter<RequestKind>))]
	public enum RequestKind
	{
		[JsonStringEnumMemberName("computerUse")]
		ComputerUse,
		[JsonStringEnumMemberName("extraUsage")]
		ExtraUsage
	}

	public required string ThreadId { get; init; }

	public RequestKind Kind { get; init; }

	public string RequestText { get; init; } = "";

	public string? LeaseId { get; init; }

	public string? LeaseTurnId { get; init; }

	public string? ClickySlug { get; init; }

	public string? JobId { get; init; }

	public string? Title { get; init; }

	public string Prompt { get; init; } = "";

	public string? FinalSummary { get; init; }

	public DateTimeOffset AskedAt { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ClickyPendingAgentRequest(ClickyPendingAgentRequest original)
	{
		ThreadId = original.ThreadId;
		Kind = original.Kind;
		RequestText = original.RequestText;
		LeaseId = original.LeaseId;
		LeaseTurnId = original.LeaseTurnId;
		ClickySlug = original.ClickySlug;
		JobId = original.JobId;
		Title = original.Title;
		Prompt = original.Prompt;
		FinalSummary = original.FinalSummary;
		AskedAt = original.AskedAt;
	}

	public ClickyPendingAgentRequest()
	{
	}
}
