using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Agents.Jobs;

public static class ClickyJobsNoteSelection
{
	public const int UnopenedListed = 5;

	public const int MostRecentListed = 6;

	public static DateTimeOffset LastActive(ClickyJob job)
	{
		return job.FinishedAt ?? job.Runs?.LastOrDefault()?.StartedAt ?? job.CreatedAt;
	}

	public static IReadOnlyList<ClickyJob> Select(IReadOnlyList<ClickyJob> allJobs, ISet<Guid>? liveCardIds, Func<ClickyJob, ClickyJobRailPhase> phase, Func<ClickyJob, bool> repeats)
	{
		List<ClickyJob> source = allJobs.Where(delegate(ClickyJob job)
		{
			if (job.IsWaitingToStart)
			{
				Guid? suggestionCardId = job.SuggestionCardId;
				if (suggestionCardId.HasValue)
				{
					Guid valueOrDefault = suggestionCardId.GetValueOrDefault();
					if (!job.IsDismissed)
					{
						return liveCardIds?.Contains(valueOrDefault) ?? false;
					}
					return false;
				}
			}
			return true;
		}).ToList();
		List<ClickyJob> list = source.Where(delegate(ClickyJob job)
		{
			bool flag = job.IsWaitingToStart || job.IsHeldForChrome || repeats(job);
			if (!flag)
			{
				ClickyJobRailPhase clickyJobRailPhase = phase(job);
				bool flag2 = (uint)clickyJobRailPhase <= 2u;
				flag = flag2;
			}
			return flag;
		}).ToList();
		HashSet<string> attentionIds = list.Select((ClickyJob j) => j.Id).ToHashSet();
		List<ClickyJob> list2 = source.Where(delegate(ClickyJob job)
		{
			bool flag3 = !job.IsWaitingToStart && !job.IsHeldForChrome && !job.IsSeen && !job.IsDismissed && !attentionIds.Contains(job.Id);
			if (flag3)
			{
				ClickyJobRailPhase clickyJobRailPhase2 = phase(job);
				bool flag4 = (uint)(clickyJobRailPhase2 - 3) <= 2u;
				flag3 = flag4;
			}
			return flag3;
		}).OrderByDescending(LastActive).Take(5)
			.ToList();
		attentionIds.UnionWith(list2.Select((ClickyJob j) => j.Id));
		IEnumerable<ClickyJob> second = source.Where((ClickyJob job) => !attentionIds.Contains(job.Id) && !job.IsWaitingToStart).OrderByDescending(LastActive).Take(6);
		return list.Concat(list2).Concat(second).OrderBy(LastActive)
			.ToList();
	}
}
