using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.App.Browser;
using HeyClicky.App.Drive;
using HeyClicky.Core.Browser;

namespace HeyClicky.App.Dock;

internal sealed class AgentDriveJobs(AgentService agents, Action<string, string> presentNeedsYou, Action<string, string> noteEventForBrain, Func<string, Task> announce) : IAgentDriveJobs
{
	public IEnumerable<IAgentDriveJob> Jobs => (from d in agents.Tasks.Select((AgentTaskSnapshot t) => agents.GetTaskDetail(t.TaskId)).OfType<AgentTaskDetail>()
		where !d.IsDemo
		select d).Select((Func<AgentTaskDetail, IAgentDriveJob>)((AgentTaskDetail d) => new DriveJob(agents, d.Snapshot.TaskId))).ToList();

	public bool ChromeIsConnected => ClickyBrowserBridge.Shared.IsConnected;

	public IAgentDriveJob? Find(string taskId)
	{
		if ((object)agents.GetTaskDetail(taskId) != null)
		{
			return new DriveJob(agents, taskId);
		}
		return null;
	}

	public bool IsThreadActive(string threadId)
	{
		return agents.IsThreadActive(threadId);
	}

	public void SubmitFollowUp(string threadId, string text, string? preauthorizedLeaseId, string launchContext)
	{
		Run(delegate
		{
			AgentService agentService = agents;
			string threadId2 = threadId;
			string text2 = text;
			string? preauthorizedLeaseId2 = preauthorizedLeaseId;
			string launchContext2 = launchContext;
			return agentService.SubmitThreadFollowUpAsync(threadId2, text2, preauthorizedLeaseId2, default(CancellationToken), launchContext2);
		}, "follow-up");
	}

	public Task<string?> ContinueLeaseAfterConsentAsync(string leaseId)
	{
		return agents.ContinueLeaseAfterComputerUseConsentAsync(leaseId);
	}

	public void CompleteLease(string leaseId, string threadId)
	{
		agents.CompleteLease(leaseId, threadId);
	}

	public void PresentNeedsYou(IAgentDriveJob job)
	{
		presentNeedsYou(job.TaskId, "needs_you");
	}

	public void NoteEventForBrain(IAgentDriveJob job, string eventText, string summary)
	{
		string threadId = job.ThreadId;
		if (threadId != null)
		{
			noteEventForBrain(threadId, eventText);
		}
	}

	public void AnnounceVerbatim(string line)
	{
		announce(line);
	}

	public bool SkipsChrome(string threadId)
	{
		return ChromeSetupFlow.Shared.SkipsChrome(threadId);
	}

	private static async Task Run(Func<Task> work, string what)
	{
		try
		{
			await work().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "drive " + what + ": " + ex.Message);
		}
	}
}
