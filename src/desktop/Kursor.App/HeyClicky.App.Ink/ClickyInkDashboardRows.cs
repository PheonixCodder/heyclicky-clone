using System.Collections.Generic;
using HeyClicky.Core.Ink;

namespace HeyClicky.App.Ink;

public static class ClickyInkDashboardRows
{
	private static readonly Dictionary<string, ClickyInkDashboardSplit.Rows> Cache = new Dictionary<string, ClickyInkDashboardSplit.Rows>();

	public static ClickyInkDashboardSplit.Rows For(ClickyInkLiveNote note, ClickyHomeSpreadPaper paper)
	{
		string key = ClickyInkDashboardSplit.Key(note);
		if (Cache.TryGetValue(key, out ClickyInkDashboardSplit.Rows value))
		{
			return value;
		}
		double clock = note.FinishedClock + 30.0;
		ClickyInkStyle style = new ClickyInkStyle(paper.Ink, paper.Quiet, paper.Deep, note.LatestEdit(clock));
		ClickyInkDashboardSplit.Rows rows = ClickyInkDashboardSplit.Plan(note, (ClickyInkGroup group) => ClickyInkFormKit.MeasureHeight(ClickyInkGroupView.Make(group, clock, style, paper, new ClickyInkActions()), 378.0) + 12.0);
		Cache[key] = rows;
		if (Cache.Count > 100)
		{
			Cache.Clear();
		}
		return rows;
	}
}
