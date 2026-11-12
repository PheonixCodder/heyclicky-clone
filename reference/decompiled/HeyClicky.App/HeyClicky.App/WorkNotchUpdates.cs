using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Browser;
using HeyClicky.App.Dock;
using HeyClicky.App.Home;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Notch.Surfaces;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Tools;

namespace HeyClicky.App;

internal sealed class WorkNotchUpdates : INotchUpdateContentSource
{
	private readonly AgentService agents;

	private readonly Func<OneBrainJobReports?> reports;

	private readonly AppHost host;

	public event Action? Changed;

	public WorkNotchUpdates(AgentService agents, Func<OneBrainJobReports?> reports, AppHost host)
	{
		this.agents = agents;
		this.reports = reports;
		this.host = host;
		agents.TasksChanged += delegate
		{
			this.Changed?.Invoke();
		};
		agents.Jobs.Changed += delegate
		{
			this.Changed?.Invoke();
		};
		agents.ResultCardShown += delegate
		{
			this.Changed?.Invoke();
		};
		OneBrainJobReports oneBrainJobReports = reports();
		if (oneBrainJobReports != null)
		{
			oneBrainJobReports.PresentingChanged += delegate
			{
				this.Changed?.Invoke();
			};
		}
	}

	public NotchUpdateContent? Content(string railKey)
	{
		string text = ClickyJob.JobIdFromRailKey(railKey);
		if (text == null)
		{
			return ClickyContent(railKey);
		}
		return JobContent(text, railKey);
	}

	public (string Subject, JsonObject Properties)? ShownAnalytics(string railKey, NotchUpdateKind kind)
	{
		bool flag = kind == NotchUpdateKind.Done;
		ArcCardContent arcCardContent = host.Dock?.ContentForKey(railKey);
		if ((object)arcCardContent == null)
		{
			NotchUpdateContent notchUpdateContent = Content(railKey);
			if ((object)notchUpdateContent == null)
			{
				return null;
			}
			string text = (flag ? "done" : ((kind == NotchUpdateKind.NeedsYou) ? "needs_you" : (notchUpdateContent.IsStopped ? "stopped" : "failed")));
			return ((notchUpdateContent.JobId == null) ? "clicky" : "job", new JsonObject((JsonNodeOptions?)null) { ["state"] = text });
		}
		return ((flag && IsRoutineReturn(arcCardContent)) ? "routine" : ((arcCardContent.JobId == null) ? "clicky" : "job"), NotchUpdateCardAnalytics.ShownProperties(arcCardContent, flag));
	}

	private bool IsRoutineReturn(ArcCardContent content)
	{
		string clickySlug = content.ClickySlug;
		if (clickySlug == null)
		{
			return false;
		}
		string taskId = content.TaskId;
		bool num;
		if (taskId != null)
		{
			AgentTaskDetail taskDetail = agents.GetTaskDetail(taskId);
			if ((object)taskDetail != null)
			{
				num = taskDetail.IsRoutineRun;
				goto IL_0065;
			}
		}
		ChatHandOffs handOffs = NotchChat.HandOffs;
		if (handOffs != null)
		{
			string text = handOffs.LatestReturnEntryId(null, clickySlug);
			if (text != null)
			{
				ChatHandOff? chatHandOff = handOffs.Entry(text);
				if ((object)chatHandOff != null)
				{
					num = chatHandOff.IsRoutineRun == true;
					goto IL_0065;
				}
			}
		}
		goto IL_0081;
		IL_0065:
		if (num)
		{
			return agents.Routines.ForClicky(clickySlug).Count > 0;
		}
		goto IL_0081;
		IL_0081:
		return false;
	}

	private NotchUpdateContent? JobContent(string jobId, string railKey)
	{
		ClickyJob clickyJob = agents.Jobs.Job(jobId);
		if ((object)clickyJob == null)
		{
			return null;
		}
		ClickyJobRailPhase clickyJobRailPhase = agents.RailPhase(clickyJob);
		string text = agents.LiveTaskId(clickyJob.Id);
		AgentTaskDetail agentTaskDetail = ((text != null) ? agents.GetTaskDetail(text) : null);
		string text2 = agentTaskDetail?.Snapshot.ThreadId ?? clickyJob.ThreadId;
		bool flag = clickyJobRailPhase == ClickyJobRailPhase.Done && (reports()?.IsPresenting(text2) ?? false);
		bool flag2 = clickyJobRailPhase == ClickyJobRailPhase.Done && !flag;
		IReadOnlyList<string> readOnlyList2;
		if (!flag2)
		{
			IReadOnlyList<string> readOnlyList = Array.Empty<string>();
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = clickyJob.FilesFromLatestReply;
		}
		IReadOnlyList<string> files = readOnlyList2;
		ClickyAnswer clickyAnswer = ((!flag2) ? null : (((text2 == null) ? null : agents.ResultCard(text2)) ?? agentTaskDetail?.Answer));
		string text3 = ((!flag2) ? null : (clickyAnswer?.Line ?? agentTaskDetail?.Summary ?? clickyJob.Summary));
		NotchUpdateRequest request = TaskRequest(agentTaskDetail, clickyJob.Title, clickyJob.Id) ?? JobRequest(clickyJob, clickyJobRailPhase, agentTaskDetail);
		return new NotchUpdateContent
		{
			RailKey = railKey,
			JobId = clickyJob.Id,
			ThreadId = text2,
			Title = clickyJob.Title,
			Summary = (string.IsNullOrWhiteSpace(text3) ? null : text3),
			Request = request,
			ReadyOutput = (flag2 ? ReadyOutput(files, text2, null) : null),
			Files = files,
			IsStopped = (clickyJobRailPhase == ClickyJobRailPhase.Stopped)
		};
	}

	private NotchUpdateContent? ClickyContent(string slug)
	{
		AgentTaskDetail agentTaskDetail = (from t in agents.Tasks
			select agents.GetTaskDetail(t.TaskId) into d
			where d?.ClickySlug == slug
			orderby d.Snapshot.StartedAt descending
			select d).FirstOrDefault();
		if ((object)agentTaskDetail == null)
		{
			return null;
		}
		string text = (agentTaskDetail.DoneTitle ?? agentTaskDetail.Snapshot.Title).Trim();
		int num;
		if (agentTaskDetail.Snapshot.Status == AgentTaskStatus.Finished)
		{
			OneBrainJobReports oneBrainJobReports = reports();
			num = ((oneBrainJobReports == null || !oneBrainJobReports.IsPresenting(agentTaskDetail.Snapshot.ThreadId)) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		IReadOnlyList<string> readOnlyList2;
		if (!flag)
		{
			IReadOnlyList<string> readOnlyList = Array.Empty<string>();
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = agentTaskDetail.Artifacts;
		}
		IReadOnlyList<string> files = readOnlyList2;
		object obj;
		if (!flag)
		{
			obj = null;
		}
		else
		{
			string threadId = agentTaskDetail.Snapshot.ThreadId;
			obj = ((threadId != null) ? agents.ResultCard(threadId) : null) ?? agentTaskDetail.Answer;
		}
		ClickyAnswer clickyAnswer = (ClickyAnswer)obj;
		return new NotchUpdateContent
		{
			RailKey = slug,
			ClickySlug = slug,
			ThreadId = agentTaskDetail.Snapshot.ThreadId,
			Title = text,
			Summary = ((!flag) ? null : (clickyAnswer?.Line ?? agentTaskDetail.Summary)),
			Request = TaskRequest(agentTaskDetail, text, null),
			ReadyOutput = (flag ? ReadyOutput(files, agentTaskDetail.Snapshot.ThreadId, slug) : null),
			Files = files,
			IsStopped = (agentTaskDetail.Snapshot.Status == AgentTaskStatus.Stopped)
		};
	}

	private NotchUpdateRequest? TaskRequest(AgentTaskDetail? task, string taskTitle, string? jobId)
	{
		if ((object)task == null)
		{
			return null;
		}
		string text = taskTitle.Trim();
		string threadId = task.Snapshot.ThreadId;
		string taskId = task.Snapshot.TaskId;
		string railKey = ((jobId != null) ? ("job:" + jobId) : (task.ClickySlug ?? ""));
		string text2 = agents.PendingComputerUseRequest(taskId);
		if (text2 != null)
		{
			CardFooterRequest cardFooterRequest = new CardFooterRequest(CardFooterRequest.Kinds.ComputerUse, agents.PendingComputerUseSummary(taskId) ?? ClickyNotes.SpeakableSentence(text2, 110));
			return new NotchUpdateRequest(cardFooterRequest.Title, cardFooterRequest.Detail, new global::_003C_003Ez__ReadOnlyArray<NotchUpdateButton>(new NotchUpdateButton[2]
			{
				new NotchUpdateButton("Let HeyClicky drive", IsPrimary: true, Answered("drive_approved", railKey, taskId, delegate
				{
					host.Dock?.LetClickyDrive(taskId);
				})),
				new NotchUpdateButton("Not now", IsPrimary: false, Answered("drive_declined", railKey, taskId, delegate
				{
					ArcDock dock = host.Dock;
					if (dock != null)
					{
						dock.DeclineComputerUse(taskId);
					}
					else
					{
						agents.SetPendingComputerUseRequest(taskId, null);
					}
				}))
			}));
		}
		if (task.RequiresExtraUsageApproval)
		{
			return new NotchUpdateRequest("Needs more agent messages", (text.Length > 0) ? (text + " is a big one. Let it keep going?") : "This one's big. Let it keep going?", new global::_003C_003Ez__ReadOnlyArray<NotchUpdateButton>(new NotchUpdateButton[2]
			{
				new NotchUpdateButton("Always approve", IsPrimary: true, Answered("extra_usage_always_approve", railKey, taskId, delegate
				{
					ApproveExtraUsageAsync(agents, task.Snapshot.TaskId, asManyAsNeeded: true);
				})),
				new NotchUpdateButton("Approve once", IsPrimary: false, Answered("extra_usage_approve_once", railKey, taskId, delegate
				{
					ApproveExtraUsageAsync(agents, task.Snapshot.TaskId, asManyAsNeeded: false);
				}))
			}));
		}
		if (threadId != null && agents.NeedsChromeConnection(threadId))
		{
			return ConnectChrome(text, jobId, threadId, railKey, taskId);
		}
		if (task.Snapshot.Status == AgentTaskStatus.Finished)
		{
			ClickyJobUserStep pendingUserStep = task.PendingUserStep;
			if ((object)pendingUserStep != null)
			{
				return UserStep(pendingUserStep.Step, jobId, taskId);
			}
		}
		if (task.Snapshot.Status == AgentTaskStatus.Failed)
		{
			return new NotchUpdateRequest((text.Length > 0) ? ("Didn't finish " + text) : "Didn't finish", FirstLine(task.FailureMessage) ?? "Something went wrong partway through.", new global::_003C_003Ez__ReadOnlySingleElementList<NotchUpdateButton>(new NotchUpdateButton("Retry", IsPrimary: true, Answered("retry", railKey, taskId, delegate
			{
				Retry(task, jobId);
			}))));
		}
		if (task.Snapshot.Status == AgentTaskStatus.Stopped)
		{
			return new NotchUpdateRequest("Interrupted", StoppedDetail(task.FailureMessage), new global::_003C_003Ez__ReadOnlySingleElementList<NotchUpdateButton>(new NotchUpdateButton("Retry", IsPrimary: true, Answered("retry", railKey, taskId, delegate
			{
				Retry(task, jobId);
			}))));
		}
		if (task.Snapshot.Status == AgentTaskStatus.WaitingForUser && jobId == null)
		{
			return new NotchUpdateRequest("Your turn", task.Summary ?? task.ProgressSummary, new global::_003C_003Ez__ReadOnlySingleElementList<NotchUpdateButton>(new NotchUpdateButton("Show in chat", IsPrimary: true, delegate
			{
				host.ShowInChat(null, task.ClickySlug);
			})));
		}
		return null;
	}

	private NotchUpdateRequest? JobRequest(ClickyJob job, ClickyJobRailPhase phase, AgentTaskDetail? task)
	{
		string taskId = task?.Snapshot.TaskId;
		if (job.IsHeldForChrome)
		{
			return ConnectChrome(job.Title.Trim(), job.Id, job.ThreadId, "job:" + job.Id, taskId);
		}
		ClickyJobUserStep waitingStep = job.WaitingStep;
		bool flag = (object)waitingStep != null;
		if (flag)
		{
			bool flag2 = (uint)phase <= 1u;
			flag = !flag2;
		}
		if (flag)
		{
			return UserStep(waitingStep.Step, job.Id, taskId);
		}
		if ((object)task != null || job.ThreadId == null)
		{
			return null;
		}
		string railKey = "job:" + job.Id;
		return phase switch
		{
			ClickyJobRailPhase.Failed => new NotchUpdateRequest((job.Title.Trim().Length > 0) ? ("Didn't finish " + job.Title.Trim()) : "Didn't finish", FirstLine(job.Summary) ?? "Something went wrong partway through.", new global::_003C_003Ez__ReadOnlySingleElementList<NotchUpdateButton>(new NotchUpdateButton("Retry", IsPrimary: true, Answered("retry", railKey, null, delegate
			{
				RetryJob(job);
			})))), 
			ClickyJobRailPhase.Stopped => new NotchUpdateRequest("Interrupted", StoppedDetail(null), new global::_003C_003Ez__ReadOnlySingleElementList<NotchUpdateButton>(new NotchUpdateButton("Retry", IsPrimary: true, Answered("retry", railKey, null, delegate
			{
				RetryJob(job);
			})))), 
			_ => null, 
		};
	}

	private Action Answered(string answer, string railKey, string? taskId, Action action)
	{
		return delegate
		{
			ArcDock.TrackRequestAnswered(agents, answer, "notch_card", railKey, taskId);
			action();
		};
	}

	private NotchUpdateRequest ConnectChrome(string title, string? jobId, string? threadId, string railKey, string? taskId)
	{
		ChromeSetupFlow flow = ChromeSetupFlow.Shared;
		ChromeSetupFlow.Target target = new ChromeSetupFlow.Target(jobId, threadId);
		List<NotchUpdateButton> list = new List<NotchUpdateButton>();
		string browserName = flow.BrowserName;
		if (flow.CanDoItForThem)
		{
			list.Add(new NotchUpdateButton("Get new extension", IsPrimary: true, Answered("chrome_do_it_for_me", railKey, taskId, delegate
			{
				flow.Choose(ChromeSetupFlow.Choice.DoItForMe, target);
			})));
		}
		list.Add(new NotchUpdateButton(flow.CanDoItForThem ? "I'll do it" : (flow.AddsAgain ? "Show me how" : ("Add to " + browserName)), !flow.CanDoItForThem, Answered("chrome_show_me_how", railKey, taskId, delegate
		{
			flow.Choose(ChromeSetupFlow.Choice.ShowMeHow, target);
		})));
		list.Add(new NotchUpdateButton("Not now", IsPrimary: false, Answered("chrome_not_now", railKey, taskId, delegate
		{
			flow.Choose(ChromeSetupFlow.Choice.NotNow, target);
		})));
		string detail = ((!flow.AddsAgain) ? ((title.Length > 0) ? $"To finish “{title}”, HeyClicky's extension goes in your {browserName} once, a couple of clicks." : CardFooterRequest.ConnectChromeDetail(browserName, addsAgain: false)) : ((title.Length > 0) ? ("To finish “" + title + "”, swap in the new extension. It updates itself, so this is the last time you'll do this.") : CardFooterRequest.ConnectChromeDetail(browserName, addsAgain: true)));
		return new NotchUpdateRequest(CardFooterRequest.ConnectChromeTitle(browserName, flow.AddsAgain), detail, list, IsChromeAsk: true);
	}

	private NotchUpdateRequest UserStep(string step, string? jobId, string? taskId)
	{
		IReadOnlyList<NotchUpdateButton> buttons;
		if (jobId != null)
		{
			IReadOnlyList<NotchUpdateButton> readOnlyList = new global::_003C_003Ez__ReadOnlySingleElementList<NotchUpdateButton>(new NotchUpdateButton("I'm done", IsPrimary: true, Answered("user_step_done", "job:" + jobId, taskId, delegate
			{
				agents.ContinueJobAfterUserStep(jobId);
			})));
			buttons = readOnlyList;
		}
		else
		{
			IReadOnlyList<NotchUpdateButton> readOnlyList = Array.Empty<NotchUpdateButton>();
			buttons = readOnlyList;
		}
		return new NotchUpdateRequest("Your turn", step, buttons);
	}

	private static async Task ApproveExtraUsageAsync(AgentService agents, string taskId, bool asManyAsNeeded)
	{
		try
		{
			await agents.ApproveExtraUsageAsync(taskId, asManyAsNeeded);
		}
		catch (Exception ex)
		{
			Log.Warn("work", "extra usage approval: " + ex.Message);
		}
	}

	private static string StoppedDetail(string? reason)
	{
		if (reason?.Trim() == "Stopped by you.")
		{
			return "You stopped it before it finished.";
		}
		return FirstLine(reason) ?? "It was cut off before it finished.";
	}

	private NotchUpdateReadyOutput? ReadyOutput(IReadOnlyList<string> files, string? threadId, string? slug)
	{
		if (files.Count == 0)
		{
			return null;
		}
		string first = files[0];
		bool flag = ArtifactPaths.IsWebLink(first);
		NotchDocumentStyle notchDocumentStyle = NotchDocumentStyle.For(first, flag);
		return new NotchUpdateReadyOutput(flag ? "Open" : ("Open " + notchDocumentStyle.KindLabel), notchDocumentStyle.Glyph, files.Count > 1, flag, null, delegate
		{
			host.OpenChatArtifact(first, threadId, slug);
		});
	}

	private void Retry(AgentTaskDetail task, string? jobId)
	{
		string text = task.UserPrompts.LastOrDefault();
		if (text == null || text.Length <= 0)
		{
			return;
		}
		if (jobId != null)
		{
			agents.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = text,
				JobId = jobId,
				TargetThreadId = task.Snapshot.ThreadId,
				LaunchSource = "retry",
				IsGeneratedTaskBrief = true
			});
			return;
		}
		string clickySlug = task.ClickySlug;
		if (clickySlug != null)
		{
			agents.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = text,
				ClickySlug = clickySlug,
				LaunchSource = "retry",
				IsGeneratedTaskBrief = true
			});
		}
	}

	private void RetryJob(ClickyJob job)
	{
		string text = job.Runs?.LastOrDefault()?.Ask;
		string prompt = ((text != null && text.Length > 0) ? text : job.Prompt);
		agents.LaunchAsync(new AgentLaunchRequest
		{
			Prompt = prompt,
			JobId = job.Id,
			TargetThreadId = job.ThreadId,
			LaunchSource = "retry",
			IsGeneratedTaskBrief = true
		});
	}

	private static string? FirstLine(string? text)
	{
		return (from l in text?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			select l.Trim()).FirstOrDefault((string l) => l.Length > 0);
	}
}
