using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Notch.Window;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Tools;

namespace HeyClicky.App.Dock;

public sealed class ArcCardContentSource(AgentService agents, Func<OneBrainJobReports?> jobReports, ArcDockHost host)
{
	public sealed record LatestTurn(ArcLatestKind Kind, string Label, string Text, IReadOnlyList<string> Artifacts, string? ThreadId);

	public const string RestartInterruptionReason = "HeyClicky restarted before this finished.";

	public ArcCardContent ForJob(ClickyJob job)
	{
		ArcPaper paper = new ArcPaper(NotePaperColors.Stock("job:" + job.Id));
		if (job.IsWaitingToStart)
		{
			return new ArcCardContent
			{
				RailKey = job.RailKey,
				Title = HeyClickyWindow.ShortName(job.DisplayName),
				State = ArcCardState.Suggested,
				Paper = paper,
				Summary = ((job.Prompt.Length == 0) ? null : job.Prompt),
				JobId = job.Id,
				WorkspacePath = job.FolderPath
			};
		}
		ClickyJobRailPhase clickyJobRailPhase = agents.RailPhase(job);
		string text = agents.LiveTaskId(job.Id);
		AgentTaskDetail agentTaskDetail = ((text == null) ? null : agents.GetTaskDetail(text));
		string threadId = agentTaskDetail?.Snapshot.ThreadId ?? job.ThreadId;
		bool flag = clickyJobRailPhase == ClickyJobRailPhase.Done && (jobReports()?.IsPresenting(threadId) ?? false);
		ArcCardState state = (flag ? ArcCardState.Working : ArcCardStates.FromJobPhase(clickyJobRailPhase, job.IsSeen));
		string notePath = (ArcLiveNotes.IsEnabled ? ArcLiveNotes.NotePath(job.FolderPath) : null);
		string railKey = job.RailKey;
		bool flag2 = clickyJobRailPhase == ClickyJobRailPhase.Done && !flag;
		bool flag3 = (uint)clickyJobRailPhase <= 1u;
		bool flag4 = flag3;
		IReadOnlyList<string> readOnlyList2;
		if (!flag2)
		{
			IReadOnlyList<string> readOnlyList = Array.Empty<string>();
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = job.FilesFromLatestReply;
		}
		IReadOnlyList<string> files = readOnlyList2;
		ClickyAnswer clickyAnswer = (flag2 ? ResultCard(agentTaskDetail, threadId, job.Id, null) : null);
		string text2 = ((!flag2) ? null : (clickyAnswer?.Line ?? agentTaskDetail?.Summary ?? job.Summary));
		CardFooterRequest request = Request(text, agentTaskDetail) ?? JobRequest(job, clickyJobRailPhase, text != null);
		ArcCardContent arcCardContent = new ArcCardContent();
		arcCardContent.RailKey = job.RailKey;
		arcCardContent.Title = HeyClickyWindow.ShortName(job.DisplayName);
		arcCardContent.State = state;
		arcCardContent.Paper = paper;
		arcCardContent.Plan = (flag ? Finalizing(railKey, notePath) : Plan(railKey, notePath));
		arcCardContent.CurrentStepLine = (flag ? "Finalizing…" : (flag4 ? (agents.LiveSteps.Current(railKey, null) ?? JobCardText.BodyText(job, clickyJobRailPhase, (object)agentTaskDetail != null, LatestCommentary(agentTaskDetail), agentTaskDetail?.ThinkingHeading, (text == null) ? null : agents.PendingComputerUseRequest(text), agentTaskDetail?.FailureMessage)) : null));
		arcCardContent.Summary = ((text2 != null && text2.Trim().Length > 0) ? text2 : null);
		arcCardContent.Files = files;
		arcCardContent.ThreadId = job.ThreadId;
		arcCardContent.JobId = job.Id;
		arcCardContent.TaskId = text;
		arcCardContent.WorkspacePath = job.FolderPath;
		ArcCardContent arcCardContent2 = arcCardContent;
		flag3 = !flag;
		bool flag5;
		if (flag3)
		{
			AgentTaskStatus? agentTaskStatus = agentTaskDetail?.Snapshot.Status;
			if (agentTaskStatus.HasValue)
			{
				AgentTaskStatus valueOrDefault = agentTaskStatus.GetValueOrDefault();
				if ((uint)valueOrDefault <= 1u)
				{
					flag5 = true;
					goto IL_0339;
				}
			}
			flag5 = false;
			goto IL_0339;
		}
		goto IL_033d;
		IL_033d:
		arcCardContent2.TaskIsRunning = flag3;
		arcCardContent.Request = request;
		arcCardContent.ReadyOutput = (flag2 ? ReadyOutput(files, text2, job.ThreadId, job.FolderPath) : null);
		arcCardContent.LiveLine = (flag4 ? agents.LiveSteps.Line(railKey) : null);
		arcCardContent.Answer = clickyAnswer;
		arcCardContent.IsFinalizing = flag;
		arcCardContent.NotePath = notePath;
		return arcCardContent;
		IL_0339:
		flag3 = flag5;
		goto IL_033d;
	}

	public ArcCardContent ForClicky(string slug)
	{
		ClickyInfo clickyInfo = agents.FindClicky(slug);
		ArcPaper paper = new ArcPaper(NotePaperColors.Stock("clicky:" + slug));
		string text = ClickyTaskId(slug);
		AgentTaskDetail agentTaskDetail = ((text == null) ? null : agents.GetTaskDetail(text));
		LatestTurn latestTurn = Latest(slug, agentTaskDetail);
		bool flag = (object)agentTaskDetail == null && (object)latestTurn != null && latestTurn.Kind == ArcLatestKind.Failed && latestTurn.Label == "Interrupted";
		string text2 = latestTurn?.ThreadId ?? clickyInfo?.ThreadId;
		bool flag2 = (object)latestTurn != null && latestTurn.Kind == ArcLatestKind.Finished && (jobReports()?.IsPresenting(agentTaskDetail?.Snapshot.ThreadId ?? text2) ?? false);
		bool isUnread = host.UnreadClickySlugs().Contains(slug);
		ArcCardState arcCardState = (flag2 ? ArcCardState.Working : ArcCardStates.FromLatest(latestTurn?.Kind, isUnread));
		if (!flag2 && arcCardState == ArcCardState.Failed && (((object)agentTaskDetail != null && agentTaskDetail.Snapshot.Status == AgentTaskStatus.Stopped) || flag))
		{
			arcCardState = ArcCardState.Stopped;
		}
		string text3 = (ArcLiveNotes.IsEnabled ? ArcLiveNotes.ClickyNotePath(agents, slug) : null);
		int num;
		IReadOnlyList<string> readOnlyList;
		if ((object)latestTurn != null && latestTurn.Kind == ArcLatestKind.Finished)
		{
			num = ((!flag2) ? 1 : 0);
			if (num != 0)
			{
				readOnlyList = latestTurn.Artifacts;
				goto IL_017d;
			}
		}
		else
		{
			num = 0;
		}
		IReadOnlyList<string> readOnlyList2 = Array.Empty<string>();
		readOnlyList = readOnlyList2;
		goto IL_017d;
		IL_038f:
		bool flag3;
		bool flag4 = flag3;
		goto IL_0393;
		IL_0393:
		ArcCardContent arcCardContent;
		arcCardContent.TaskIsRunning = flag4;
		ArcCardContent arcCardContent2;
		CardFooterRequest request;
		arcCardContent2.Request = request;
		ArcCardContent arcCardContent3 = arcCardContent2;
		bool flag5 = (uint)(arcCardState - 5) <= 1u;
		IReadOnlyList<string> files;
		string text4;
		string text5;
		arcCardContent3.ReadyOutput = (flag5 ? ReadyOutput(files, text4, text2, text5) : null);
		arcCardContent2.LiveLine = ((arcCardState == ArcCardState.Working && !flag2 && text3 != null) ? agents.LiveSteps.Line(text3) : null);
		ClickyAnswer clickyAnswer;
		arcCardContent2.Answer = clickyAnswer;
		arcCardContent2.IsFinalizing = flag2;
		arcCardContent2.NotePath = text3;
		return arcCardContent2;
		IL_017d:
		files = readOnlyList;
		clickyAnswer = ((num != 0) ? ResultCard(agentTaskDetail, text2, null, slug) : null);
		text4 = ((num == 0) ? null : (clickyAnswer?.Line ?? agentTaskDetail?.Summary ?? latestTurn?.Text));
		request = Request(text, agentTaskDetail) ?? (((object)agentTaskDetail == null && arcCardState == ArcCardState.Failed) ? new CardFooterRequest(CardFooterRequest.Kinds.Failed, latestTurn?.Text ?? "") : null) ?? (flag ? new CardFooterRequest(CardFooterRequest.Kinds.Stopped, "HeyClicky restarted before this finished.") : null);
		text5 = agentTaskDetail?.WorkspacePath ?? agents.WorkspacePath(slug) ?? "";
		string title = HeyClickyWindow.ShortName(clickyInfo?.Name ?? slug);
		arcCardContent2 = new ArcCardContent();
		arcCardContent2.RailKey = slug;
		arcCardContent2.Title = title;
		arcCardContent2.State = arcCardState;
		arcCardContent2.Paper = paper;
		arcCardContent2.Plan = ((text3 == null) ? null : (flag2 ? Finalizing(text3, text3) : Plan(text3, text3)));
		arcCardContent2.CurrentStepLine = (flag2 ? "Finalizing…" : ((arcCardState != ArcCardState.Working) ? null : (((text3 != null) ? agents.LiveSteps.Current(text3, null) : null) ?? latestTurn?.Text)));
		arcCardContent2.Summary = ((text4 != null && text4.Trim().Length > 0) ? text4 : null);
		arcCardContent2.Files = files;
		arcCardContent2.ThreadId = text2;
		arcCardContent2.ClickySlug = slug;
		arcCardContent2.TaskId = text;
		arcCardContent2.WorkspacePath = text5;
		arcCardContent = arcCardContent2;
		flag4 = !flag2;
		if (flag4)
		{
			AgentTaskStatus? agentTaskStatus = agentTaskDetail?.Snapshot.Status;
			if (agentTaskStatus.HasValue)
			{
				AgentTaskStatus valueOrDefault = agentTaskStatus.GetValueOrDefault();
				if ((uint)valueOrDefault <= 1u)
				{
					flag3 = true;
					goto IL_038f;
				}
			}
			flag3 = false;
			goto IL_038f;
		}
		goto IL_0393;
	}

	public string? ClickyTaskId(string slug)
	{
		string bound = agents.FindClicky(slug)?.ThreadId;
		List<AgentTaskDetail> source = (from d in agents.Tasks.Select((AgentTaskSnapshot t) => agents.GetTaskDetail(t.TaskId)).OfType<AgentTaskDetail>()
			where !d.IsDemo
			select d).ToList();
		object obj = ((bound != null) ? source.FirstOrDefault((AgentTaskDetail d) => d.Snapshot.ThreadId == bound) : null)?.Snapshot.TaskId;
		if (obj == null)
		{
			AgentTaskDetail? agentTaskDetail = source.FirstOrDefault((AgentTaskDetail d) => d.ClickySlug == slug);
			if ((object)agentTaskDetail == null)
			{
				return null;
			}
			obj = agentTaskDetail.Snapshot.TaskId;
		}
		return (string?)obj;
	}

	private LatestTurn? Latest(string slug, AgentTaskDetail? detail)
	{
		ClickyAppLiveStatus clickyAppLiveStatus = agents.LiveStatusForClicky(slug);
		switch (clickyAppLiveStatus.Phase)
		{
		case ClickyAppLiveStatus.Phases.NeedsYou:
			return new LatestTurn(ArcLatestKind.NeedsYou, "Needs you", clickyAppLiveStatus.Line, Array.Empty<string>(), detail?.Snapshot.ThreadId);
		case ClickyAppLiveStatus.Phases.Working:
		{
			DateTimeOffset? finishedAt = clickyAppLiveStatus.StartedAt;
			object obj2;
			if (finishedAt.HasValue)
			{
				DateTimeOffset valueOrDefault2 = finishedAt.GetValueOrDefault();
				obj2 = " · " + Relative(valueOrDefault2);
			}
			else
			{
				obj2 = "";
			}
			return new LatestTurn(ArcLatestKind.Working, "Working" + (string?)obj2, clickyAppLiveStatus.Line, Array.Empty<string>(), detail?.Snapshot.ThreadId);
		}
		case ClickyAppLiveStatus.Phases.Failed:
			return new LatestTurn(ArcLatestKind.Failed, "Didn't finish", clickyAppLiveStatus.Line, Array.Empty<string>(), detail?.Snapshot.ThreadId);
		default:
		{
			ClickyWorkPiece openQuestion = agents.ClickyApps.Shelf(slug).OpenQuestion;
			if ((object)openQuestion != null)
			{
				return new LatestTurn(ArcLatestKind.NeedsYou, "Waiting for your answer", (openQuestion.Summary.Length == 0) ? openQuestion.Title : openQuestion.Summary, Array.Empty<string>(), openQuestion.ThreadId);
			}
			if (clickyAppLiveStatus.Phase == ClickyAppLiveStatus.Phases.Finished && (object)detail != null)
			{
				DateTimeOffset? finishedAt = clickyAppLiveStatus.FinishedAt;
				object obj;
				if (finishedAt.HasValue)
				{
					DateTimeOffset valueOrDefault = finishedAt.GetValueOrDefault();
					obj = " · " + Relative(valueOrDefault);
				}
				else
				{
					obj = "";
				}
				return new LatestTurn(ArcLatestKind.Finished, "Latest" + (string?)obj, clickyAppLiveStatus.Line, detail.Artifacts, detail.Snapshot.ThreadId);
			}
			ClickyWorkPiece clickyWorkPiece = (from w in agents.ClickyApps.Works(slug)
				orderby w.FinishedAt descending
				select w).FirstOrDefault();
			if ((object)detail == null)
			{
				ClickyRestartInterruptions.Turn turn = agents.RestartInterruptions.Interruption(slug);
				if ((object)turn != null && ((object)clickyWorkPiece == null || clickyWorkPiece.FinishedAt < turn.StartedAt))
				{
					return new LatestTurn(ArcLatestKind.Failed, "Interrupted", "HeyClicky restarted before this finished.", Array.Empty<string>(), turn.ThreadId);
				}
			}
			if ((object)clickyWorkPiece == null)
			{
				return null;
			}
			int kind = (clickyWorkPiece.Failed ? 3 : 2);
			string label = (clickyWorkPiece.Failed ? "Didn't finish" : "Latest") + " · " + Relative(clickyWorkPiece.FinishedAt);
			string text = ((clickyWorkPiece.Summary.Length == 0) ? clickyWorkPiece.Title : clickyWorkPiece.Summary);
			IReadOnlyList<string> artifacts;
			if (!clickyWorkPiece.Failed)
			{
				artifacts = clickyWorkPiece.ArtifactPaths;
			}
			else
			{
				IReadOnlyList<string> readOnlyList = Array.Empty<string>();
				artifacts = readOnlyList;
			}
			return new LatestTurn((ArcLatestKind)kind, label, text, artifacts, clickyWorkPiece.ThreadId);
		}
		}
	}

	private static string Relative(DateTimeOffset date)
	{
		TimeSpan timeSpan = DateTimeOffset.Now - date;
		if (timeSpan.TotalSeconds < 60.0)
		{
			return "just now";
		}
		if (timeSpan.TotalMinutes < 60.0)
		{
			int num = (int)timeSpan.TotalMinutes;
			if (num != 1)
			{
				return $"{num} minutes ago";
			}
			return "1 minute ago";
		}
		if (timeSpan.TotalHours < 24.0)
		{
			int num2 = (int)timeSpan.TotalHours;
			if (num2 != 1)
			{
				return $"{num2} hours ago";
			}
			return "1 hour ago";
		}
		int num3 = (int)timeSpan.TotalDays;
		if (num3 != 1)
		{
			return $"{num3} days ago";
		}
		return "1 day ago";
	}

	private static string? LatestCommentary(AgentTaskDetail? detail)
	{
		return detail?.Activity.LastOrDefault((AgentActivityItem a) => a.Kind == AgentActivityKind.Commentary)?.Text;
	}

	private ClickyArcPlan? Plan(string key, string? notePath)
	{
		ClickyArcPlan clickyArcPlan = agents.LiveSteps.Plan(key);
		if ((object)clickyArcPlan != null && !clickyArcPlan.IsEmpty)
		{
			return clickyArcPlan;
		}
		if (notePath == null)
		{
			return null;
		}
		return host.NoteChecklistPlan(notePath);
	}

	private ClickyArcPlan? Finalizing(string key, string? notePath)
	{
		ClickyArcPlan clickyArcPlan = agents.LiveSteps.LastPlan(key) ?? ((notePath != null) ? host.NoteChecklistPlan(notePath) : null);
		if ((object)clickyArcPlan == null || clickyArcPlan.IsEmpty)
		{
			return null;
		}
		return clickyArcPlan.Finalizing;
	}

	private ClickyAnswer? ResultCard(AgentTaskDetail? detail, string? threadId, string? jobId, string? slug)
	{
		if (OneBrainJobReports.IsOn && threadId != null)
		{
			ClickyAnswer clickyAnswer = agents.ResultCard(threadId);
			if ((object)clickyAnswer != null)
			{
				return clickyAnswer;
			}
		}
		return detail?.Answer ?? host.LatestReplyAnswer(jobId, slug);
	}

	private CardFooterRequest? Request(string? taskId, AgentTaskDetail? detail)
	{
		if (taskId == null || (object)detail == null)
		{
			return null;
		}
		string text = agents.PendingComputerUseRequest(taskId);
		AgentTaskStatus status = detail.Snapshot.Status;
		string failureSummary = ((status != AgentTaskStatus.Stopped) ? detail.FailureMessage : ((detail.ProgressSummary.Trim() == "Stopped.") ? "Stopped by you." : detail.ProgressSummary));
		string? pendingComputerUseSummary = agents.PendingComputerUseSummary(taskId);
		string speakableRequest = ((text == null) ? "" : ClickyNotes.SpeakableSentence(text, 110));
		bool requiresExtraUsageApproval = detail.RequiresExtraUsageApproval;
		string threadId = detail.Snapshot.ThreadId;
		return CardFooterRequest.FromTask(hasTask: true, text, pendingComputerUseSummary, speakableRequest, requiresExtraUsageApproval, threadId != null && agents.NeedsChromeConnection(threadId), status == AgentTaskStatus.Finished, detail.PendingUserStep?.Step, agents.CanRetryAfterFailure(taskId), agents.CanRetryAfterInterruption(taskId), failureSummary);
	}

	private static CardFooterRequest? JobRequest(ClickyJob job, ClickyJobRailPhase phase, bool hasTask)
	{
		if (job.IsHeldForChrome)
		{
			return new CardFooterRequest(CardFooterRequest.Kinds.ConnectChrome);
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
			return new CardFooterRequest(CardFooterRequest.Kinds.UserStep, waitingStep.Step);
		}
		if (hasTask || job.ThreadId == null)
		{
			return null;
		}
		return phase switch
		{
			ClickyJobRailPhase.Failed => new CardFooterRequest(CardFooterRequest.Kinds.Failed, job.Summary ?? ""), 
			ClickyJobRailPhase.Stopped => new CardFooterRequest(CardFooterRequest.Kinds.Stopped), 
			_ => null, 
		};
	}

	public static CardReadyOutput ReadyOutput(IReadOnlyList<string> files, string? summary, string? threadId, string? workspace)
	{
		return CardReadyOutput.Make(files, summary, threadId, (IReadOnlyList<string> entries) => MainEntries(entries, workspace), (string e) => DockFiles.Resolve(e, workspace), FileCardStack.FileKind);
	}

	public static IReadOnlyList<string> MainEntries(IReadOnlyList<string> entries, string? workspace)
	{
		return ClickyMainArtifacts.MainEntries(entries, (string e) => DockFiles.Resolve(e, workspace));
	}

	public static IReadOnlyList<string> CardEntries(ArcCardContent content)
	{
		HashSet<string> seen = new HashSet<string>();
		List<string> list = MainEntries(content.Files.Where((string f) => f.Length > 0 && seen.Add(f)).ToList(), content.WorkspacePath).ToList();
		string text = content.ReadyOutput?.CardEntry;
		if (text != null)
		{
			list.Remove(text);
			list.Insert(0, text);
		}
		return list;
	}
}
