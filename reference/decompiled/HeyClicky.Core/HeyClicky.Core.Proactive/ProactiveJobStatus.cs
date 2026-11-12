using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Proactive;

public sealed record ProactiveJobStatus(string JobId, ProactiveJobState State, IReadOnlyList<ProactiveSuggestionCard>? Cards, string? Error, DateTimeOffset? BreakUntil)
{
	public bool IsSettled
	{
		get
		{
			ProactiveJobState state = State;
			if ((uint)(state - 2) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public string? CuratedEvidence { get; init; }

	public string? MorningHeadline { get; init; }

	public string? MorningHeadlineLine { get; init; }
}
