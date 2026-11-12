using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyAppLiveStatus(ClickyAppLiveStatus.Phases Phase, string Line, IReadOnlyList<string> ProgressLines, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt)
{
	public enum Phases
	{
		Idle,
		Working,
		NeedsYou,
		Finished,
		Failed
	}

	public static readonly ClickyAppLiveStatus Idle = new ClickyAppLiveStatus(Phases.Idle, "", Array.Empty<string>(), null, null);

	[CompilerGenerated]
	private ClickyAppLiveStatus(ClickyAppLiveStatus original)
	{
		Phase = original.Phase;
		Line = original.Line;
		ProgressLines = original.ProgressLines;
		StartedAt = original.StartedAt;
		FinishedAt = original.FinishedAt;
	}
}
