using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public sealed record MorningSuggestionsNotice(Guid Id, IReadOnlySet<Guid> CardIds, DateTimeOffset CreatedAt, DateTimeOffset PresentNotBefore, int? PreviewCardCount = null)
{
	public bool IsDebugPreview => PreviewCardCount.HasValue;

	public int RemainingCardCount(IReadOnlyList<ProactiveSuggestionCard> cards, DateTimeOffset now)
	{
		int? previewCardCount = PreviewCardCount;
		if (previewCardCount.HasValue)
		{
			int valueOrDefault = previewCardCount.GetValueOrDefault();
			if (CardIds.Count == 0)
			{
				return valueOrDefault;
			}
		}
		return cards.Count((ProactiveSuggestionCard c) => CardIds.Contains(c.Id) && !c.IsExpired(now));
	}
}
