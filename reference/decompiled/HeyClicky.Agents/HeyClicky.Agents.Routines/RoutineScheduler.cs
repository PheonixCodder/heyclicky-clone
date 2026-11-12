using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Agents.Routines;

public sealed class RoutineScheduler : IDisposable
{
	public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30L);

	private static readonly TimeSpan StaleRunAfter = TimeSpan.FromHours(2);

	private static readonly TimeSpan BlockedRetryDelay = TimeSpan.FromSeconds(60L);

	private static readonly TimeSpan OverdueSettleDelay = TimeSpan.FromMinutes(3L);

	private const string InterruptedByDisconnect = "Codex disconnected while background agent tasks were still running.";

	private const string InterruptedByShutdown = "HeyClicky stopped before the agent task finished.";

	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	private readonly object gate = new object();

	private readonly object moveGate = new object();

	private readonly string path;

	private readonly IRoutineHost host;

	private List<ClickyRoutine> routines;

	private Timer? timer;

	private int evaluating;

	public Func<DateTimeOffset?>? AgentAllowanceResetsAt { get; set; }

	public IReadOnlyList<ClickyRoutine> Routines
	{
		get
		{
			lock (gate)
			{
				return routines.ToList();
			}
		}
	}

	public RoutineWakeGate Wake { get; } = new RoutineWakeGate();

	public static TimeSpan RunStaleAfter => StaleRunAfter;

	public event EventHandler? Changed;

	public event Action<string, string?, IReadOnlyDictionary<string, object?>>? ProductEvent;

	private void Track(string productEvent, string? clickySlug, Dictionary<string, object?> properties)
	{
		try
		{
			this.ProductEvent?.Invoke(productEvent, clickySlug, properties);
		}
		catch (Exception ex)
		{
			Log.Warn("Routines", "analytics " + productEvent + ": " + ex.Message);
		}
	}

	private static int Minutes(double intervalSeconds)
	{
		return (int)(intervalSeconds / 60.0);
	}

	internal RoutineScheduler(string stateDirectory, IRoutineHost host)
	{
		Directory.CreateDirectory(stateDirectory);
		path = Path.Combine(stateDirectory, "routines.json");
		this.host = host;
		routines = Load();
	}

	private List<ClickyRoutine> Load()
	{
		try
		{
			if (File.Exists(path))
			{
				return JsonSerializer.Deserialize<List<ClickyRoutine>>(File.ReadAllText(path), Json) ?? new List<ClickyRoutine>();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Routines", "routines.json unreadable: " + ex.Message);
		}
		return new List<ClickyRoutine>();
	}

	private void SaveLocked()
	{
		string sourceFileName = path + ".tmp";
		File.WriteAllText(sourceFileName, JsonSerializer.Serialize(routines, Json));
		File.Move(sourceFileName, path, overwrite: true);
		RoutineKeepAwake.Update(routines.Any((ClickyRoutine r) => !r.IsPaused));
	}

	private void Mutate(string id, Action<ClickyRoutine> change)
	{
		lock (gate)
		{
			ClickyRoutine clickyRoutine = routines.FirstOrDefault((ClickyRoutine x) => x.Id == id);
			if (clickyRoutine == null)
			{
				return;
			}
			change(clickyRoutine);
			SaveLocked();
		}
		this.Changed?.Invoke(this, EventArgs.Empty);
	}

	public IReadOnlyList<ClickyRoutine> ForClicky(string slug)
	{
		lock (gate)
		{
			return (from r in routines
				where r.JobId == null && r.ClickySlug == slug
				orderby r.CreatedAt
				select r).ToList();
		}
	}

	public IReadOnlyList<ClickyRoutine> ForJob(string jobId)
	{
		lock (gate)
		{
			return (from r in routines
				where r.JobId == jobId
				orderby r.CreatedAt
				select r).ToList();
		}
	}

	public ISet<string> JobIdsWithRoutines()
	{
		lock (gate)
		{
			return routines.Select((ClickyRoutine r) => r.JobId).OfType<string>().ToHashSet();
		}
	}

	public void RemoveAllJobRoutinesForSignOut()
	{
		lock (gate)
		{
			if (!routines.Any((ClickyRoutine r) => r.JobId != null))
			{
				return;
			}
			routines.RemoveAll((ClickyRoutine r) => r.JobId != null);
			SaveLocked();
		}
		this.Changed?.Invoke(this, EventArgs.Empty);
	}

	private bool IsOwnerBusy(ClickyRoutine r)
	{
		string jobId = r.JobId;
		if (jobId == null)
		{
			return host.IsClickyBusy(r.ClickySlug);
		}
		return host.IsJobBusy(jobId);
	}

	public void Start()
	{
		if (timer == null)
		{
			SettleOverdue(DateTimeOffset.Now);
			timer = new Timer(delegate(object? _)
			{
				_ = EvaluateAsync();
			}, null, TimeSpan.FromSeconds(5L), PollInterval);
			Log.Info("Routines", $"scheduler started with {Routines.Count} routine(s)");
		}
	}

	public void Stop()
	{
		RoutineKeepAwake.Update(hasActiveRoutines: false);
		timer?.Dispose();
		timer = null;
	}

	public void SettleOverdue(DateTimeOffset now)
	{
		lock (gate)
		{
			bool flag = false;
			foreach (ClickyRoutine item in routines.Where((ClickyRoutine r) => !r.IsPaused && r.NextRunAt < now))
			{
				item.NextRunAt = now + OverdueSettleDelay;
				flag = true;
			}
			if (flag)
			{
				SaveLocked();
			}
		}
	}

	public void MoveClickyRoutinesOntoJobs()
	{
		lock (moveGate)
		{
			MoveClickyRoutinesOntoJobsLocked();
		}
	}

	private void MoveClickyRoutinesOntoJobsLocked()
	{
		List<ClickyRoutine> list;
		lock (gate)
		{
			list = routines.Where((ClickyRoutine r) => r.IsSavedOnAClicky).ToList();
		}
		bool flag = false;
		foreach (ClickyRoutine routine in list)
		{
			if (!host.ClickyExists(routine.ClickySlug) || host.IsClickyBusy(routine.ClickySlug))
			{
				continue;
			}
			string text;
			try
			{
				text = host.CreateJobForMovedRoutine(routine);
			}
			catch (Exception ex)
			{
				Log.Warn("Routines", $"routine {routine.Id} not moved off Clicky {routine.ClickySlug}: {ex.Message}");
				continue;
			}
			if (text == null)
			{
				continue;
			}
			lock (gate)
			{
				ClickyRoutine clickyRoutine = routines.FirstOrDefault((ClickyRoutine r) => r.Id == routine.Id);
				if (clickyRoutine == null || clickyRoutine.JobId != null)
				{
					continue;
				}
				clickyRoutine.JobId = text;
				SaveLocked();
				goto IL_019d;
			}
			IL_019d:
			flag = true;
			host.RetireClickysNoteForMovedRoutineJob(text, onlyWhenLeft: false);
			Log.Info("Routines", $"routine {routine.Id} moved from Clicky {routine.ClickySlug} onto job {text}");
			Track("clicky_routine_updated", null, new Dictionary<string, object>
			{
				["routine_id"] = routine.Id,
				["is_job"] = true,
				["change"] = "moved_from_clicky"
			});
		}
		if (flag)
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
		List<string> list2;
		lock (gate)
		{
			list2 = (from r in routines
				where r.JobId != null && r.ClickySlug.Length > 0
				select r.JobId).ToList();
		}
		foreach (string item in list2)
		{
			host.RetireClickysNoteForMovedRoutineJob(item, onlyWhenLeft: true);
		}
	}

	public async Task EvaluateAsync()
	{
		if (Interlocked.Exchange(ref evaluating, 1) == 1)
		{
			return;
		}
		try
		{
			bool hasActiveRoutines;
			lock (gate)
			{
				hasActiveRoutines = routines.Any((ClickyRoutine r) => !r.IsPaused);
			}
			RoutineKeepAwake.Update(hasActiveRoutines);
			if (!host.IsSignedIn)
			{
				return;
			}
			MoveClickyRoutinesOntoJobs();
			host.BackfillWorkReportsIfNeeded();
			DateTimeOffset now = DateTimeOffset.Now;
			ReconcileUnfinishedRuns(now);
			List<ClickyRoutine> list;
			lock (gate)
			{
				list = routines.Where((ClickyRoutine r) => !r.IsPaused && r.NextRunAt <= now).ToList();
			}
			if (list.Count == 0)
			{
				return;
			}
			if (!NetworkInterface.GetIsNetworkAvailable())
			{
				MarkWaitingForNetwork(list, now, "offline");
				return;
			}
			if (Wake.IsInDarkWake)
			{
				MarkWaitingForNetwork(list, now, "display off after sleep");
				return;
			}
			if (Wake.ShouldHoldForFullWake(now, OverdueSettleDelay))
			{
				Log.Info("Routines", $"waking: holding {list.Count} due routine(s) until the wake settle runs");
				return;
			}
			foreach (ClickyRoutine item in list)
			{
				await LaunchRunAsync(item.Id, now, scheduled: true).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Routines", "evaluation failed", ex);
		}
		finally
		{
			Volatile.Write(ref evaluating, 0);
		}
	}

	private void MarkWaitingForNetwork(List<ClickyRoutine> due, DateTimeOffset now, string reason)
	{
		List<ClickyRoutine> list = due.Where((ClickyRoutine r) => !r.WaitingForNetworkSince.HasValue).ToList();
		if (list.Count == 0)
		{
			return;
		}
		foreach (ClickyRoutine item in list)
		{
			Mutate(item.Id, delegate(ClickyRoutine x)
			{
				x.NoteWaitingForNetwork(now);
			});
		}
		Log.Info("Routines", $"{reason}: {list.Count} due routine(s) now waiting for the internet");
	}

	public void NoteSystemSleeping()
	{
		Wake.NoteSleeping();
		Log.Info("Routines", "going to sleep: due routines hold until the display is back");
	}

	public void NoteDisplayPower(bool isOn)
	{
		if (Wake.NoteDisplay(isOn))
		{
			HandleFullWake("display on");
		}
	}

	public void HandleFullWake(string source)
	{
		if (Wake.ConsumeFullWake())
		{
			Log.Info("Routines", source + ": settling overdue routines, then evaluating");
			SettleOverdue(DateTimeOffset.Now);
		}
		else
		{
			Log.Info("Routines", source + ": wake already settled, evaluating");
		}
		if (timer != null)
		{
			EvaluateAsync();
		}
	}

	private void ReconcileUnfinishedRuns(DateTimeOffset now)
	{
		List<ClickyRoutine> list;
		lock (gate)
		{
			list = routines.Where(delegate(ClickyRoutine r)
			{
				DateTimeOffset? lastRunStartedAt = r.LastRunStartedAt;
				if (lastRunStartedAt.HasValue)
				{
					DateTimeOffset valueOrDefault = lastRunStartedAt.GetValueOrDefault();
					if (!r.LastRunFinishedAt.HasValue)
					{
						return now - valueOrDefault > StaleRunAfter;
					}
				}
				return false;
			}).ToList();
		}
		foreach (ClickyRoutine item in list)
		{
			if (!IsOwnerBusy(item))
			{
				Mutate(item.Id, delegate(ClickyRoutine x)
				{
					x.RecordOutcome(now, "The previous run ended before its result was saved.", failed: true, shouldRetry: true);
				});
			}
		}
	}

	private async Task<bool> LaunchRunAsync(string routineId, DateTimeOffset now, bool scheduled)
	{
		ClickyRoutine routine;
		lock (gate)
		{
			routine = routines.FirstOrDefault((ClickyRoutine r) => r.Id == routineId);
		}
		if (routine == null)
		{
			return false;
		}
		string jobId = routine.JobId;
		if (jobId != null)
		{
			return await LaunchJobRunAsync(routine, jobId, now, scheduled).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!host.ClickyExists(routine.ClickySlug))
		{
			Log.Info("Routines", $"routine {routineId} paused: its Clicky {routine.ClickySlug} is gone");
			Mutate(routineId, delegate(ClickyRoutine r)
			{
				r.IsPaused = true;
			});
			return false;
		}
		if (host.IsClickyBusy(routine.ClickySlug) || host.IsClickyAwaitingExtraUsage(routine.ClickySlug))
		{
			Mutate(routineId, delegate(ClickyRoutine r)
			{
				r.NextRunAt = now + BlockedRetryDelay;
			});
			return false;
		}
		int runNumber = routine.RunCount + 1;
		string prompt = RunPrompt(routine, runNumber, now);
		string launchContext = $"This turn is run #{runNumber} of the routine \"{routine.Title}\" ({routine.CadenceLabel.ToLowerInvariant()}) that the user set up, so HeyClicky is launching it automatically with the user's standing instructions. " + ((runNumber > 1) ? "Earlier runs of this routine are in this same conversation above; build on them and report what is new rather than repeating them. " : "");
		string taskId = null;
		try
		{
			taskId = await host.LaunchRoutineRunAsync(routine, runNumber, prompt, launchContext).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Error("Routines", "routine " + routineId + " launch failed", ex);
		}
		if (taskId != null)
		{
			Mutate(routineId, delegate(ClickyRoutine r)
			{
				r.RecordDispatch(now, taskId);
			});
			Log.Info("Routines", $"routine {routineId} run #{runNumber} dispatched to {routine.ClickySlug}");
			Track("clicky_routine_run_dispatched", routine.ClickySlug, new Dictionary<string, object>
			{
				["routine_id"] = routineId,
				["run_number"] = runNumber,
				["trigger"] = (scheduled ? "scheduled" : "manual")
			});
			return true;
		}
		Mutate(routineId, delegate(ClickyRoutine r)
		{
			r.ScheduleRetry(now);
		});
		return false;
	}

	private async Task<bool> LaunchJobRunAsync(ClickyRoutine routine, string jobId, DateTimeOffset now, bool scheduled)
	{
		if (!host.JobExists(jobId))
		{
			Log.Info("Routines", $"routine {routine.Id} paused — its job {jobId} is gone");
			Mutate(routine.Id, delegate(ClickyRoutine r)
			{
				r.IsPaused = true;
			});
			return false;
		}
		if (host.IsJobBusy(jobId))
		{
			Log.Info("Routines", $"routine {routine.Id} skipped — job {jobId} is busy; retrying next tick");
			Mutate(routine.Id, delegate(ClickyRoutine r)
			{
				r.NextRunAt = now + BlockedRetryDelay;
			});
			return false;
		}
		if (host.IsJobAwaitingExtraUsage(jobId))
		{
			Mutate(routine.Id, delegate(ClickyRoutine r)
			{
				r.NextRunAt = now + BlockedRetryDelay;
			});
			return false;
		}
		int runNumber = routine.RunCount + 1;
		string prompt = RunPrompt(routine, runNumber, now);
		string taskId = null;
		try
		{
			taskId = await host.LaunchJobRoutineRunAsync(routine, runNumber, prompt, scheduled).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Error("Routines", "routine " + routine.Id + " launch failed", ex);
		}
		if (taskId != null)
		{
			Track("clicky_routine_run_dispatched", null, new Dictionary<string, object>
			{
				["routine_id"] = routine.Id,
				["run_number"] = runNumber,
				["is_job"] = true,
				["trigger"] = (scheduled ? "scheduled" : "manual")
			});
			Mutate(routine.Id, delegate(ClickyRoutine r)
			{
				r.RecordDispatch(now, taskId);
			});
			Log.Info("Routines", $"routine {routine.Id} run #{runNumber} dispatched to job {jobId}");
			return true;
		}
		Log.Info("Routines", $"routine {routine.Id} launch refused for job {jobId} (scheduled={scheduled}, signedIn={host.IsSignedIn})");
		Mutate(routine.Id, delegate(ClickyRoutine r)
		{
			r.ScheduleRetry(now);
		});
		return false;
	}

	private static string RunPrompt(ClickyRoutine r, int runNumber, DateTimeOffset now)
	{
		List<string> list = new List<string>
		{
			ClickyRoutine.PromptHeaderLine(r.Title, runNumber, r.CadenceLabel, now),
			"",
			r.TaskPrompt,
			""
		};
		DateTimeOffset? lastRunStartedAt = r.LastRunStartedAt;
		if (lastRunStartedAt.HasValue)
		{
			DateTimeOffset valueOrDefault = lastRunStartedAt.GetValueOrDefault();
			if (runNumber > 1)
			{
				list.Add($"This is a repeating routine; the previous run started {valueOrDefault:MMM d, yyyy h:mm tt} and its answer is above in this conversation. Report what is NEW or changed since then, keep it short, and say so in one line if nothing changed.");
				goto IL_00a9;
			}
		}
		list.Add("This is the first run of a repeating routine. Do the task fully this time; later runs will report only what changed.");
		goto IL_00a9;
		IL_00a9:
		if (r.PendingRetryAt.HasValue && r.LastRunFailed)
		{
			list.Add("The previous attempt failed or was interrupted. Check the conversation and existing results first, and finish only what is still missing. Do not repeat any message, post, upload, or other external action that already succeeded. If an external action's outcome is uncertain, verify it before trying again.");
		}
		list.Add("Post the answer in this chat as your reply. Do not create files unless the task itself asks for one.");
		return string.Join("\n", list);
	}

	public ClickyRoutine CreateRoutine(string clickySlug, string title, string taskPrompt, int intervalMinutes, bool runImmediately, string? jobId = null)
	{
		ClickyRoutine clickyRoutine = ClickyRoutine.Create(clickySlug, title, ClickyRoutine.TaskPromptStrippingLeadingCadence(taskPrompt), intervalMinutes, jobId);
		if (!runImmediately)
		{
			clickyRoutine.NextRunAt = DateTimeOffset.Now.AddSeconds(clickyRoutine.IntervalSeconds);
		}
		lock (gate)
		{
			routines.Add(clickyRoutine);
			SaveLocked();
		}
		this.Changed?.Invoke(this, EventArgs.Empty);
		TrackCreated(clickyRoutine, runImmediately);
		Log.Info("Routines", $"created routine {clickyRoutine.Id} for {clickyRoutine.OwnerLogName} {clickyRoutine.CadenceLabel}");
		if (runImmediately)
		{
			EvaluateAsync();
		}
		return clickyRoutine;
	}

	private void TrackCreated(ClickyRoutine routine, bool runImmediately)
	{
		Track("clicky_routine_created", routine.OwningClickySlug, new Dictionary<string, object>
		{
			["routine_id"] = routine.Id,
			["interval_minutes"] = Minutes(routine.IntervalSeconds),
			["run_immediately"] = runImmediately,
			["is_job"] = routine.JobId != null
		});
	}

	public void SetInterval(string routineId, int minutes, string source = "profile")
	{
		ClickyRoutine clickyRoutine;
		lock (gate)
		{
			clickyRoutine = routines.FirstOrDefault((ClickyRoutine r) => r.Id == routineId);
		}
		if (clickyRoutine == null)
		{
			return;
		}
		int clamped = Math.Clamp(minutes, 2, 10080);
		if (clamped == clickyRoutine.IntervalMinutes)
		{
			return;
		}
		Track("clicky_routine_updated", clickyRoutine.OwningClickySlug, new Dictionary<string, object>
		{
			["routine_id"] = routineId,
			["interval_minutes"] = clamped,
			["previous_interval_minutes"] = clickyRoutine.IntervalMinutes,
			["source"] = source
		});
		Mutate(routineId, delegate(ClickyRoutine r)
		{
			r.IntervalSeconds = clamped * 60;
			if (!r.HasSetTime)
			{
				r.ScheduledMinuteOfDay = null;
				r.ScheduledWeekday = null;
			}
			r.ScheduledWeekdays = null;
			RescheduleAfterCadenceChange(r);
		});
	}

	public void SetSchedule(string routineId, int minutes, int? minuteOfDay, IReadOnlyList<int>? weekdays, string source = "settings")
	{
		ClickyRoutine clickyRoutine;
		lock (gate)
		{
			clickyRoutine = routines.FirstOrDefault((ClickyRoutine r) => r.Id == routineId);
		}
		if (clickyRoutine == null)
		{
			return;
		}
		ClickyRoutine.Schedule schedule = ClickyRoutine.NormalizedSchedule(minutes, minuteOfDay, weekdays);
		if (schedule.Minutes != clickyRoutine.IntervalMinutes || schedule.MinuteOfDay != clickyRoutine.ScheduledMinuteOfDay || schedule.Weekday != clickyRoutine.ScheduledWeekday || !SameDays(schedule.Weekdays, clickyRoutine.ScheduledWeekdays))
		{
			Track("clicky_routine_updated", clickyRoutine.OwningClickySlug, new Dictionary<string, object>
			{
				["routine_id"] = routineId,
				["interval_minutes"] = schedule.Minutes,
				["previous_interval_minutes"] = clickyRoutine.IntervalMinutes,
				["has_set_time"] = schedule.MinuteOfDay.HasValue,
				["day_count"] = schedule.Weekdays?.Count ?? (schedule.Weekday.HasValue ? 1 : 0),
				["source"] = source
			});
			Mutate(routineId, delegate(ClickyRoutine r)
			{
				r.IntervalSeconds = schedule.Minutes * 60;
				r.ScheduledMinuteOfDay = schedule.MinuteOfDay;
				r.ScheduledWeekday = schedule.Weekday;
				r.ScheduledWeekdays = schedule.Weekdays?.ToList();
				RescheduleAfterCadenceChange(r);
			});
		}
	}

	private static bool SameDays(IReadOnlyList<int>? a, IReadOnlyList<int>? b)
	{
		if (a != null && b != null)
		{
			return a.SequenceEqual(b);
		}
		if (a == null)
		{
			return b == null;
		}
		return false;
	}

	private static void RescheduleAfterCadenceChange(ClickyRoutine routine)
	{
		DateTimeOffset now = DateTimeOffset.Now;
		DateTimeOffset? dateTimeOffset = routine.NextSetTime(now.AddSeconds(60.0));
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			routine.NextRunAt = valueOrDefault;
		}
		else
		{
			DateTimeOffset dateTimeOffset2 = (routine.LastRunStartedAt ?? now).AddSeconds(routine.IntervalSeconds);
			routine.NextRunAt = ((dateTimeOffset2 > now.AddSeconds(60.0)) ? dateTimeOffset2 : now.AddSeconds(60.0));
		}
		routine.PendingRetryAt = null;
	}

	public void UpdateDetails(string routineId, string title, string taskPrompt, string source = "settings")
	{
		string trimmedTitle = title.Trim();
		string trimmedTask = taskPrompt.Trim();
		ClickyRoutine clickyRoutine;
		lock (gate)
		{
			clickyRoutine = routines.FirstOrDefault((ClickyRoutine r) => r.Id == routineId);
		}
		if (clickyRoutine != null && trimmedTitle.Length != 0 && trimmedTask.Length != 0 && (!(trimmedTitle == clickyRoutine.Title) || !(trimmedTask == clickyRoutine.TaskPrompt)))
		{
			Track("clicky_routine_updated", clickyRoutine.OwningClickySlug, new Dictionary<string, object>
			{
				["routine_id"] = routineId,
				["changed_title"] = trimmedTitle != clickyRoutine.Title,
				["changed_task"] = trimmedTask != clickyRoutine.TaskPrompt,
				["source"] = source
			});
			Mutate(routineId, delegate(ClickyRoutine r)
			{
				r.Title = trimmedTitle;
				r.TaskPrompt = trimmedTask;
			});
		}
	}

	public ClickyRoutine RegisterApprovedSuggestion(string clickySlug, string title, string taskPrompt, int intervalMinutes, string? taskId, string? jobId = null)
	{
		DateTimeOffset now = DateTimeOffset.Now;
		double seconds = Math.Clamp(intervalMinutes, 2, 10080) * 60;
		if (jobId != null)
		{
			ClickyRoutine clickyRoutine = ForJob(jobId).FirstOrDefault();
			if (clickyRoutine != null)
			{
				Delete(clickyRoutine.Id, "suggestion_restarted");
			}
		}
		ClickyRoutine clickyRoutine2 = ((jobId == null) ? ForClicky(clickySlug).FirstOrDefault((ClickyRoutine r) => string.Equals(r.Title, title, StringComparison.OrdinalIgnoreCase)) : null);
		if (clickyRoutine2 != null)
		{
			Mutate(clickyRoutine2.Id, delegate(ClickyRoutine r)
			{
				r.TaskPrompt = taskPrompt;
				r.IntervalSeconds = seconds;
				r.IsPaused = false;
				r.RunCount++;
				r.LastRunStartedAt = now;
				r.LastRunFinishedAt = null;
				r.LastRunFailed = false;
				r.PendingRetryAt = null;
				r.LastRunTaskId = taskId;
				r.NextRunAt = r.RegularNextRunAt(now);
			});
			Log.Info("Routines", "approved suggestion updated routine " + clickyRoutine2.Id + " on " + clickyRoutine2.OwnerLogName);
			return clickyRoutine2;
		}
		ClickyRoutine clickyRoutine3 = ClickyRoutine.Create(clickySlug, title, taskPrompt, intervalMinutes, jobId);
		clickyRoutine3.RunCount = 1;
		clickyRoutine3.LastRunStartedAt = now;
		clickyRoutine3.LastRunTaskId = taskId;
		clickyRoutine3.NextRunAt = now.AddSeconds(clickyRoutine3.IntervalSeconds);
		lock (gate)
		{
			routines.Add(clickyRoutine3);
			SaveLocked();
		}
		this.Changed?.Invoke(this, EventArgs.Empty);
		TrackCreated(clickyRoutine3, runImmediately: false);
		Log.Info("Routines", $"approved suggestion registered routine {clickyRoutine3.Id} on {clickyRoutine3.OwnerLogName} ({clickyRoutine3.CadenceLabel})");
		return clickyRoutine3;
	}

	public void SetPaused(string routineId, bool paused, string source = "profile")
	{
		ClickyRoutine clickyRoutine;
		lock (gate)
		{
			clickyRoutine = routines.FirstOrDefault((ClickyRoutine r) => r.Id == routineId);
		}
		if (clickyRoutine != null && clickyRoutine.IsPaused != paused)
		{
			Track("clicky_routine_pause_changed", clickyRoutine.OwningClickySlug, new Dictionary<string, object>
			{
				["routine_id"] = routineId,
				["paused"] = paused,
				["source"] = source
			});
		}
		Mutate(routineId, delegate(ClickyRoutine r)
		{
			r.IsPaused = paused;
			if (!paused)
			{
				r.ConsecutiveFailedRunCount = 0;
				if (r.NextRunAt < DateTimeOffset.Now)
				{
					r.NextRunAt = DateTimeOffset.Now;
				}
			}
		});
	}

	public void Delete(string routineId, string source = "profile")
	{
		ClickyRoutine clickyRoutine;
		lock (gate)
		{
			clickyRoutine = routines.FirstOrDefault((ClickyRoutine r) => r.Id == routineId);
			routines.RemoveAll((ClickyRoutine r) => r.Id == routineId);
			SaveLocked();
		}
		if (clickyRoutine != null)
		{
			Track("clicky_routine_deleted", clickyRoutine.OwningClickySlug, new Dictionary<string, object>
			{
				["routine_id"] = routineId,
				["source"] = source
			});
		}
		this.Changed?.Invoke(this, EventArgs.Empty);
	}

	public async Task<bool> RunNowAsync(string routineId)
	{
		bool flag = await LaunchRunAsync(routineId, DateTimeOffset.Now, scheduled: false).ConfigureAwait(continueOnCapturedContext: false);
		string clickySlug;
		lock (gate)
		{
			clickySlug = routines.FirstOrDefault((ClickyRoutine r) => r.Id == routineId)?.OwningClickySlug;
		}
		Track("clicky_routine_run_requested", clickySlug, new Dictionary<string, object>
		{
			["routine_id"] = routineId,
			["dispatched"] = flag
		});
		return flag;
	}

	public IReadOnlyList<string> PromptLines(string slug)
	{
		return RoutinePromptLines(ForClicky(slug));
	}

	public IReadOnlyList<string> PromptLinesForJob(string jobId)
	{
		return RoutinePromptLines(ForJob(jobId));
	}

	private static IReadOnlyList<string> RoutinePromptLines(IReadOnlyList<ClickyRoutine> routines)
	{
		return routines.Select(delegate(ClickyRoutine r)
		{
			string value = string.Join(' ', from l in r.TaskPrompt.Split('\n')
				select l.Trim() into l
				where l.Length > 0
				select l);
			return $"- title: \"{r.Title}\" · {r.CadenceLabel.ToLowerInvariant()} · {(r.IsPaused ? "paused" : "active")} · ran {r.RunCount}× · task: {value}";
		}).ToList();
	}

	public string? JobRoutineLine(string jobId)
	{
		ClickyRoutine clickyRoutine = ForJob(jobId).FirstOrDefault();
		if (clickyRoutine == null)
		{
			return null;
		}
		return "repeats (routine_id " + clickyRoutine.Id + ")";
	}

	internal void NoteTaskEnded(string? clickySlug, string? jobId, string taskId, string? summary, bool failed, bool interrupted, bool awaitingExtraUsage, bool transientConnectivityFailure = false, bool hitAgentMessageLimit = false)
	{
		if (clickySlug == null && jobId == null)
		{
			return;
		}
		ClickyRoutine clickyRoutine;
		lock (gate)
		{
			clickyRoutine = routines.FirstOrDefault(delegate(ClickyRoutine r)
			{
				bool num;
				if (jobId == null)
				{
					if (r.JobId != null)
					{
						goto IL_0075;
					}
					num = r.ClickySlug == clickySlug;
				}
				else
				{
					num = r.JobId == jobId;
				}
				if (num && r.LastRunStartedAt.HasValue && !r.LastRunFinishedAt.HasValue)
				{
					if (r.LastRunTaskId != null)
					{
						return r.LastRunTaskId == taskId;
					}
					return true;
				}
				goto IL_0075;
				IL_0075:
				return false;
			});
		}
		if (clickyRoutine == null)
		{
			return;
		}
		string owningClickySlug = clickyRoutine.OwningClickySlug;
		Track("clicky_routine_run_ended", owningClickySlug, new Dictionary<string, object>
		{
			["routine_id"] = clickyRoutine.Id,
			["run_number"] = clickyRoutine.RunCount,
			["outcome"] = (interrupted ? "interrupted" : (failed ? "failed" : "completed")),
			["duration_seconds"] = Math.Max(0.0, (DateTimeOffset.Now - (clickyRoutine.LastRunStartedAt ?? DateTimeOffset.Now)).TotalSeconds)
		});
		string text = summary;
		bool flag = ((text == "Codex disconnected while background agent tasks were still running." || text == "HeyClicky stopped before the agent task finished.") ? true : false);
		bool flag2 = flag;
		bool shouldRetry = !awaitingExtraUsage && (!interrupted || flag2);
		bool paused = false;
		int failedInARow = 0;
		if (hitAgentMessageLimit)
		{
			DateTimeOffset? resetsAt = AgentAllowanceResetsAt?.Invoke();
			DateTimeOffset next = default(DateTimeOffset);
			Mutate(clickyRoutine.Id, delegate(ClickyRoutine r)
			{
				r.RecordAgentMessageLimit(DateTimeOffset.Now, summary, resetsAt);
				next = r.NextRunAt;
			});
			Log.Info("Routines", $"routine {clickyRoutine.Id} out of agent messages; next run {next:yyyy-MM-dd HH:mm} (allowance resets {resetsAt?.ToString("yyyy-MM-dd HH:mm") ?? "unknown"})");
			return;
		}
		Mutate(clickyRoutine.Id, delegate(ClickyRoutine r)
		{
			paused = r.RecordOutcome(DateTimeOffset.Now, summary, failed, shouldRetry, !transientConnectivityFailure);
			failedInARow = r.ConsecutiveFailedRunCount;
		});
		Log.Info("Routines", $"routine {clickyRoutine.Id} run ended failed={failed}{(paused ? " (paused after repeated failures)" : "")}");
		if (paused)
		{
			Track("clicky_routine_pause_changed", owningClickySlug, new Dictionary<string, object>
			{
				["routine_id"] = clickyRoutine.Id,
				["paused"] = true,
				["source"] = "repeated_failures",
				["failed_runs_in_a_row"] = failedInARow
			});
		}
	}

	public void Dispose()
	{
		Stop();
	}
}
