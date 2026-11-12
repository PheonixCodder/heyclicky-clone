using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

internal static class ClickyReadMarks
{
	private const string DefaultsKey = "clicky.dock.clickyLastSeen.v1";

	private static Dictionary<string, DateTimeOffset>? marks;

	private static Dictionary<string, DateTimeOffset> Marks
	{
		get
		{
			object obj = marks;
			if (obj == null)
			{
				obj = AgentDefaults.Get<Dictionary<string, DateTimeOffset>>("clicky.dock.clickyLastSeen.v1") ?? new Dictionary<string, DateTimeOffset>();
				marks = (Dictionary<string, DateTimeOffset>?)obj;
			}
			return (Dictionary<string, DateTimeOffset>)obj;
		}
	}

	public static IReadOnlyList<string> Unread(AgentService agents)
	{
		List<string> list = new List<string>();
		bool flag = false;
		foreach (ClickyInfo clicky in agents.Clickys)
		{
			DateTimeOffset dateTimeOffset = (from w in agents.ClickyApps.Works(clicky.Slug)
				select w.FinishedAt).DefaultIfEmpty(DateTimeOffset.MinValue).Max();
			DateTimeOffset? finishedAt = agents.LiveStatusForClicky(clicky.Slug).FinishedAt;
			if (finishedAt.HasValue)
			{
				DateTimeOffset valueOrDefault = finishedAt.GetValueOrDefault();
				if (valueOrDefault > dateTimeOffset)
				{
					dateTimeOffset = valueOrDefault;
				}
			}
			if (!Marks.TryGetValue(clicky.Slug, out var value))
			{
				Marks[clicky.Slug] = DateTimeOffset.Now;
				flag = true;
			}
			else if (dateTimeOffset > value)
			{
				list.Add(clicky.Slug);
			}
		}
		if (flag)
		{
			AgentDefaults.Set("clicky.dock.clickyLastSeen.v1", Marks);
		}
		return list;
	}

	public static void MarkRead(string slug)
	{
		Marks[slug] = DateTimeOffset.Now;
		AgentDefaults.Set("clicky.dock.clickyLastSeen.v1", Marks);
	}

	public static void MarkAllRead(IEnumerable<string> slugs)
	{
		foreach (string slug in slugs)
		{
			Marks[slug] = DateTimeOffset.Now;
		}
		AgentDefaults.Set("clicky.dock.clickyLastSeen.v1", Marks);
	}
}
