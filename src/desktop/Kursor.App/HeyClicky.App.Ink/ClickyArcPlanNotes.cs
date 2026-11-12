using System.Collections.Generic;
using System.Linq;
using HeyClicky.Agents.Jobs;
using HeyClicky.Core.Ink;

namespace HeyClicky.App.Ink;

public static class ClickyArcPlanNotes
{
	public static ClickyArcPlan? Read(ClickyLiveSteps steps, string? notePath)
	{
		if (notePath == null)
		{
			return null;
		}
		ClickyArcPlan clickyArcPlan = steps.Plan(notePath);
		if ((object)clickyArcPlan != null && !clickyArcPlan.IsEmpty)
		{
			return clickyArcPlan;
		}
		string text = ClickyInkPlanReading.LatestText(notePath);
		if (text != null)
		{
			ClickyArcPlan clickyArcPlan2 = ReadNoteText(text);
			if ((object)clickyArcPlan2 != null)
			{
				steps.RememberLastPlan(clickyArcPlan2, notePath);
				return clickyArcPlan2;
			}
		}
		return null;
	}

	public static ClickyArcPlan? ReadNoteText(string noteText)
	{
		IReadOnlyList<(string, bool)> readOnlyList = ClickyInkPlanReading.ChecklistSteps(noteText);
		if (readOnlyList == null)
		{
			return null;
		}
		return new ClickyArcPlan(readOnlyList.Select<(string, bool), ClickyArcPlan.Step>(((string Text, bool IsDone) i) => new ClickyArcPlan.Step(i.Text, i.IsDone)).ToList());
	}

	public static ClickyArcPlan? FinalizingFor(ClickyLiveSteps steps, string? key)
	{
		if (key == null)
		{
			return null;
		}
		ClickyArcPlan clickyArcPlan = steps.LastPlan(key) ?? Read(steps, key);
		if ((object)clickyArcPlan != null && !clickyArcPlan.IsEmpty)
		{
			return clickyArcPlan.Finalizing;
		}
		return null;
	}

	public static string? NoteTitle(string? notePath)
	{
		return ClickyInkPlanReading.NoteTitle(notePath);
	}
}
