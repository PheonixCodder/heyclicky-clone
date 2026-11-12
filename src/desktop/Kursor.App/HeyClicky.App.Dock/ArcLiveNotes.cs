using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.Core.Ink;

namespace HeyClicky.App.Dock;

public static class ArcLiveNotes
{
	public static bool IsEnabled
	{
		get
		{
			if (ClickyAppsPreferences.IsEnabled)
			{
				return ClickyLiveNotePreferences.IsEnabled;
			}
			return false;
		}
	}

	public static string NotePath(string folder)
	{
		return ClickyLiveNotePreferences.NotePath(folder);
	}

	public static string? ClickyNotePath(AgentService agents, string slug)
	{
		string text = agents.FindClicky(slug)?.ThreadId;
		if (text != null)
		{
			ClickyJob clickyJob = agents.Jobs.JobForThread(text);
			if ((object)clickyJob != null)
			{
				return NotePath(clickyJob.FolderPath);
			}
		}
		string text2 = agents.WorkspacePath(slug);
		if (text2 == null || text2.Length <= 0)
		{
			return null;
		}
		return NotePath(text2);
	}
}
