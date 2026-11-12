using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyJobRun
{
	public string? ThreadId { get; init; }

	public DateTimeOffset StartedAt { get; init; }

	public required string Ask { get; init; }

	public DateTimeOffset? FinishedAt { get; init; }

	public ClickyJobOutcome Outcome { get; init; }

	public string? Summary { get; init; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ClickyJobRun(ClickyJobRun original)
	{
		ThreadId = original.ThreadId;
		StartedAt = original.StartedAt;
		Ask = original.Ask;
		FinishedAt = original.FinishedAt;
		Outcome = original.Outcome;
		Summary = original.Summary;
	}

	public ClickyJobRun()
	{
	}
}
