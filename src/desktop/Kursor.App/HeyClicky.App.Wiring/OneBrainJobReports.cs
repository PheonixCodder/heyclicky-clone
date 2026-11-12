using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;

namespace HeyClicky.App.Wiring;

public sealed class OneBrainJobReports
{
	private sealed class Report(string turnKey, string taskId, string? threadId, string name)
	{
		public bool SpeaksAloud;

		public bool RaisesHeadsUp;

		public bool PlaysChime;

		public string TurnKey { get; } = turnKey;

		public string TaskId { get; } = taskId;

		public string? ThreadId { get; } = threadId;

		public string Name { get; } = name;
	}

	public static readonly TimeSpan Gather = TimeSpan.FromSeconds(1.2);

	public static readonly TimeSpan QuietWait = TimeSpan.FromSeconds(120L);

	public static readonly TimeSpan PcStepWait = TimeSpan.FromSeconds(300L);

	public static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(25L);

	private readonly AgentService agents;

	private readonly CompanionSession companion;

	private readonly object gate = new object();

	private List<Report> waiting = new List<Report>();

	private List<Report> inFlight = new List<Report>();

	private Task? flushTask;

	private readonly Dictionary<string, IReadOnlyList<string>> linesByTurn = new Dictionary<string, IReadOnlyList<string>>();

	private readonly Dictionary<string, IReadOnlyList<string>> linesByThread = new Dictionary<string, IReadOnlyList<string>>();

	private readonly HashSet<string> spokenTurns = new HashSet<string>();

	private readonly List<string> presentedTurnOrder = new List<string>();

	private readonly HashSet<string> presentingThreadIds = new HashSet<string>();

	private readonly HashSet<string> presentedThreadIds = new HashSet<string>();

	private readonly HashSet<string> cardsShownThisTurn = new HashSet<string>();

	private int cardBlocksShownThisTurn;

	private int cardErrorsThisTurn;

	public Func<string, Task>? Speak { get; set; }

	public Action? PlayDoneChime { get; set; }

	public Func<bool>? IsPcStepMomentInProgress { get; set; }

	public static bool IsOn => true;

	private bool IsPresentingAny
	{
		get
		{
			lock (gate)
			{
				return presentingThreadIds.Count > 0;
			}
		}
	}

	public event Action<string>? HeadsUpReady;

	public event Action<IReadOnlyList<string>, IReadOnlyList<string>>? ReportPresented;

	public event Action? PresentingChanged;

	public OneBrainJobReports(AgentService agents, CompanionSession companion)
	{
		this.agents = agents;
		this.companion = companion;
	}

	public static string TurnKey(string taskId, string? turnId)
	{
		return taskId + ":" + turnId;
	}

	public bool IsPresenting(string? threadId)
	{
		if (threadId == null)
		{
			return false;
		}
		lock (gate)
		{
			return presentingThreadIds.Contains(threadId);
		}
	}

	public bool IsFinalizingResult(ClickyJob job)
	{
		if (!IsPresentingAny || agents.RailPhase(job) != ClickyJobRailPhase.Done)
		{
			return false;
		}
		string text = agents.LiveTaskId(job.Id);
		string text2 = ((text == null) ? null : agents.GetTaskDetail(text)?.Snapshot.ThreadId);
		return IsPresenting(text2 ?? job.ThreadId);
	}

	public bool IsFinalizingResult(string clickySlug)
	{
		if (IsPresentingAny && agents.LiveStatusForClicky(clickySlug).Phase == ClickyAppLiveStatus.Phases.Finished)
		{
			return IsPresenting(agents.ClickyAppLiveThreadId(clickySlug));
		}
		return false;
	}

	public bool HasPresented(string threadId)
	{
		lock (gate)
		{
			return presentedThreadIds.Contains(threadId);
		}
	}

	public IReadOnlyList<string> Lines(string threadId)
	{
		lock (gate)
		{
			return linesByThread.GetValueOrDefault(threadId) ?? Array.Empty<string>();
		}
	}

	public bool Present(string turnKey, string taskId, string? threadId, string name)
	{
		if (!IsOn)
		{
			return false;
		}
		lock (gate)
		{
			if (presentedTurnOrder.Contains(turnKey))
			{
				return true;
			}
			presentedTurnOrder.Add(turnKey);
			if (presentedTurnOrder.Count > 40)
			{
				foreach (string item in presentedTurnOrder.Take(presentedTurnOrder.Count - 40))
				{
					linesByTurn.Remove(item);
					spokenTurns.Remove(item);
				}
				presentedTurnOrder.RemoveRange(0, presentedTurnOrder.Count - 40);
			}
			waiting.Add(new Report(turnKey, taskId, threadId, name));
			if (threadId != null)
			{
				presentingThreadIds.Add(threadId);
				presentedThreadIds.Remove(threadId);
			}
			if (flushTask == null)
			{
				flushTask = Task.Run(async delegate
				{
					await Task.Delay(Gather).ConfigureAwait(continueOnCapturedContext: false);
					await FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
				});
			}
		}
		this.PresentingChanged?.Invoke();
		Log.Info("JobReports", $"[one brain] report queued for {name} ({threadId})");
		return true;
	}

	private bool IsKnown(string turnKey)
	{
		lock (gate)
		{
			return presentedTurnOrder.Contains(turnKey);
		}
	}

	public bool SpeakWhenReady(string turnKey)
	{
		if (!IsOn || !IsKnown(turnKey))
		{
			return false;
		}
		IReadOnlyList<string> readOnlyList = null;
		lock (gate)
		{
			Report report = waiting.FirstOrDefault((Report r) => r.TurnKey == turnKey);
			if (report != null)
			{
				report.SpeaksAloud = true;
			}
			else
			{
				Report report2 = inFlight.FirstOrDefault((Report r) => r.TurnKey == turnKey);
				IReadOnlyList<string> value;
				if (report2 != null)
				{
					report2.SpeaksAloud = true;
				}
				else if (linesByTurn.TryGetValue(turnKey, out value) && !spokenTurns.Contains(turnKey) && !UserIsTalking())
				{
					spokenTurns.Add(turnKey);
					readOnlyList = value;
				}
			}
		}
		if (readOnlyList != null)
		{
			Speak?.Invoke(string.Join(" ", readOnlyList));
			ClickyAnalytics.TrackProductEvent("job_report_action", null, null, new JsonObject((JsonNodeOptions?)null)
			{
				["action"] = "spoken_after_presented",
				["line_count"] = readOnlyList.Count
			});
		}
		return true;
	}

	public bool SpeakOnRequest(string? threadId)
	{
		if (!IsOn || threadId == null)
		{
			return false;
		}
		IReadOnlyList<string> valueOrDefault;
		lock (gate)
		{
			valueOrDefault = linesByThread.GetValueOrDefault(threadId);
		}
		if (valueOrDefault == null || valueOrDefault.Count <= 0)
		{
			return false;
		}
		bool flag = !UserIsTalking();
		if (flag)
		{
			Speak?.Invoke(string.Join(" ", valueOrDefault));
		}
		ClickyAnalytics.TrackProductEvent("job_report_action", null, threadId, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "spoken_on_request",
			["spoke"] = flag,
			["line_count"] = valueOrDefault.Count
		});
		return true;
	}

	public bool HoldHeadsUpForCard(string turnKey, bool playsChime = false)
	{
		lock (gate)
		{
			Report report = waiting.FirstOrDefault((Report r) => r.TurnKey == turnKey) ?? inFlight.FirstOrDefault((Report r) => r.TurnKey == turnKey);
			if (report == null)
			{
				return false;
			}
			report.RaisesHeadsUp = true;
			report.PlaysChime |= playsChime;
			return true;
		}
	}

	private bool UserIsTalking()
	{
		if (companion.State == VoiceState.Idle && !companion.IsListening)
		{
			return companion.ActiveTurnCount > 0;
		}
		return true;
	}

	private async Task FlushAsync()
	{
		DateTimeOffset waitedFrom = DateTimeOffset.Now;
		while (true)
		{
			TimeSpan timeSpan = DateTimeOffset.Now - waitedFrom;
			bool num = UserIsTalking() && timeSpan < QuietWait;
			bool flag = (IsPcStepMomentInProgress?.Invoke() ?? false) && timeSpan < PcStepWait;
			if (!num && !flag)
			{
				break;
			}
			await Task.Delay(300).ConfigureAwait(continueOnCapturedContext: false);
		}
		double totalSeconds = (DateTimeOffset.Now - waitedFrom).TotalSeconds;
		List<Report> batch;
		lock (gate)
		{
			inFlight = waiting;
			waiting = new List<Report>();
			flushTask = null;
			batch = inFlight;
			if (batch.Count > 0)
			{
				cardsShownThisTurn.Clear();
				cardBlocksShownThisTurn = 0;
				cardErrorsThisTurn = 0;
			}
		}
		if (batch.Count == 0)
		{
			return;
		}
		long reportStartedAt = Environment.TickCount64;
		int jobCount = batch.Count;
		string reportThreadId = ((jobCount == 1) ? batch[0].ThreadId : null);
		ClickyAnalytics.TrackProductEvent("job_report_action", null, reportThreadId, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "started",
			["job_count"] = jobCount,
			["waited_seconds"] = ClickyAnalytics.RoundedSeconds(totalSeconds)
		});
		string text = string.Join(", ", batch.Select(delegate(Report r)
		{
			string name = r.Name;
			string threadId = r.ThreadId;
			return "\"" + name + "\"" + ((threadId != null) ? (" (job_id: " + agents.OneBrainJobId(threadId) + ")") : "");
		}));
		string fact = ((batch.Count == 1) ? ("[Job ready] The job " + text + " just came back; its report is above. Show its card with show_result, then tell them in one or two short say() lines.") : ("[Job ready] These jobs just came back: " + text + "; their reports are above. Show each one's card with show_result, then tell them, a short say() line for each."));
		IReadOnlyList<string> lines = await RunReportTurnAsync(fact).ConfigureAwait(continueOnCapturedContext: false);
		int attempts = 1;
		if (lines == null)
		{
			Log.Info("JobReports", "[one brain] report turn failed: trying once more");
			ClickyAnalytics.TrackProductEvent("job_report_action", null, reportThreadId, new JsonObject((JsonNodeOptions?)null)
			{
				["action"] = "retried",
				["job_count"] = jobCount
			});
			attempts = 2;
			lines = await RunReportTurnAsync(fact).ConfigureAwait(continueOnCapturedContext: false);
		}
		JsonObject jsonObject;
		lock (gate)
		{
			inFlight = new List<Report>();
			int cardsShown = batch.Select((Report r) => r.ThreadId).OfType<string>().Count(cardsShownThisTurn.Contains);
			jsonObject = ClickyAnalytics.JobReportProperties(batch.Count, attempts, cardsShown, cardBlocksShownThisTurn, cardErrorsThisTurn, lines?.Count ?? 0, batch.Any((Report r) => r.RaisesHeadsUp), batch.Any((Report r) => r.PlaysChime), Environment.TickCount64 - reportStartedAt);
			foreach (Report item in batch.Where((Report r) => r.ThreadId != null))
			{
				presentingThreadIds.Remove(item.ThreadId);
				presentedThreadIds.Add(item.ThreadId);
			}
		}
		this.PresentingChanged?.Invoke();
		if (batch.Any((Report r) => r.PlaysChime))
		{
			PlayDoneChime?.Invoke();
		}
		foreach (Report item2 in batch.Where((Report r) => r.RaisesHeadsUp))
		{
			this.HeadsUpReady?.Invoke(item2.TaskId);
		}
		if (lines == null || lines.Count <= 0)
		{
			Log.Info("JobReports", $"[one brain] report turn said nothing for {batch.Count} job(s): only their status shows");
			jsonObject["action"] = ((lines == null) ? "failed" : "no_lines");
			jsonObject["spoke"] = false;
			ClickyAnalytics.TrackProductEvent("job_report_action", null, reportThreadId, jsonObject);
			return;
		}
		bool flag2;
		lock (gate)
		{
			foreach (Report item3 in batch)
			{
				linesByTurn[item3.TurnKey] = lines;
				string threadId2 = item3.ThreadId;
				if (threadId2 != null)
				{
					linesByThread[threadId2] = lines;
				}
			}
			flag2 = batch.Any((Report r) => r.SpeaksAloud) && !UserIsTalking();
			if (flag2)
			{
				foreach (Report item4 in batch)
				{
					spokenTurns.Add(item4.TurnKey);
				}
			}
		}
		Log.Info("JobReports", $"[one brain] report turn: {lines.Count} line(s) for {batch.Count} job(s), spoken={flag2}: {string.Join(" ", lines)}");
		jsonObject["action"] = "finished";
		jsonObject["spoke"] = flag2;
		jsonObject["wanted_to_speak"] = batch.Any((Report r) => r.SpeaksAloud);
		ClickyAnalytics.TrackProductEvent("job_report_action", null, reportThreadId, jsonObject);
		if (flag2)
		{
			Func<string, Task> speak = Speak;
			if (speak != null)
			{
				await speak(string.Join(" ", lines)).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		try
		{
			this.ReportPresented?.Invoke(batch.Select((Report r) => r.ThreadId).OfType<string>().ToList(), lines);
		}
		catch (Exception ex)
		{
			Log.Warn("JobReports", "ReportPresented handler threw: " + ex.Message);
		}
	}

	private async Task<IReadOnlyList<string>?> RunReportTurnAsync(string fact)
	{
		using CancellationTokenSource timeout = new CancellationTokenSource(AnswerTimeout);
		return await companion.RunReportTurnAsync(fact, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public string ShowResult(string argumentsJson)
	{
		JsonObject jsonObject = null;
		try
		{
			jsonObject = JsonNode.Parse(argumentsJson, null) as JsonObject;
		}
		catch (JsonException)
		{
		}
		string text = jsonObject?["codexThreadId"]?.ToString()?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return CardError("show_result needs the job's codexThreadId.");
		}
		ClickyAnswer clickyAnswer = ClickyAnswer.Parse(argumentsJson);
		if ((object)clickyAnswer == null)
		{
			return CardError("The card needs an outcome line.");
		}
		if (!agents.NoteResultCard(text, clickyAnswer))
		{
			return CardError("No job with that codexThreadId came back just now.");
		}
		lock (gate)
		{
			cardsShownThisTurn.Add(text);
			cardBlocksShownThisTurn += clickyAnswer.Blocks.Count;
		}
		Log.Info("JobReports", "[one brain] show_result: card for " + text.Substring(0, Math.Min(8, text.Length)) + " blocks=" + string.Join(",", clickyAnswer.Blocks));
		return Json("shown", null);
	}

	private string CardError(string message)
	{
		lock (gate)
		{
			cardErrorsThisTurn++;
		}
		return Json("error", message);
	}

	private static string Json(string status, string? message)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["status"] = status };
		if (message != null)
		{
			jsonObject["message"] = message;
		}
		return jsonObject.ToJsonString();
	}
}
