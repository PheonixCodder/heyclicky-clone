using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public static class JobCardText
{
	public static string BodyText(ClickyJob job, ClickyJobRailPhase phase, bool hasTask, string? latestCommentary, string? thinkingHeading, string? pendingComputerUse, string? taskFailureSummary)
	{
		object obj2;
		object obj;
		string text;
		switch (phase)
		{
		case ClickyJobRailPhase.Queued:
			return "Getting started…";
		case ClickyJobRailPhase.Working:
		{
			if (!hasTask)
			{
				return "Working on it";
			}
			object obj3;
			if (latestCommentary != null)
			{
				obj3 = ClickyWorkReplyParts.FirstSentence(latestCommentary, 160);
			}
			else
			{
				obj3 = null;
			}
			string text3 = (string)obj3;
			string text4 = thinkingHeading?.Trim();
			if (text3 == null || text3.Length <= 0)
			{
				if (text4 == null || text4.Length <= 0)
				{
					return "Working on it";
				}
				return text4;
			}
			return text3;
		}
		case ClickyJobRailPhase.NeedsYou:
		{
			string text2 = pendingComputerUse?.Trim();
			if (text2 != null && text2.Length > 0)
			{
				return "One step on your PC: " + text2;
			}
			return "Needs more agent messages to keep going.";
		}
		case ClickyJobRailPhase.Done:
		{
			string text5 = job.Summary?.Trim();
			obj2 = ((text5 != null && text5.Length > 0) ? text5 : null);
			string replyText = job.ReplyText;
			if (replyText != null)
			{
				string text6 = ClickyWorkReplyParts.FirstSentence(replyText, 420);
				if (text6 != null && text6.Length > 0)
				{
					obj = text6;
					goto IL_00f6;
				}
			}
			obj = null;
			goto IL_00f6;
		}
		case ClickyJobRailPhase.Failed:
		{
			string summary2 = job.Summary;
			if (summary2 == null || summary2.Length <= 0)
			{
				if (taskFailureSummary != null && taskFailureSummary.Length > 0)
				{
					return taskFailureSummary;
				}
				return "Something went wrong.";
			}
			return summary2;
		}
		default:
			{
				string summary = job.Summary;
				if (summary == null || summary.Length <= 0)
				{
					return "Stopped before it finished.";
				}
				return summary;
			}
			IL_00f6:
			text = (string)obj;
			if (obj2 == null)
			{
				obj2 = text ?? "Done.";
			}
			return (string)obj2;
		}
	}
}
