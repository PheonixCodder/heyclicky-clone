using System.Linq;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Jobs;
using HeyClicky.Core.Notch;

namespace HeyClicky.App;

internal sealed class WorkHandOffDirectory(AgentService agents) : IChatHandOffDirectory
{
	public (string Id, string Title)? JobForThread(string threadId)
	{
		ClickyJob clickyJob = agents.Jobs.JobForThread(threadId);
		if ((object)clickyJob == null)
		{
			return null;
		}
		return (clickyJob.Id, clickyJob.Title);
	}

	public string? JobTitle(string jobId)
	{
		return agents.Jobs.Job(jobId)?.Title;
	}

	public string? ClickySlugForThread(string threadId)
	{
		return agents.Clickys.FirstOrDefault((ClickyInfo c) => c.ThreadId == threadId)?.Slug;
	}

	public string? ClickyName(string slug)
	{
		return agents.FindClicky(slug)?.Name;
	}
}
