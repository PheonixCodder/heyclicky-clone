using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Notch.Window;
using HeyClicky.Core;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Tools;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;
using HeyClicky.Platform.Win32;

namespace HeyClicky.App.Wiring;

public sealed class AgentVoiceRelay
{
	private readonly AgentService agents;

	private readonly CoreServices core;

	private readonly CompanionSession companion;

	private readonly BrainContext context;

	private readonly ConcurrentDictionary<string, bool> announcedFinish = new ConcurrentDictionary<string, bool>();

	private bool VoiceAnnouncementsEnabled => core.Settings.Read((AppSettings s) => s.AgentAnnouncementsVoiceEnabled);

	public Action<string>? ShowAtCursor { get; set; }

	public OneBrainJobReports? JobReports { get; set; }

	public Func<AgentTaskDetail, bool>? FinishShowsOnNotchCard { get; set; }

	private static bool JobsWorld => ClickyJobsPreferences.IsEnabled;

	public static Func<AgentTaskDetail, string?>? ResultFacts { get; set; }

	public AgentVoiceRelay(AgentService agents, CoreServices core, CompanionSession companion, BrainContext context)
	{
		this.agents = agents;
		this.core = core;
		this.companion = companion;
		this.context = context;
		agents.TaskEvent += delegate(object? _, AgentTaskEvent e)
		{
			try
			{
				OnTaskEvent(e);
			}
			catch (Exception ex)
			{
				Log.Warn("AgentVoice", $"{e.Kind}: {ex.Message}");
			}
		};
	}

	private string BrainName(AgentTaskDetail detail)
	{
		string jobId = detail.JobId;
		string? text;
		if (jobId != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(jobId);
			if ((object)clickyJob != null)
			{
				string title = clickyJob.Title;
				if (title != null && title.Length > 0)
				{
					text = title;
					goto IL_0049;
				}
			}
		}
		text = detail.ClickyName;
		if (text == null)
		{
			return detail.Snapshot.Title;
		}
		goto IL_0049;
		IL_0049:
		return text;
	}

	private void OnTaskEvent(AgentTaskEvent e)
	{
		AgentTaskDetail taskDetail = agents.GetTaskDetail(e.TaskId);
		if ((object)taskDetail == null)
		{
			return;
		}
		string text = BrainName(taskDetail);
		string clickySlug = taskDetail.ClickySlug;
		string text2 = ClickyNotes.ProgressNoteKey(clickySlug, taskDetail.Snapshot.ThreadId, e.TaskId);
		switch (e.Kind)
		{
		case AgentTaskEventKind.Commentary:
			if (!string.IsNullOrWhiteSpace(e.Text) && !taskDetail.IsDemo)
			{
				AgentTaskStatus status = taskDetail.Snapshot.Status;
				if ((uint)status <= 1u)
				{
					string note = ClickyNotes.ProgressNote(text, clickySlug, taskDetail.Snapshot.ThreadId, e.Text, JobsWorld, taskDetail.JobId != null);
					core.BrainSession.NoteForNextTurn(note, text2);
					Log.Info("AgentVoice", "progress for the next turn: " + ClickyNotes.Condensed(e.Text).Substring(0, Math.Min(80, ClickyNotes.Condensed(e.Text).Length)));
				}
			}
			break;
		case AgentTaskEventKind.Finished:
		{
			string text3 = taskDetail.Summary ?? taskDetail.Snapshot.FinalResponseMarkdown ?? e.Text ?? "";
			Report(taskDetail, text2, "finished", text3);
			string turnKey = OneBrainJobReports.TurnKey(e.TaskId, taskDetail.TurnId);
			bool flag = agents.IsTaskWaitingOnUserAnswer(e.TaskId);
			if (!taskDetail.IsDemo && !flag && agents.FullReplyText(e.TaskId) != null)
			{
				JobReports?.Present(turnKey, e.TaskId, taskDetail.Snapshot.ThreadId, text);
			}
			if (taskDetail.IsDemo)
			{
				Log.Info("AgentVoice", "finish not announced: an onboarding demo agent");
			}
			else
			{
				if (flag)
				{
					break;
				}
				Func<AgentTaskDetail, bool>? finishShowsOnNotchCard = FinishShowsOnNotchCard;
				if (finishShowsOnNotchCard != null && finishShowsOnNotchCard(taskDetail))
				{
					Log.Info("AgentVoice", "finish not spoken: its result shows on the notch's card");
				}
				else
				{
					if (!announcedFinish.TryAdd(e.TaskId + ":" + (taskDetail.TurnId ?? taskDetail.Snapshot.StartedAt.ToString("O")), value: true))
					{
						break;
					}
					if (QuietContext.IsQuiet() || (!QuietContext.IgnoredForTests && QuietContext.QueryNotificationState() == QuietContext.NotificationState.PresentationMode))
					{
						Log.Info("AgentVoice", "finish not announced: quiet (a call, presenting or Do Not Disturb)");
						break;
					}
					bool flag2 = core.Settings.Read((AppSettings s) => s.AgentCursorTextAnnouncementsEnabled) && !HeyClickyWindow.KeepsAgentLinesOffCursor;
					if (string.IsNullOrWhiteSpace(text3) || (!VoiceAnnouncementsEnabled && !flag2))
					{
						break;
					}
					if (VoiceAnnouncementsEnabled)
					{
						OneBrainJobReports? jobReports = JobReports;
						if (jobReports != null && jobReports.SpeakWhenReady(turnKey))
						{
							break;
						}
					}
					string text4 = ClickyNotes.FinishedLine(text, text3);
					if (VoiceAnnouncementsEnabled)
					{
						companion.AnnounceAsync(text4, flag2);
					}
					else
					{
						ShowAtCursor?.Invoke(text4);
					}
				}
			}
			break;
		}
		case AgentTaskEventKind.Failed:
			Report(taskDetail, text2, "failed", taskDetail.FailureMessage ?? e.Text);
			break;
		case AgentTaskEventKind.Stopped:
			Report(taskDetail, text2, "was stopped before finishing", e.Text);
			break;
		case AgentTaskEventKind.NeedsExtraUsageApproval:
			if (!taskDetail.IsDemo)
			{
				core.BrainSession.NoteForNextTurn(JobsWorld ? ClickyNotes.JobEventNote(text, taskDetail.Snapshot.ThreadId, "is paused: it needs more agent messages from their plan to finish, and a yes from the user (sessions_answer_request, request extra_usage) keeps it going", null) : ClickyNotes.EventNote(taskDetail.ClickyName, clickySlug, taskDetail.Snapshot.Title, "is paused: it needs more agent messages from their plan to finish, and a yes from the user (sessions_answer_request, request extra_usage) keeps it going", null));
				if (VoiceAnnouncementsEnabled)
				{
					companion.AnnounceAsync(ClickyNotes.PausedForExtraUsageLine(text));
				}
			}
			break;
		case AgentTaskEventKind.ResponseDelta:
		case AgentTaskEventKind.ToolActivity:
		case AgentTaskEventKind.TitleChanged:
			break;
		}
	}

	private void Report(AgentTaskDetail detail, string progressKey, string @event, string? summary)
	{
		if (!detail.IsDemo)
		{
			core.BrainSession.DropNoteForNextTurn(progressKey);
			core.BrainSession.NoteForNextTurn(JobsWorld ? ClickyNotes.JobEventNote(BrainName(detail), detail.Snapshot.ThreadId, @event, summary, (@event == "finished") ? agents.ResultReport(detail.Snapshot.TaskId) : null) : ClickyNotes.EventNote(detail.ClickyName, detail.ClickySlug, detail.Snapshot.Title, @event, summary));
			string clickySlug = detail.ClickySlug;
			if (clickySlug != null)
			{
				context.LastReport = new ClickyNotes.Report(clickySlug, detail.ClickyName ?? detail.Snapshot.Title, @event, DateTimeOffset.Now);
			}
			Log.Info("AgentVoice", (detail.ClickyName ?? detail.Snapshot.Title) + " " + @event + ": noted for the next turn");
		}
	}

	public async Task SpeakStartCueAsync(string prompt)
	{
		if (!VoiceAnnouncementsEnabled || string.IsNullOrWhiteSpace(prompt))
		{
			return;
		}
		try
		{
			string content = new JsonObject((JsonNodeOptions?)null)
			{
				["user_prompt"] = prompt,
				["is_fresh_thread"] = false,
				["launch_source"] = "voice"
			}.ToJsonString();
			WorkerClient worker = core.Worker;
			HttpMethod post = HttpMethod.Post;
			StringContent body = new StringContent(content, Encoding.UTF8, "application/json");
			TimeSpan? timeout = TimeSpan.FromSeconds(8L);
			byte[] array = await worker.SendRawAsync(post, "/codex-thread-launch", body, authenticated: true, default(CancellationToken), timeout, "companion").ConfigureAwait(continueOnCapturedContext: false);
			if (array != null)
			{
				string text = (string?)(JsonNode.Parse(array, null) as JsonObject)?["spoken_start_cue"];
				if (!string.IsNullOrWhiteSpace(text))
				{
					Log.Info("AgentVoice", "start cue: " + text);
					await companion.AnnounceAsync(ClickyNotes.StartedLine(text)).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("AgentVoice", "start cue: " + ex.Message);
		}
	}
}
