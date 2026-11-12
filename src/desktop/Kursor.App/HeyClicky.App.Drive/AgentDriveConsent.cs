using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.Core.Drive;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.App.Drive;

public sealed class AgentDriveConsent
{
	private readonly ClickyTakeOverController drive;

	private readonly IAgentDriveJobs jobs;

	private readonly MacStepWaiters waiters;

	public const string MacStepResultLaunchContext = "HeyClicky did the step on their PC you asked for and is passing on what happened (the user didn't type this message).";

	public const string MacStepAnswerLaunchContext = "HeyClicky is passing on the user's answer to your request for a step on their PC (they chose it on HeyClicky's card; they didn't type this message).";

	public Action<string>? ShowCursorLine { get; set; }

	public bool AgentClickyDriveIsAvailable => drive.IsAvailable();

	public bool IsPcStepMomentInProgress
	{
		get
		{
			if (!drive.IsDriving && !(DateTimeOffset.Now - drive.LastEndedAt < TimeSpan.FromSeconds(20L)))
			{
				return waiters.HasStepAsked(TimeSpan.FromSeconds(60L));
			}
			return true;
		}
	}

	public AgentDriveConsent(ClickyTakeOverController drive, IAgentDriveJobs jobs, MacStepWaiters? waiters = null)
	{
		this.drive = drive;
		this.jobs = jobs;
		this.waiters = waiters ?? MacStepWaiters.Shared;
		drive.MacStepBindingForBrainDrive = MacStepBindingForBrainDrive;
	}

	public void AnnounceAgentAsksToUsePcIfNeeded(IAgentDriveJob job, string requestKind = "end_of_turn")
	{
		string pendingComputerUseRequest = job.PendingComputerUseRequest;
		if (pendingComputerUseRequest != null && !job.DidAnnounceComputerUseRequest)
		{
			job.DidAnnounceComputerUseRequest = true;
			ClickyDriveAnalytics.TrackMacStepRequested(job.ThreadId, requestKind, AgentClickyDriveIsAvailable);
			jobs.NoteEventForBrain(job, "is waiting for a yes to let HeyClicky drive their PC for one step", pendingComputerUseRequest);
			drive.PlayChime("agent-needs-you", 0.8);
			jobs.AnnounceVerbatim(ClickyDriveLines.AsksToUsePc((job.Name == "Your job") ? null : job.Name, job.PendingComputerUseSummary ?? pendingComputerUseRequest));
		}
	}

	public void ApproveComputerUse(string taskId)
	{
		LetClickyDriveForAgent(taskId);
	}

	public void LetClickyDriveForAgent(string taskId)
	{
		IAgentDriveJob agentDriveJob = jobs.Find(taskId);
		if (agentDriveJob != null && DriveWaitingMacStep(agentDriveJob))
		{
			return;
		}
		IAgentDriveJob agentDriveJob2 = jobs.Find(taskId);
		if (agentDriveJob2 == null)
		{
			return;
		}
		string threadId = agentDriveJob2.ThreadId;
		if (threadId == null)
		{
			return;
		}
		string request = agentDriveJob2.PendingComputerUseRequest;
		if (request == null)
		{
			return;
		}
		string askingLease = agentDriveJob2.AgentTurnLeaseId;
		string name = agentDriveJob2.Name;
		agentDriveJob2.PendingComputerUseRequest = null;
		ClickyAnalytics.TrackComputerUseConsent(approved: true, always: false);
		ClickyDriveAnalytics.TrackMacStepAnswered(threadId, "approve", "end_of_turn");
		waiters.ForgetUnanswered(taskId);
		if (!AgentClickyDriveIsAvailable)
		{
			Log.Info("drive", "clicky drive unavailable for thread=" + threadId);
			ClickyDriveAnalytics.Track("start_refused", threadId, new JsonObject((JsonNodeOptions?)null)
			{
				["reason"] = "drive_unavailable",
				["origin"] = "job_step_approved"
			});
			Report(AgentDriveReports.Unavailable(request));
			return;
		}
		bool flag = drive.DriveForAgent(request, name, askingLease, delegate(AgentDriveOutcome outcome)
		{
			Log.Info("drive", $"clicky drive for thread={threadId} ended actions={outcome.Actions.Count}");
			Report(AgentDriveReports.Report(outcome, request));
		}, threadId);
		Log.Info("drive", $"clicky drive for thread={threadId} started={flag}");
		if (!flag)
		{
			Report(AgentDriveReports.Busy(request));
		}
		void Report(string answer)
		{
			DeliverMacStepOutcome(taskId, threadId, askingLease, answer);
		}
	}

	public void DeliverMacStepOutcome(string taskId, string threadId, string? askingTurnLeaseId, string report)
	{
		waiters.ForgetUnanswered(taskId);
		if (waiters.IsWaiting(taskId))
		{
			ClickyDriveAnalytics.TrackMacStepResultDelivered(threadId, "open_call");
			waiters.Resolve(taskId, new MacStepAnswer.Drove(report));
			return;
		}
		waiters.EndDrive(taskId);
		IAgentDriveJob agentDriveJob = jobs.Find(taskId);
		if (agentDriveJob != null)
		{
			agentDriveJob.PendingComputerUseRequest = null;
		}
		Log.Info("drive", "step result for thread=" + threadId + " delivered as a message");
		if (jobs.IsThreadActive(threadId))
		{
			ClickyDriveAnalytics.TrackMacStepResultDelivered(threadId, "steer");
			jobs.SubmitFollowUp(threadId, report, null, "HeyClicky did the step on their PC you asked for and is passing on what happened (the user didn't type this message).");
		}
		else
		{
			ClickyDriveAnalytics.TrackMacStepResultDelivered(threadId, "revive");
			ContinueAskingTurnAsync(threadId, askingTurnLeaseId, report);
		}
	}

	public AgentDriveBinding? MacStepBindingForBrainDrive(string goal)
	{
		List<(IAgentDriveJob, string, string, string)> list = (from j in jobs.Jobs
			where j.ThreadId != null && (waiters.Step(j.TaskId) ?? j.PendingComputerUseRequest) != null
			select (Job: j, ThreadId: j.ThreadId, Request: waiters.Step(j.TaskId) ?? j.PendingComputerUseRequest, Title: j.Title)).ToList();
		(IAgentDriveJob, string, string, string) tuple = list.FirstOrDefault<(IAgentDriveJob, string, string, string)>(((IAgentDriveJob Job, string ThreadId, string Request, string Title) c) => AgentDriveReports.DriveGoalNamesJob(goal, c.ThreadId, c.Title));
		if (tuple.Item1 == null && list.Count == 1 && AgentDriveReports.DriveGoalSharesWordsWithStep(goal, list[0].Item3))
		{
			tuple = list[0];
		}
		if (tuple.Item1 == null)
		{
			return null;
		}
		string taskId = tuple.Item1.TaskId;
		string askingLease = tuple.Item1.AgentTurnLeaseId;
		tuple.Item1.PendingComputerUseRequest = null;
		waiters.ForgetUnanswered(taskId);
		if (waiters.IsWaiting(taskId))
		{
			waiters.MarkDriving(taskId);
		}
		ClickyAnalytics.TrackComputerUseConsent(approved: true, always: false);
		ClickyDriveAnalytics.TrackMacStepAnswered(tuple.Item2, "approve", "brain_drive");
		Log.Info("drive", "the brain's drive is the step of thread=" + tuple.Item2);
		string request = tuple.Item3;
		string threadId = tuple.Item2;
		return new AgentDriveBinding(tuple.Item4, askingLease, delegate(AgentDriveOutcome outcome)
		{
			DeliverMacStepOutcome(taskId, threadId, askingLease, AgentDriveReports.Report(outcome, request));
		}, threadId);
	}

	private async Task ContinueAskingTurnAsync(string threadId, string? askingTurnLeaseId, string answer)
	{
		string reopened = null;
		if (askingTurnLeaseId != null)
		{
			try
			{
				reopened = await jobs.ContinueLeaseAfterConsentAsync(askingTurnLeaseId).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Warn("drive", "lease not reopened: " + ex.Message);
			}
			if (reopened == null)
			{
				jobs.CompleteLease(askingTurnLeaseId, threadId);
			}
		}
		Log.Info("drive", $"answer thread={threadId} continuesAskingTurn={reopened != null}");
		jobs.SubmitFollowUp(threadId, answer, reopened, "HeyClicky is passing on the user's answer to your request for a step on their PC (they chose it on HeyClicky's card; they didn't type this message).");
	}

	public void DeclineComputerUse(string taskId)
	{
		waiters.ForgetUnanswered(taskId);
		string text = waiters.Step(taskId) ?? jobs.Find(taskId)?.PendingComputerUseRequest;
		if (text != null)
		{
			waiters.RememberDeclined(taskId, text);
		}
		try
		{
			ShowCursorLine?.Invoke("Okay, I'll leave that step to you. The job finishes without it.");
		}
		catch (Exception ex)
		{
			Log.Warn("drive", "decline line: " + ex.Message);
		}
		if (waiters.IsWaiting(taskId))
		{
			IAgentDriveJob agentDriveJob = jobs.Find(taskId);
			if (agentDriveJob != null)
			{
				agentDriveJob.PendingComputerUseRequest = null;
			}
			ClickyAnalytics.TrackComputerUseConsent(approved: false, always: false);
			ClickyDriveAnalytics.TrackMacStepAnswered(agentDriveJob?.ThreadId, "decline", "waiting_call");
			waiters.Resolve(taskId, MacStepAnswer.DeclinedAnswer);
			return;
		}
		IAgentDriveJob agentDriveJob2 = jobs.Find(taskId);
		if (agentDriveJob2 != null)
		{
			string threadId = agentDriveJob2.ThreadId;
			if (threadId != null)
			{
				string agentTurnLeaseId = agentDriveJob2.AgentTurnLeaseId;
				agentDriveJob2.PendingComputerUseRequest = null;
				ClickyAnalytics.TrackComputerUseConsent(approved: false, always: false);
				ClickyDriveAnalytics.TrackMacStepAnswered(threadId, "decline", "end_of_turn");
				Log.Info("drive", "decline thread=" + threadId);
				ContinueAskingTurnAsync(threadId, agentTurnLeaseId, "Not now. Finish without controlling my PC, and tell me what you did get done. Don't ask to do this step on my PC again unless I tell you to go ahead with it.");
			}
		}
	}

	public async Task<JsonObject> AnswerMacStepToolCallAsync(string threadId, string step, string why, string ask = "")
	{
		IAgentDriveJob job = jobs.Jobs.FirstOrDefault((IAgentDriveJob j) => j.ThreadId == threadId);
		if (job == null)
		{
			return new JsonObject((JsonNodeOptions?)null) { ["error"] = "No running job for this call." };
		}
		if (!AgentClickyDriveIsAvailable)
		{
			ClickyDriveAnalytics.TrackMacStepCallReturned(threadId, "unavailable");
			return AgentDriveReports.MacStepUnavailable();
		}
		if ((job.NeedsChromeConnection || job.FoundChromeNotConnectedThisTurn) && !jobs.ChromeIsConnected && !jobs.SkipsChrome(threadId))
		{
			ClickyDriveAnalytics.TrackMacStepCallReturned(threadId, "waiting_on_chrome_choice");
			return job.NeedsChromeConnection ? AgentDriveReports.MacStepChoosingChrome() : AgentDriveReports.MacStepChromeAskedAtTurnEnd();
		}
		string taskId = job.TaskId;
		string text = waiters.Step(taskId);
		if (text != null)
		{
			if (text != step)
			{
				ClickyDriveAnalytics.TrackMacStepCallReturned(threadId, "busy");
				return AgentDriveReports.MacStepBusy();
			}
			MacStepAnswer answer = await waiters.WaitAsync(taskId, step, AgentDriveReports.MacStepOpenCall, StillRunning).ConfigureAwait(continueOnCapturedContext: false);
			ClickyDriveAnalytics.TrackMacStepCallReturned(threadId, answer, joinedWaitingCall: true);
			return AgentDriveReports.MacStepToolResult(answer, step);
		}
		if (job.PendingComputerUseRequest != null)
		{
			ClickyDriveAnalytics.TrackMacStepCallReturned(threadId, "busy");
			return AgentDriveReports.MacStepBusy();
		}
		string shown = AgentDriveReports.ShownRequest(step, why);
		if (waiters.WasRecentlyDeclined(taskId, step) || waiters.WasRecentlyDeclined(taskId, shown))
		{
			ClickyDriveAnalytics.TrackMacStepCallReturned(threadId, "declined_before");
			return AgentDriveReports.MacStepDeclinedBefore();
		}
		job.PendingComputerUseRequest = shown;
		job.PendingComputerUseSummary = AgentDriveReports.MacStepCardLine(ask, why);
		job.DidAnnounceComputerUseRequest = false;
		AnnounceAgentAsksToUsePcIfNeeded(job, "mac_step_call");
		jobs.PresentNeedsYou(job);
		Log.Info("drive", "mac_step asked thread=" + threadId);
		MacStepAnswer macStepAnswer = await waiters.WaitAsync(taskId, step, AgentDriveReports.MacStepOpenCall, StillRunning).ConfigureAwait(continueOnCapturedContext: false);
		if (macStepAnswer is MacStepAnswer.NotYet)
		{
			waiters.RememberUnanswered(taskId, shown);
		}
		else if (job.PendingComputerUseRequest == shown)
		{
			job.PendingComputerUseRequest = null;
		}
		ClickyDriveAnalytics.TrackMacStepCallReturned(threadId, macStepAnswer, joinedWaitingCall: false);
		return AgentDriveReports.MacStepToolResult(macStepAnswer, step);
		bool StillRunning()
		{
			return jobs.Find(taskId)?.IsRunning ?? false;
		}
	}

	public bool DriveWaitingMacStep(IAgentDriveJob job)
	{
		string taskId = job.TaskId;
		string step = waiters.Step(taskId);
		if (step == null)
		{
			return false;
		}
		job.PendingComputerUseRequest = null;
		ClickyAnalytics.TrackComputerUseConsent(approved: true, always: false);
		ClickyDriveAnalytics.TrackMacStepAnswered(job.ThreadId, "approve", "waiting_call");
		waiters.MarkDriving(taskId);
		string threadId = job.ThreadId;
		string askingLease = job.AgentTurnLeaseId;
		if (!drive.DriveForAgent(step, job.Name, askingLease, delegate(AgentDriveOutcome outcome)
		{
			string report = AgentDriveReports.Report(outcome, step);
			if (threadId == null)
			{
				ClickyDriveAnalytics.TrackMacStepResultDelivered(threadId, "open_call");
				waiters.Resolve(taskId, new MacStepAnswer.Drove(report));
			}
			else
			{
				DeliverMacStepOutcome(taskId, threadId, askingLease, report);
			}
		}, threadId))
		{
			waiters.Resolve(taskId, MacStepAnswer.CouldNotStartAnswer);
		}
		return true;
	}
}
