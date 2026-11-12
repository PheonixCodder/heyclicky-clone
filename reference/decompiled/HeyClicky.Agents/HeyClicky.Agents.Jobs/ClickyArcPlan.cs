using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyArcPlan(IReadOnlyList<Step> Steps)
{
	public sealed record Step(string Text, bool IsDone, bool IsNow = false);

	public int DoneCount => Steps.Count((Step s) => s.IsDone);

	public int? CurrentIndex
	{
		get
		{
			for (int i = 0; i < Steps.Count; i++)
			{
				if (Steps[i].IsNow && !Steps[i].IsDone)
				{
					return i;
				}
			}
			for (int j = 0; j < Steps.Count; j++)
			{
				if (!Steps[j].IsDone)
				{
					return j;
				}
			}
			return null;
		}
	}

	public bool IsEmpty => Steps.Count == 0;

	public ClickyArcPlan Finalizing => new ClickyArcPlan(Steps.Select((Step s) => new Step(s.Text, IsDone: true)).Append(new Step("Finalizing", IsDone: false, IsNow: true)).ToList());

	public const string FinalizingStepText = "Finalizing";

	public static ClickyArcPlan FromPlanTool(IEnumerable<AgentPlanStep> steps)
	{
		return new ClickyArcPlan(steps.Select((AgentPlanStep s) => new Step(s.Text, s.IsDone, s.IsNow)).ToList());
	}

	public bool Equals(ClickyArcPlan? other)
	{
		if ((object)other != null)
		{
			return Steps.SequenceEqual(other.Steps);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Steps.Aggregate(0, (int h, Step s) => HashCode.Combine(h, s));
	}

	[CompilerGenerated]
	private ClickyArcPlan(ClickyArcPlan original)
	{
		Steps = original.Steps;
	}
}
