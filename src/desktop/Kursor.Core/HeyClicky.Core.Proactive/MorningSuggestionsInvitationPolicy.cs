using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public static class MorningSuggestionsInvitationPolicy
{
	public enum RebuildKind
	{
		NothingToDo,
		SettleForToday,
		Rebuild
	}

	public readonly record struct RebuildDecision(RebuildKind Kind, IReadOnlySet<Guid>? CardIds = null, DateTimeOffset CreatedAt = default(DateTimeOffset), DateTimeOffset PresentNotBefore = default(DateTimeOffset));

	public static readonly TimeSpan DelayAfterGreeting = TimeSpan.FromSeconds(30L);

	public static readonly TimeSpan SnoozeAfterPreemption = TimeSpan.FromMinutes(5L);

	public const int ConsecutiveDismissalDaysBeforeRest = 3;

	public static readonly TimeSpan RestAfterRepeatedDismissals = TimeSpan.FromDays(5);

	public static bool IsResting(MorningDismissalHistory history, DateTimeOffset now)
	{
		DateTimeOffset? restingUntil = history.RestingUntil;
		if (restingUntil.HasValue)
		{
			DateTimeOffset valueOrDefault = restingUntil.GetValueOrDefault();
			return now < valueOrDefault;
		}
		return false;
	}

	public static MorningDismissalHistory RecordingDismissal(MorningDismissalHistory history, DateTimeOffset now)
	{
		DateTimeOffset? restingUntil = history.RestingUntil;
		DateTimeOffset? obj;
		if (restingUntil.HasValue)
		{
			DateTimeOffset valueOrDefault = restingUntil.GetValueOrDefault();
			if (now >= valueOrDefault)
			{
				obj = null;
				goto IL_0034;
			}
		}
		obj = history.RestingUntil;
		goto IL_0034;
		IL_0034:
		DateTimeOffset? restingUntil2 = obj;
		DateTime date = now.LocalDateTime.Date;
		List<DateTime> list = history.DismissedDays.ToList();
		if (list.Count != 0)
		{
			List<DateTime> list2 = list;
			if (!(list2[list2.Count - 1] != date))
			{
				goto IL_0077;
			}
		}
		list.Add(date);
		goto IL_0077;
		IL_0077:
		list = list.TakeLast(3).ToList();
		if (list.Count != 3 || !list.Zip(list.Skip(1)).All(((DateTime First, DateTime Second) p) => (p.Second - p.First).TotalDays == 1.0))
		{
			return new MorningDismissalHistory(list, restingUntil2);
		}
		return new MorningDismissalHistory(new List<DateTime>(), now + RestAfterRepeatedDismissals);
	}

	public static MorningDismissalHistory RecordingApproval(MorningDismissalHistory history)
	{
		return MorningDismissalHistory.Empty;
	}

	public static IReadOnlyList<ProactiveSuggestionCard> QualifyingMorningCards(IReadOnlyList<ProactiveSuggestionCard> cards, DateTimeOffset now)
	{
		return cards.Where((ProactiveSuggestionCard c) => c.CheckReason == ProactiveCheckReason.Morning && c.CreatedAt.LocalDateTime.Date == now.LocalDateTime.Date && !c.IsExpired(now)).ToList();
	}

	public static bool IsSettled(DateTimeOffset? settledAt, DateTimeOffset now)
	{
		if (settledAt.HasValue)
		{
			return settledAt.GetValueOrDefault().LocalDateTime.Date == now.LocalDateTime.Date;
		}
		return false;
	}

	public static DateTimeOffset AnchorDate(DateTimeOffset? greetingPresentedAt, DateTimeOffset? sweepStartedAt, DateTimeOffset? batchLandedAt, DateTimeOffset now)
	{
		if (greetingPresentedAt.HasValue)
		{
			DateTimeOffset valueOrDefault = greetingPresentedAt.GetValueOrDefault();
			if (valueOrDefault.LocalDateTime.Date == now.LocalDateTime.Date)
			{
				return valueOrDefault;
			}
		}
		DateTimeOffset?[] array = new DateTimeOffset?[2] { sweepStartedAt, batchLandedAt };
		for (int i = 0; i < array.Length; i++)
		{
			DateTimeOffset? dateTimeOffset = array[i];
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault2 = dateTimeOffset.GetValueOrDefault();
				if (valueOrDefault2.LocalDateTime.Date == now.LocalDateTime.Date && valueOrDefault2 <= now)
				{
					return valueOrDefault2;
				}
			}
		}
		return now;
	}

	public static RebuildDecision Decide(bool hasActiveInvitation, bool isLaneEnabled, DateTimeOffset? settledAt, bool isReadingSuggestions, IReadOnlyList<ProactiveSuggestionCard> cards, DateTimeOffset? greetingPresentedAt, DateTimeOffset? sweepStartedAt, DateTimeOffset now, bool isRestingAfterRepeatedDismissals = false)
	{
		if (hasActiveInvitation || !isLaneEnabled || isRestingAfterRepeatedDismissals || IsSettled(settledAt, now))
		{
			return new RebuildDecision(RebuildKind.NothingToDo);
		}
		IReadOnlyList<ProactiveSuggestionCard> readOnlyList = QualifyingMorningCards(cards, now);
		if (readOnlyList.Count == 0)
		{
			return new RebuildDecision(RebuildKind.NothingToDo);
		}
		if (isReadingSuggestions)
		{
			return new RebuildDecision(RebuildKind.SettleForToday);
		}
		DateTimeOffset dateTimeOffset = readOnlyList.Min((ProactiveSuggestionCard c) => c.CreatedAt);
		DateTimeOffset dateTimeOffset2 = AnchorDate(greetingPresentedAt, sweepStartedAt, dateTimeOffset, now);
		return new RebuildDecision(RebuildKind.Rebuild, readOnlyList.Select((ProactiveSuggestionCard c) => c.Id).ToHashSet(), dateTimeOffset, dateTimeOffset2 + DelayAfterGreeting);
	}
}
