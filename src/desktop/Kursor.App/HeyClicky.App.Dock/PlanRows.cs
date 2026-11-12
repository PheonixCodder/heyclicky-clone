using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Agents.Jobs;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Dock;

internal sealed class PlanRows : StackPanel
{
	private readonly Color ink;

	private ClickyArcPlan plan;

	private ClickyLiveLine? line;

	private bool lineRisesIn;

	private HashSet<int> shownDone;

	private readonly List<PlanStepRow> rows = new List<PlanStepRow>();

	private int firstShown;

	private readonly List<DispatcherQueueTimer> cascade = new List<DispatcherQueueTimer>();

	private readonly bool chat;

	public PlanRows(ClickyArcPlan plan, ClickyLiveLine? line, Color ink, bool chat = false)
	{
		this.ink = ink;
		this.plan = plan;
		this.line = line;
		this.chat = chat;
		base.Spacing = (chat ? 9 : 8);
		shownDone = PlanCascade.Done(plan);
		Build();
	}

	private void Build()
	{
		base.Children.Clear();
		rows.Clear();
		ClickyArcPlan clickyArcPlan = PlanCascade.Displayed(plan, shownDone);
		int? currentIndex = clickyArcPlan.CurrentIndex;
		firstShown = PlanCascade.FirstShown(clickyArcPlan);
		for (int i = firstShown; i < Math.Min(clickyArcPlan.Steps.Count, firstShown + 4); i++)
		{
			ClickyArcPlan.Step step = clickyArcPlan.Steps[i];
			PlanStepRow item = new PlanStepRow(step.Text, step.IsDone, i == currentIndex, ink, chat);
			rows.Add(item);
			base.Children.Add(item);
			if (i == currentIndex)
			{
				ClickyLiveLine clickyLiveLine = line;
				if ((object)clickyLiveLine != null)
				{
					base.Children.Add(chat ? new LiveLineView(clickyLiveLine, ink, 11.5, lineRisesIn)
					{
						Margin = new Thickness(24.0, -4.0, 0.0, 0.0)
					} : new LiveLineView(clickyLiveLine, ink, 14.5, lineRisesIn)
					{
						Margin = new Thickness(31.0, -4.0, 0.0, 0.0)
					});
				}
			}
		}
		lineRisesIn = false;
	}

	public void Update(ClickyArcPlan? next, ClickyLiveLine? nextLine)
	{
		if ((object)next == null || next.IsEmpty)
		{
			return;
		}
		bool flag = !object.Equals(line, nextLine);
		lineRisesIn = flag && (object)nextLine != null && !ArcCardView.IsSameBatch(line, nextLine);
		line = nextLine;
		if (!plan.Steps.Select((ClickyArcPlan.Step s) => s.Text).SequenceEqual(next.Steps.Select((ClickyArcPlan.Step s) => s.Text)))
		{
			foreach (DispatcherQueueTimer item in cascade)
			{
				item.Stop();
			}
			cascade.Clear();
			plan = next;
			shownDone = PlanCascade.Done(next);
			Build();
			return;
		}
		plan = next;
		HashSet<int> hashSet = PlanCascade.Done(next);
		foreach (DispatcherQueueTimer item2 in cascade)
		{
			item2.Stop();
		}
		cascade.Clear();
		if (!shownDone.IsSubsetOf(hashSet))
		{
			shownDone.IntersectWith(hashSet);
		}
		List<int> list = (from i in hashSet.Except(shownDone)
			orderby i
			select i).ToList();
		if (list.Count == 0)
		{
			if (flag)
			{
				Build();
			}
			else
			{
				Refresh(animated: false);
			}
			return;
		}
		for (int j = 0; j < list.Count; j++)
		{
			int index = list[j];
			cascade.Add(DockTimer.After((double)j * 0.09 + 0.001, delegate
			{
				shownDone.Add(index);
				Refresh(animated: true);
			}));
		}
		if (flag)
		{
			Build();
		}
	}

	private void Refresh(bool animated)
	{
		ClickyArcPlan clickyArcPlan = PlanCascade.Displayed(plan, shownDone);
		if (PlanCascade.FirstShown(clickyArcPlan) != firstShown)
		{
			Build();
			return;
		}
		int? currentIndex = clickyArcPlan.CurrentIndex;
		for (int i = 0; i < rows.Count; i++)
		{
			int num = firstShown + i;
			if (num >= clickyArcPlan.Steps.Count)
			{
				break;
			}
			rows[i].Set(clickyArcPlan.Steps[num].IsDone, num == currentIndex, animated);
		}
		if ((object)line != null)
		{
			Build();
		}
	}
}
