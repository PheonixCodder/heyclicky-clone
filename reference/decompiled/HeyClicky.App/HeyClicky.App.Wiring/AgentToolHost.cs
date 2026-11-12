using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Portraits;
using HeyClicky.App.Proactive;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Tools;

namespace HeyClicky.App.Wiring;

public sealed class AgentToolHost : IAgentToolHost
{
	private readonly AgentService agents;

	private readonly Func<string?, bool> openHome;

	private readonly Func<bool> closeHome;

	public IReadOnlyList<AgentClickyInfo> Clickys => agents.Clickys.Select(Info).ToList();

	public IReadOnlyList<AgentTaskInfo> Tasks => (from t in agents.Tasks
		select agents.GetTaskDetail(t.TaskId) into d
		where (object)d != null
		select TaskInfo(d)with
		{
			PendingComputerUseRequest = agents.PendingComputerUseRequest(d.Snapshot.TaskId),
			IsDemo = d.IsDemo
		}).ToList();

	public string ProjectsRoot => agents.ProjectsRoot;

	public static Action<AgentToolLaunch, string>? Launched { get; set; }

	public static Action<string, string>? Steered { get; set; }

	public Action<string>? ApproveComputerUseHook { get; set; }

	public Action<string>? DeclineComputerUseHook { get; set; }

	public AgentToolHost(AgentService agents, Func<string?, bool> openHome, Func<bool> closeHome)
	{
		this.agents = agents;
		this.openHome = openHome;
		this.closeHome = closeHome;
	}

	private static AgentClickyInfo Info(ClickyInfo c)
	{
		return new AgentClickyInfo(c.Slug, c.Name, c.RoleText, c.OneLiner, c.ThreadId, c.IsArchived);
	}

	public AgentClickyInfo? FindClicky(string slug)
	{
		ClickyInfo clickyInfo = agents.FindClicky(slug);
		if ((object)clickyInfo == null)
		{
			return null;
		}
		return Info(clickyInfo);
	}

	private static AgentTaskInfo TaskInfo(AgentTaskDetail d)
	{
		return new AgentTaskInfo(d.Snapshot.TaskId, d.Snapshot.ThreadId, d.Snapshot.Title, d.Snapshot.Status, d.ClickySlug, d.ClickyName, d.ProgressSummary, d.Summary, d.Artifacts, d.RequiresExtraUsageApproval, d.UserPrompts, (from a in d.Activity
			where a.Kind == AgentActivityKind.Commentary
			select a.Text).ToList(), d.StreamingResponse, d.WorkspacePath, d.Snapshot.StartedAt);
	}

	public AgentJobInfo? FindJob(string threadId)
	{
		ClickyJob clickyJob = agents.Jobs.JobForThread(threadId);
		if ((object)clickyJob == null)
		{
			return null;
		}
		string state = agents.RailPhase(clickyJob) switch
		{
			ClickyJobRailPhase.Queued => "starting", 
			ClickyJobRailPhase.Working => "working", 
			ClickyJobRailPhase.NeedsYou => "waiting on the user", 
			ClickyJobRailPhase.Done => "done", 
			ClickyJobRailPhase.Failed => "failed", 
			_ => "stopped", 
		};
		string text = agents.LiveTaskId(clickyJob.Id);
		AgentTaskDetail agentTaskDetail = ((text == null) ? null : agents.GetTaskDetail(text));
		List<AgentJobRunInfo> runs = (clickyJob.Runs ?? Array.Empty<ClickyJobRun>()).Select((ClickyJobRun r) => new AgentJobRunInfo(r.Ask, r.StartedAt, Outcome(r.Outcome), r.Summary)).ToList();
		string text2 = ((text == null) ? null : agents.FullReplyText(text));
		string latestReply = ((text2 != null && text2.Length > 0) ? text2 : (clickyJob.ReplyText ?? ""));
		return new AgentJobInfo(clickyJob.ThreadId, clickyJob.Title, state, clickyJob.FolderPath, runs, latestReply, clickyJob.Summary, clickyJob.ArtifactEntries, ((object)agentTaskDetail == null) ? null : TaskInfo(agentTaskDetail));
		static string Outcome(ClickyJobOutcome outcome)
		{
			return outcome.ToString().ToLowerInvariant();
		}
	}

	public string? ClickySlugForThread(string threadId)
	{
		object obj = agents.Clickys.FirstOrDefault((ClickyInfo c) => c.ThreadId == threadId)?.Slug;
		if (obj == null)
		{
			AgentTaskInfo? agentTaskInfo = Tasks.FirstOrDefault((AgentTaskInfo t) => t.ThreadId == threadId && t.ClickySlug != null);
			if ((object)agentTaskInfo == null)
			{
				return null;
			}
			obj = agentTaskInfo.ClickySlug;
		}
		return (string?)obj;
	}

	public string? WorkspacePath(string clickySlug)
	{
		return agents.WorkspacePath(clickySlug);
	}

	public async Task<string> LaunchAsync(AgentToolLaunch launch, CancellationToken ct)
	{
		bool withoutChrome = launch.ChromeChoice == "without";
		string text = await agents.LaunchAsync(new AgentLaunchRequest
		{
			Prompt = launch.Prompt,
			ClickySlug = launch.ClickySlug,
			NewClickyName = launch.NewClickyName,
			NewClickyRole = launch.NewClickyRole,
			NewClickyOneLiner = launch.NewClickyOneLiner,
			LaunchContext = (withoutChrome ? (launch.LaunchContext + " They chose not to connect Chrome for this job: don't ask about Chrome again. For a step on a website they're signed into, call mac_step and HeyClicky does it on their screen while they watch.") : launch.LaunchContext),
			LaunchSource = launch.LaunchSource,
			ImagesJpeg = launch.ImagesJpeg,
			AttachedFilePaths = launch.AttachedFilePaths,
			UsageContext = launch.UsageContext,
			IsGeneratedTaskBrief = launch.IsGeneratedTaskBrief,
			BuildsOnJobIds = launch.BuildsOnJobIds
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (withoutChrome)
		{
			string text2 = agents.GetTaskDetail(text)?.JobId;
			if (text2 != null)
			{
				agents.Jobs.SetSkipsChrome(text2);
			}
		}
		try
		{
			Launched?.Invoke(launch, text);
		}
		catch (Exception ex)
		{
			Log.Warn("Tools", "launch note: " + ex.Message);
		}
		return text;
	}

	public Task SteerAsync(string taskId, string text, CancellationToken ct)
	{
		return SteerAsync(taskId, text, null, ct);
	}

	public async Task SteerAsync(string taskId, string text, IReadOnlyList<byte[]>? imagesJpeg, CancellationToken ct)
	{
		await agents.SteerAsync(taskId, text, imagesJpeg, ct, carriesCompanionConversation: true).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			Steered?.Invoke(taskId, text);
		}
		catch (Exception ex)
		{
			Log.Warn("Tools", "steer note: " + ex.Message);
		}
	}

	public string OpenThread(string threadId)
	{
		return agents.OpenThread(threadId);
	}

	public Task StopAsync(string taskId)
	{
		return agents.StopAsync(taskId);
	}

	public Task ApproveExtraUsageAsync(string taskId, bool asManyAsNeeded, CancellationToken ct)
	{
		return agents.ApproveExtraUsageAsync(taskId, asManyAsNeeded, ct);
	}

	public AgentClickyInfo CreateClicky(string name, string roleText, string oneLiner)
	{
		ClickyInfo clickyInfo = agents.CreateClicky(name, roleText, oneLiner);
		ClickyRoster.Shared.TrackCreated(clickyInfo.Slug, "agent");
		return Info(clickyInfo);
	}

	public async Task<string?> HandleSuggestionActionAsync(string task)
	{
		SuggestionVoiceAction.Request action = SuggestionVoiceAction.Parse(task);
		if (action.Kind == SuggestionVoiceAction.Kind.NotSuggestionAction)
		{
			return null;
		}
		return (await ProactiveHooks.RunOnUiAsync((ProactiveCoordinator c) => c.HandleVoiceAction(action)).ConfigureAwait(continueOnCapturedContext: false)) ?? "{\"status\":\"error\",\"message\":\"Suggestions aren't available right now.\"}";
	}

	public async Task<IReadOnlyList<(string Role, string Text)>> RecentMessagesAsync(string threadId, int limit, CancellationToken ct)
	{
		IReadOnlyList<AgentTranscriptTurn> obj = await agents.ReadThreadAsync(threadId, ct).ConfigureAwait(continueOnCapturedContext: false);
		List<(string, string)> list = new List<(string, string)>();
		foreach (AgentTranscriptTurn item in obj)
		{
			foreach (string userMessage in item.UserMessages)
			{
				if (!string.IsNullOrWhiteSpace(userMessage))
				{
					list.Add(("user", userMessage));
				}
			}
			foreach (string assistantMessage in item.AssistantMessages)
			{
				if (!string.IsNullOrWhiteSpace(assistantMessage))
				{
					list.Add(("clicky", assistantMessage));
				}
			}
		}
		return list.TakeLast(limit).ToList();
	}

	public async Task<IReadOnlyList<AgentThreadInfo>> HistoryAsync(int limit, CancellationToken ct)
	{
		return (await agents.ListThreadsAsync(limit, null, null, ct).ConfigureAwait(continueOnCapturedContext: false)).Item1.Select((AgentThreadSummary t) => new AgentThreadInfo(t.ThreadId, t.Name, t.Preview, t.UpdatedAt, t.Status, t.ClickySlug)).ToList();
	}

	public bool OpenHome(string? threadId)
	{
		string arg = ((threadId == null) ? null : agents.Tasks.FirstOrDefault((AgentTaskSnapshot t) => t.ThreadId == threadId)?.TaskId);
		return openHome(arg);
	}

	public bool CloseHome()
	{
		return closeHome();
	}

	public bool OpenPath(string pathOrUrl)
	{
		try
		{
			ShellOpen.Start(pathOrUrl);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("Tools", "open " + pathOrUrl + ": " + ex.Message);
			return false;
		}
	}

	public bool RevealPath(string path)
	{
		try
		{
			Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"")
			{
				UseShellExecute = false
			});
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("Tools", "reveal " + path + ": " + ex.Message);
			return false;
		}
	}

	public bool SkipOnboardingInterview()
	{
		return FlowHooks.SkipInterview?.Invoke() ?? false;
	}

	public void ApproveComputerUse(string taskId)
	{
		ApproveComputerUseHook?.Invoke(taskId);
	}

	public void DeclineComputerUse(string taskId)
	{
		DeclineComputerUseHook?.Invoke(taskId);
	}

	public async Task<string> FindJobsAsync(string query, int limit)
	{
		return ToolResults.Json(await agents.FindJobsAsync(query, limit, forHelper: false).ConfigureAwait(continueOnCapturedContext: false));
	}

	public (bool IsJob, string? ThreadId) ThreadIdForJobId(string jobId)
	{
		return agents.ThreadIdForOneBrainJobId(jobId);
	}

	public string JobIdForThread(string threadId)
	{
		return agents.OneBrainJobId(threadId);
	}

	public string? BuildsOnContext(IReadOnlyList<string> jobIds, string? receivingThreadId)
	{
		return agents.Jobs.BuildsOnContext(jobIds, (receivingThreadId == null) ? null : agents.Jobs.JobForThread(receivingThreadId)?.Id, agents.EarlierWorkElsewhere);
	}

	public void RetitleJob(string threadId, string title)
	{
		ClickyJob clickyJob = agents.Jobs.JobForThread(threadId);
		if ((object)clickyJob != null)
		{
			agents.Jobs.Retitle(clickyJob.Id, title);
		}
	}
}
