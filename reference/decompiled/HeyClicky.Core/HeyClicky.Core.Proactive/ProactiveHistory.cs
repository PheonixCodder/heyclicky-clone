using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public static class ProactiveHistory
{
	public const int MaximumEntries = 80;

	public static readonly TimeSpan MaximumEntryAge = TimeSpan.FromDays(30);

	private const int MaximumDecidedWireEntries = 40;

	private const int MaximumUndecidedWireEntries = 10;

	public static List<ProactiveHistoryEntry> Record(IReadOnlyList<ProactiveHistoryEntry> entries, ProactiveHistoryEntry entry, DateTimeOffset now)
	{
		List<ProactiveHistoryEntry> list = Recent(entries, now).ToList();
		list.Insert(0, entry);
		return list.Take(80).ToList();
	}

	public static List<ProactiveHistoryEntry> RecordUndecided(IReadOnlyList<ProactiveHistoryEntry> entries, ProactiveSuggestionCard card, ProactiveResolution resolution, DateTimeOffset now)
	{
		if (card.ViewedAt.HasValue)
		{
			return Record(entries, new ProactiveHistoryEntry(card.Title, card.Description, resolution, now, null, true), now);
		}
		return entries.ToList();
	}

	public static IEnumerable<ProactiveHistoryEntry> Recent(IReadOnlyList<ProactiveHistoryEntry> entries, DateTimeOffset now)
	{
		return entries.Where((ProactiveHistoryEntry e) => e.DecidedAt > now - MaximumEntryAge);
	}

	public static IReadOnlyList<ProactiveHistoryEntry> WireEntries(IReadOnlyList<ProactiveHistoryEntry> entries, DateTimeOffset now)
	{
		List<ProactiveHistoryEntry> source = Recent(entries, now).ToList();
		IEnumerable<ProactiveHistoryEntry> first = source.Where(delegate(ProactiveHistoryEntry e)
		{
			ProactiveResolution resolution = e.Resolution;
			return (uint)resolution <= 1u;
		}).Take(40);
		IEnumerable<ProactiveHistoryEntry> second = source.Where(delegate(ProactiveHistoryEntry e)
		{
			ProactiveResolution resolution2 = e.Resolution;
			return (uint)(resolution2 - 2) <= 1u && e.WasViewed == true;
		}).Take(10);
		return (from e in first.Concat(second)
			orderby e.DecidedAt descending
			select e).ToList();
	}
}
