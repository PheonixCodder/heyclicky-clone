using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public sealed record QueuedProactiveTask(ProactiveSuggestionCard Card, string ClickySlug, string? AdjustmentText, bool WasFounded, bool WasRestored)
{
	public Guid Id => Card.Id;

	public bool IsPaused { get; init; }

	public bool IsLaunching { get; init; }

	public static IReadOnlyList<QueuedProactiveTask> NextCandidates(IReadOnlyList<QueuedProactiveTask> entries)
	{
		HashSet<string> seen = new HashSet<string>();
		return entries.Where((QueuedProactiveTask e) => seen.Add(e.ClickySlug) && !e.IsPaused && !e.IsLaunching).ToList();
	}
}
