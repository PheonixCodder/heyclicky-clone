using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public static class DockOrder
{
	public static string JobSlotId(string jobId)
	{
		return "job-slot:" + jobId;
	}

	public static IReadOnlyList<DockWindowKey> NewestFirst(IReadOnlyList<string> clickySlugs, IReadOnlyList<ClickyJob> jobs, Func<string, DockRecency> recency)
	{
		List<(DockWindowKey, DockRecency, int)> list = new List<(DockWindowKey, DockRecency, int)>();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string clickySlug in clickySlugs)
		{
			string text = "clicky:" + clickySlug;
			if (hashSet.Add(text))
			{
				list.Add((new DockWindowKey(clickySlug, text, clickySlug, null), recency(clickySlug), list.Count));
			}
		}
		foreach (ClickyJob job in jobs)
		{
			string text2 = JobSlotId(job.Id);
			if (hashSet.Add(text2))
			{
				list.Add((new DockWindowKey(job.RailKey, text2, null, job), recency(job.RailKey), list.Count));
			}
		}
		list.Sort(((DockWindowKey Window, DockRecency Recency, int Order) a, (DockWindowKey Window, DockRecency Recency, int Order) b) => (!(a.Recency != b.Recency)) ? a.Order.CompareTo(b.Order) : b.Recency.CompareTo(a.Recency));
		return list.Select<(DockWindowKey, DockRecency, int), DockWindowKey>(((DockWindowKey Window, DockRecency Recency, int Order) w) => w.Window).ToList();
	}

	public static IReadOnlyList<DockWindowKey> DeckOrder(IReadOnlyList<string> clickySlugs, IReadOnlyList<ClickyJob> jobs, Func<string, DockRecency> recency)
	{
		List<DockWindowKey> list = NewestFirst(clickySlugs, jobs, recency).ToList();
		list.Reverse();
		return list;
	}

	public static string? NextFrontKeyAfterRemoving(IReadOnlyList<string> deckKeys, string removed)
	{
		int num = deckKeys.ToList().IndexOf(removed);
		if (num < 0)
		{
			return null;
		}
		if (num + 1 >= deckKeys.Count)
		{
			if (num <= 0)
			{
				return null;
			}
			return deckKeys[num - 1];
		}
		return deckKeys[num + 1];
	}
}
