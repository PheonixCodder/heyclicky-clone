using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.Agents.Routines;
using HeyClicky.App.Ink;
using HeyClicky.Core.Ink;

namespace HeyClicky.App;

public sealed class AgentsInkNoteHost(AgentService agents) : IClickyInkNoteHost
{
	private const double RoutineRunStaleAfterSeconds = 7200.0;

	public Func<string, ClickyInkJob?>? FindJob { get; set; }

	public Func<string, bool>? IsUnread { get; set; }

	public Func<string, ClickyInkClickyLive>? LiveStatus { get; set; }

	public Action<string, string>? OpenJobArtifactHandler { get; set; }

	public Func<string, string?, string?, bool>? OpenInViewer { get; set; }

	public Action<string, int, string>? SetInterval { get; set; }

	public Action<string, string, string>? SendOnThread { get; set; }

	public static ClickyInkClickyLive InkLive(ClickyAppLiveStatus live)
	{
		return new ClickyInkClickyLive(live.Phase switch
		{
			ClickyAppLiveStatus.Phases.Working => ClickyInkClickyPhase.Working, 
			ClickyAppLiveStatus.Phases.NeedsYou => ClickyInkClickyPhase.NeedsYou, 
			ClickyAppLiveStatus.Phases.Failed => ClickyInkClickyPhase.Failed, 
			ClickyAppLiveStatus.Phases.Finished => ClickyInkClickyPhase.Finished, 
			_ => ClickyInkClickyPhase.Idle, 
		}, live.FinishedAt);
	}

	public bool ClickyExists(string slug)
	{
		return (object)agents.FindClicky(slug) != null;
	}

	public ClickyInkClickyLive ClickyLiveStatus(string slug)
	{
		Func<string, ClickyInkClickyLive> liveStatus = LiveStatus;
		if (liveStatus != null)
		{
			return liveStatus(slug);
		}
		return InkLive(agents.LiveStatusForClicky(slug));
	}

	public bool IsClickyUnread(string slug)
	{
		return IsUnread?.Invoke(slug) ?? false;
	}

	public ClickyInkJob? Job(string jobId)
	{
		return FindJob?.Invoke(jobId);
	}

	public IReadOnlyList<ClickyInkRoutineControl> RoutineControls(string slug, DateTimeOffset now)
	{
		return agents.Routines.ForClicky(slug).Select(delegate(ClickyRoutine routine)
		{
			string id = routine.Id;
			string title = routine.Title;
			int intervalMinutes = routine.IntervalMinutes;
			bool isPaused = routine.IsPaused;
			bool isPausedAfterRepeatedFailures = routine.IsPausedAfterRepeatedFailures;
			DateTimeOffset? lastRunStartedAt = routine.LastRunStartedAt;
			int isRunning;
			if (lastRunStartedAt.HasValue)
			{
				DateTimeOffset valueOrDefault = lastRunStartedAt.GetValueOrDefault();
				if (!routine.LastRunFinishedAt.HasValue)
				{
					isRunning = (((now - valueOrDefault).TotalSeconds < 7200.0) ? 1 : 0);
					goto IL_0068;
				}
			}
			isRunning = 0;
			goto IL_0068;
			IL_0068:
			return new ClickyInkRoutineControl(id, title, intervalMinutes, isPaused, isPausedAfterRepeatedFailures, (byte)isRunning != 0, routine.NextRunAt, routine.RunCount);
		}).ToList();
	}

	public void SetRoutinePaused(string routineId, bool isPaused, string source)
	{
		agents.Routines.SetPaused(routineId, isPaused, source);
	}

	public void SetRoutineInterval(string routineId, int minutes, string source)
	{
		Action<string, int, string> setInterval = SetInterval;
		if (setInterval != null)
		{
			setInterval(routineId, minutes, source);
		}
		else
		{
			agents.Routines.SetInterval(routineId, minutes, source);
		}
	}

	public bool RunRoutineNow(string routineId)
	{
		agents.Routines.RunNowAsync(routineId);
		return true;
	}

	public void SendToClicky(string slug, string message, string launchContext)
	{
		agents.LaunchAsync(new AgentLaunchRequest
		{
			Prompt = message,
			ClickySlug = slug,
			LaunchContext = launchContext,
			LaunchSource = "text",
			UsageContext = "agent_chat"
		});
	}

	public void SendToThread(string threadId, string message, string launchContext)
	{
		Action<string, string, string> sendOnThread = SendOnThread;
		if (sendOnThread != null)
		{
			sendOnThread(threadId, message, launchContext);
		}
		else
		{
			Log.Warn("ink", "no job thread route yet for " + threadId);
		}
	}

	public void OpenJobArtifact(string target, string jobId)
	{
		Action<string, string> openJobArtifactHandler = OpenJobArtifactHandler;
		if (openJobArtifactHandler != null)
		{
			openJobArtifactHandler(target, jobId);
		}
		else if (!OpenInViewerIfSupported(target, null, null))
		{
			ClickyInkNoteBridge.OpenInItsApp(target);
		}
	}

	public string? BoundThreadId(string slug)
	{
		return agents.FindClicky(slug)?.ThreadId;
	}

	public bool OpenInViewerIfSupported(string target, string? slug, string? threadId)
	{
		return OpenInViewer?.Invoke(target, slug, threadId) ?? false;
	}
}
