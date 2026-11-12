using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Browser;

namespace HeyClicky.App.Dock;

internal sealed class ChromeSetupJobs(AgentService agents, Action<string> presentJobNeedsYou) : IChromeSetupJobs
{
	public string? JobIdForThread(string threadId)
	{
		return agents.Jobs.JobForThread(threadId)?.Id;
	}

	public bool IsHeldForChrome(string jobId)
	{
		return agents.Jobs.Job(jobId)?.IsHeldForChrome ?? false;
	}

	public IReadOnlyList<string> HeldJobIdsNewestFirst()
	{
		return (from j in agents.Jobs.Jobs
			where j.IsHeldForChrome && !j.IsDismissed
			orderby j.CreatedAt descending
			select j.Id).ToList();
	}

	public void SetWaitsForChrome(string jobId, bool waits)
	{
		agents.Jobs.SetWaitsForChrome(jobId, waits);
	}

	public void SetSkipsChrome(string jobId)
	{
		agents.Jobs.SetSkipsChrome(jobId);
	}

	public bool SkipsChrome(string jobId)
	{
		return agents.Jobs.Job(jobId)?.SkipsChrome == true;
	}

	public void PresentNeedsYou(string jobId)
	{
		ClickyJob clickyJob = agents.Jobs.Job(jobId);
		if ((object)clickyJob != null)
		{
			presentJobNeedsYou(clickyJob.RailKey);
		}
	}

	public void StartFromSavedPrompt(string jobId, bool withoutChrome)
	{
		ClickyJob job = agents.Jobs.Job(jobId);
		if ((object)job == null)
		{
			return;
		}
		string context = "HeyClicky started this as background work while their Chrome wasn't connected." + (withoutChrome ? " They chose not to connect Chrome for this job: don't ask about Chrome again. For a step on a website they're signed into, call mac_step and HeyClicky does it on their screen while they watch." : "");
		Task.Run(async delegate
		{
			try
			{
				await agents.LaunchAsync(new AgentLaunchRequest
				{
					Prompt = job.Prompt,
					JobId = job.Id,
					LaunchContext = context,
					LaunchSource = "voice"
				}).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Warn("dock", "held job " + job.Id + " didn't start: " + ex.Message);
				agents.Jobs.Dismiss(job.Id);
			}
		});
	}

	public string? CreatePreviewJob(string title, string prompt)
	{
		return agents.Jobs.CreateJob(title, prompt, agents.ProjectsRoot).Id;
	}

	public void Dismiss(string jobId)
	{
		agents.Jobs.Dismiss(jobId);
	}
}
