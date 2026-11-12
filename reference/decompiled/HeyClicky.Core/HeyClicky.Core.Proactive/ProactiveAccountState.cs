using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Proactive;

public sealed class ProactiveAccountState
{
	public List<ProactiveSuggestionCard> Cards { get; set; } = new List<ProactiveSuggestionCard>();

	public List<QueuedProactiveTask> Queue { get; set; } = new List<QueuedProactiveTask>();

	public PendingSuggestionJob? PendingJob { get; set; }

	public ProactiveSlotDayState Slots { get; set; } = new ProactiveSlotDayState(null, null, null);

	public DateTimeOffset? IgnoredSuggestionsBreakUntil { get; set; }

	public DateTimeOffset? SuggestionsLastFetchedAt { get; set; }

	public DateTimeOffset? SuggestionsRowLastSeenAt { get; set; }
}
