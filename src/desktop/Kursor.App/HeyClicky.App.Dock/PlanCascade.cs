using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public static class PlanCascade
{
	public const double StepDelay = 0.09;

	public static HashSet<int> Done(ClickyArcPlan plan)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (int item in from i in Enumerable.Range(0, plan.Steps.Count)
			where plan.Steps[i].IsDone
			select i)
		{
			hashSet.Add(item);
		}
		return hashSet;
	}

	public static ClickyArcPlan Displayed(ClickyArcPlan plan, IReadOnlySet<int>? shown)
	{
		if (shown == null)
		{
			return plan;
		}
		bool flag = true;
		List<ClickyArcPlan.Step> list = new List<ClickyArcPlan.Step>();
		for (int i = 0; i < plan.Steps.Count; i++)
		{
			ClickyArcPlan.Step step = plan.Steps[i];
			bool flag2 = step.IsDone && shown.Contains(i);
			list.Add(new ClickyArcPlan.Step(step.Text, flag2, step.IsNow && flag));
			flag = flag && flag2;
		}
		return new ClickyArcPlan(list);
	}

	public static int FirstShown(ClickyArcPlan plan)
	{
		int num = plan.CurrentIndex ?? plan.Steps.Count;
		return Math.Max(0, Math.Min(num - 1, plan.Steps.Count - 4));
	}
}
