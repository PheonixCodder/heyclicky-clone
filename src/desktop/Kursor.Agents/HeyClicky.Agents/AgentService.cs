using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents.Backend;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Harness;
using HeyClicky.Agents.Jobs;
using HeyClicky.Agents.Routines;
using HeyClicky.Agents.Runtime;
using HeyClicky.Agents.Turns;

namespace HeyClicky.Agents;

public sealed class AgentService : IAgentService, IAsyncDisposable
{
	private sealed record TurnSpec(string Prompt, IReadOnlyList<byte[]>? Images, IReadOnlyList<string>? AttachedFiles, string LaunchContext, string LaunchSource, string UsageContext, bool IsGeneratedBrief, IReadOnlyList<(string User, string Assistant)>? PriorTurns, AgentTurnLease? PreauthorizedLease, bool AutoApproveExtraUsage, string? UserMemory);

	private sealed class RoutineHost(AgentService owner) : IRoutineHost
	{
		public bool IsSignedIn => owner.tokens.ExpiresAt.HasValue;

		public void BackfillWorkReportsIfNeeded()
		{
			owner.BackfillWorkReportsIfNeeded();
		}

		public bool ClickyExists(string slug)
		{
			ClickyInfo clickyInfo = owner.clickys.Get(slug);
			if ((object)clickyInfo != null)
			{
				return !clickyInfo.IsArchived;
			}
			return false;
		}

		public bool IsClickyBusy(string slug)
		{
			return owner.tasks.Values.Any((AgentTask t) => t.ClickySlug == slug && Lock(t, () => t.IsActive));
		}

		public bool IsClickyAwaitingExtraUsage(string slug)
		{
			return owner.tasks.Values.Any((AgentTask t) => t.ClickySlug == slug && Lock(t, () => t.RequiresExtraUsageApproval));
		}

		public bool JobExists(string jobId)
		{
			return (object)owner.Jobs.Job(jobId) != null;
		}

		public bool IsJobBusy(string jobId)
		{
			ClickyJob clickyJob = owner.Jobs.Job(jobId);
			if ((object)clickyJob == null)
			{
				return false;
			}
			AgentTask task = owner.LiveTask(clickyJob);
			if (task != null && Lock(task, () => task.IsActive))
			{
				return true;
			}
			return clickyJob.AllThreadIds.Any(owner.session.IsThreadActive);
		}

		public bool IsJobAwaitingExtraUsage(string jobId)
		{
			ClickyJob clickyJob = owner.Jobs.Job(jobId);
			if ((object)clickyJob != null)
			{
				AgentTask task = owner.LiveTask(clickyJob);
				if (task != null)
				{
					return Lock(task, () => task.RequiresExtraUsageApproval);
				}
			}
			return false;
		}

		public async Task<string?> LaunchJobRoutineRunAsync(ClickyRoutine routine, int runNumber, string prompt, bool scheduled)
		{
			ClickyJob job = owner.Jobs.Job(routine.JobId);
			if ((object)job == null)
			{
				return null;
			}
			string threadId = job.ThreadId;
			string continuedThreadId = ((threadId != null && AgentHarnessThreads.ContinuesOnNewConversationHarness(threadId)) ? threadId : null);
			string text = $"This turn is run #{runNumber} of the routine \"{routine.Title}\" ({routine.CadenceLabel.ToLowerInvariant()}) that the user set up on this job, so HeyClicky is launching it automatically with the user's standing instructions.";
			if (continuedThreadId == null)
			{
				owner.RetireClickysNoteForMovedRoutineJob(job);
				bool hasPreviousNote = File.Exists(Path.Combine(job.FolderPath, ClickyWorkFolderFiles.PreviousNoteRelativePath));
				text = text + "\n" + owner.JobContinuationContext(job, hasPreviousNote);
			}
			else if (runNumber > 1)
			{
				text += " Earlier runs are in this conversation above; build on them and report what is new rather than repeating them.";
			}
			AgentTask existing = owner.tasks.Values.FirstOrDefault((AgentTask x) => Lock(x, () => x.JobId) == job.Id);
			if (existing != null)
			{
				lock (existing.Gate)
				{
					existing.IsRoutineRun = true;
				}
				await owner.StartFollowUpTurnAsync(existing, prompt, null, "routine", "companion", null, CancellationToken.None, text, continuedThreadId, continuedThreadId == null).ConfigureAwait(continueOnCapturedContext: false);
				owner.Jobs.BeginRun(job.Id, routine.Title, continuedThreadId);
				return existing.Id;
			}
			string text2 = await owner.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = prompt,
				JobId = job.Id,
				TargetThreadId = continuedThreadId,
				StartsNewJobConversation = (continuedThreadId == null),
				LaunchSource = "routine",
				IsGeneratedTaskBrief = true,
				LaunchContext = text
			}).ConfigureAwait(continueOnCapturedContext: false);
			owner.Jobs.BeginRun(job.Id, routine.Title, continuedThreadId);
			if (owner.tasks.TryGetValue(text2, out AgentTask value))
			{
				lock (value.Gate)
				{
					value.IsRoutineRun = true;
				}
			}
			return text2;
		}

		public async Task<string?> LaunchRoutineRunAsync(ClickyRoutine routine, int runNumber, string prompt, string launchContext)
		{
			ClickyInfo clicky = owner.clickys.Get(routine.ClickySlug);
			if ((object)clicky == null)
			{
				return null;
			}
			AgentTask existing = owner.tasks.Values.FirstOrDefault((AgentTask t) => t.ClickySlug == clicky.Slug);
			string contextOverride = launchContext + TurnPromptBuilder.ClickyLaunchContext(clicky, foundedForThisTask: false);
			if (existing != null)
			{
				lock (existing.Gate)
				{
					existing.IsRoutineRun = true;
				}
				await owner.StartFollowUpTurnAsync(existing, prompt, null, "routine", "companion", null, CancellationToken.None, contextOverride).ConfigureAwait(continueOnCapturedContext: false);
				return existing.Id;
			}
			string text = await owner.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = prompt,
				ClickySlug = clicky.Slug,
				LaunchSource = "routine",
				IsGeneratedTaskBrief = true,
				LaunchContext = launchContext
			}).ConfigureAwait(continueOnCapturedContext: false);
			if (owner.tasks.TryGetValue(text, out AgentTask value))
			{
				lock (value.Gate)
				{
					value.IsRoutineRun = true;
				}
			}
			return text;
		}

		public string? CreateJobForMovedRoutine(ClickyRoutine routine)
		{
			ClickyInfo clickyInfo = owner.clickys.Get(routine.ClickySlug);
			if ((object)clickyInfo == null)
			{
				return null;
			}
			string folderPath = owner.clickys.EnsureWorkspace(clickyInfo);
			return owner.Jobs.CreateJobForMovedRoutine(routine.Title, routine.TaskPrompt, folderPath, clickyInfo.ThreadId, routine.LastRunFinishedAt ?? routine.LastRunStartedAt).Id;
		}

		public void RetireClickysNoteForMovedRoutineJob(string jobId, bool onlyWhenLeft)
		{
			ClickyJob clickyJob = owner.Jobs.Job(jobId);
			if ((object)clickyJob != null && (!onlyWhenLeft || File.Exists(LiveNotePathForJob(clickyJob))))
			{
				owner.RetireClickysNoteForMovedRoutineJob(clickyJob);
			}
		}
	}

	public const string BrowserMcpServerKey = "clicky_browser";

	private readonly ConcurrentDictionary<string, DateTimeOffset> clickyAppFinishedAt = new ConcurrentDictionary<string, DateTimeOffset>();

	private static readonly Regex RoutineHeader = new Regex("^\\[Routine \"(.+?)\"(?: —)? run #(\\d+), ([^,\\]]+)");

	private const string LegacyClickyMigrationKey = "clicky.clickyApps.legacyClickysAsJobs.v1";

	private const string LegacyClickyDockingKey = "clicky.clickyApps.legacyClickysDocked.v1";

	private const string LegacyClickyWorkBackfillKey = "clicky.clickyApps.legacyClickyWorkBackfilled.v1";

	public const int LegacyClickyDockLimit = 30;

	private readonly ConcurrentDictionary<string, string> computerUseSummaries = new ConcurrentDictionary<string, string>();

	private readonly ConcurrentDictionary<string, bool> computerUseAnnounced = new ConcurrentDictionary<string, bool>();

	private readonly ConcurrentDictionary<string, AgentTurnLease> reopenedLeases = new ConcurrentDictionary<string, AgentTurnLease>();

	private const string LeaseMarkerStart = "<clicky_agent_turn_lease_metadata>";

	private static readonly TimeSpan EarlyStopRefundWindow = TimeSpan.FromSeconds(10L);

	private static readonly TimeSpan ComposioRefreshAfter = TimeSpan.FromMinutes(10L);

	private readonly ClickyEndpoints endpoints;

	private readonly Func<CancellationToken, Task<string?>> supabaseAccessToken;

	private readonly AgentRuntimeOptions options;

	private readonly AgentWorkerApi api;

	private readonly AgentSessionTokenProvider tokens;

	private readonly CodexHomeManager home;

	private readonly ClickyStore clickys;

	private readonly UserMemoryCache userMemory;

	private readonly CodexSession session = new CodexSession();

	private readonly RoutineScheduler routines;

	private readonly SemaphoreSlim runtimeGate = new SemaphoreSlim(1, 1);

	private readonly SemaphoreSlim slots;

	private readonly ConcurrentDictionary<string, AgentTask> tasks = new ConcurrentDictionary<string, AgentTask>();

	private readonly CancellationTokenSource lifetime = new CancellationTokenSource();

	private ComposioMcpConfiguration? composio;

	private bool composioUnreachable;

	private DateTimeOffset composioFetchedAt;

	private AgentIntegrationCatalog? lastIntegrationCatalog;

	private IReadOnlySet<string>? composioRefetchedForConnectedApps;

	private IReadOnlyList<CustomConnectorRegistration> customConnectors = Array.Empty<CustomConnectorRegistration>();

	private bool configDirty = true;

	private bool runtimeFilesPrepared;

	private int tasksChangedPending;

	private bool disposed;

	private long runtimeReadyMs = -1L;

	private long launchToTurnStartedMs = -1L;

	private long launchToFirstTokenMs = -1L;

	private long launchToFinishedMs = -1L;

	private int composioGeneration;

	private int accountGeneration;

	private const int MaxPersonaNameBackfillPerLaunch = 15;

	private int personaNameBackfillStarted;

	public const string PaywallSummary = "Oops, you're out of tokens! Beef up HeyClicky to continue.";

	private readonly object dataSourcesGate = new object();

	private Dictionary<string, List<string>>? dataSources;

	private readonly HashSet<string> jobIdsBeingNamed = new HashSet<string>();

	private int jobNamingRoundRunning;

	private static readonly HashSet<string> SentenceStartWords = new HashSet<string>
	{
		"i", "im", "ive", "id", "sorry", "unfortunately", "please", "could", "can", "would",
		"there", "this", "it", "its", "you", "youre", "here", "heres", "based", "without",
		"what", "which", "how", "why", "unable", "no", "not"
	};

	private ClickyJobStepWatcher? stepWatcher;

	public static readonly TimeSpan JobResumeWindow = TimeSpan.FromHours(24);

	public const string FollowUpLanguageLine = "Answer in the language of this newest message.";

	public const string ResumedRunContext = "This is a new message to this job, in the conversation where you did the earlier work: build on it, and change what they ask now. Answer in the language of this newest message.";

	private readonly ConcurrentDictionary<string, ClickyAnswer> resultCards = new ConcurrentDictionary<string, ClickyAnswer>();

	public const int ReportReplyLimit = 6000;

	public Func<bool> ChromeIsConnected { get; set; } = () => false;

	public Func<string, bool> ThreadSkipsChrome { get; set; } = (string _) => false;

	public ClickyAppStore ClickyApps { get; private set; }

	public Func<string, string?>? TakeUnansweredComputerUseStep { get; set; }

	public Func<string, IReadOnlyList<(string User, string Assistant)>>? CompanionTurnsSinceLastMessage { get; set; }

	public Func<string, IReadOnlyList<(string User, string Assistant)>>? CompanionTurnsForFreshLaunch { get; set; }

	public Func<string, string, bool>? WasComputerUseStepRecentlyDeclined { get; set; }

	public Action<string>? ForgetDeclinedComputerUseSteps { get; set; }

	public IReadOnlyList<AgentTaskSnapshot> Tasks => (from s in tasks.Values.Select(delegate(AgentTask t)
		{
			lock (t.Gate)
			{
				return t.Snapshot();
			}
		})
		orderby s.StartedAt descending
		select s).ToList();

	public bool IsRuntimeReady => runtimeFilesPrepared;

	private bool ComposioSessionIsStale => ComposioSessionReuse.IsStale(composioFetchedAt, DateTimeOffset.Now, ComposioRefreshAfter);

	public string DefaultModel => options.Model;

	public string DefaultReasoningEffort => options.ReasoningEffort;

	public Func<string?>? CurrentUserId { get; set; }

	public bool AutoApproveExtraUsage
	{
		get
		{
			return options.AutoApproveExtraUsage;
		}
		set
		{
			options.AutoApproveExtraUsage = value;
		}
	}

	public bool AllowsDeveloperModelSelection => tokens.AllowsDeveloperModelSelection;

	public Func<string, (string Model, string Effort)?>? DeveloperModelSelection { get; set; }

	public static string DefaultProjectsRoot => new AgentRuntimeOptions().ProjectsRoot;

	public IReadOnlyList<ClickyInfo> Clickys => clickys.Clickys;

	public RoutineScheduler Routines => routines;

	public RoutinesTool RoutinesTool { get; }

	public string ProjectsRoot => options.ProjectsRoot;

	public string CodexHome => options.CodexHome;

	public AgentRuntimeTimings Timings => new AgentRuntimeTimings(runtimeReadyMs, launchToTurnStartedMs, launchToFirstTokenMs, launchToFinishedMs);

	public Func<CancellationToken, Task<AgentHarnessMcpServer?>>? BrowserMcpServerProvider { get; set; }

	public Func<bool>? IsClickyDriveAvailable { get; set; }

	public IClickyLiveNoteTurns? LiveNotes { get; set; }

	private string WorkerBase => endpoints.WorkerBaseUrl.AbsoluteUri.TrimEnd('/');

	public IReadOnlyList<ClickyInfo> ArchivedClickys => clickys.ArchivedClickys;

	public Func<bool>? CustomConnectorTokensExpiring { get; set; }

	public Func<CancellationToken, Task<IReadOnlyList<CustomConnectorRegistration>?>>? RefreshCustomConnectorTokensAsync { get; set; }

	private string DataSourcesPath => Path.Combine(options.StateDirectory, "data-sources.json");

	public ClickyJobStore Jobs { get; }

	public ClickyLearnedHowTos LearnedHowTos { get; }

	public ClickyRestartInterruptions RestartInterruptions { get; }

	public ClickyLiveSteps LiveSteps { get; } = new ClickyLiveSteps();

	public ClickyJobStepWatcher StepWatcher => stepWatcher ?? (stepWatcher = CreateStepWatcher());

	public Func<string, IReadOnlyList<AgentTranscriptTurn>?>? CachedTranscript { get; set; }

	public Func<string>? ChromeAnswerMeaningsForBrain { get; set; }

	public string JobsRootPath => Path.Combine(options.ProjectsRoot, "jobs");

	private static string HarnessRecordsRoot => Path.Combine(AgentHarnessRunnerProcess.StateDirectory, "h1-local");

	public event Action<string, string>? BrowserTurnEnded;

	public event Action<string>? TaskNeedsChromeConnection;

	public event Action<string>? TaskNoLongerWaitsOnChrome;

	public event EventHandler? TasksChanged;

	public event EventHandler<AgentTaskEvent>? TaskEvent;

	public event EventHandler? RoutinesChanged;

	public event EventHandler<AgentPaywallEventArgs>? PaywallReached;

	public event Action<bool>? AutoApproveExtraUsageChanged;

	public event EventHandler? ThreadHistoryChanged;

	public event Action<string>? ClickyFounded;

	public event Action<string, string, string, double, IReadOnlyDictionary<string, double>>? HarnessTurnEnded;

	public event Action<string, string, string, string>? InternalError;

	public event Action<string, string>? DataSourceRecorded;

	public event Action<string, string, string, string>? TaskFilePatchStreaming;

	public event Action<string, IReadOnlyList<string>>? TaskFileChangeCompleted;

	public event Action<string, string?, IReadOnlyDictionary<string, object?>>? JobAction;

	public event Action<string>? ResultCardShown;

	public AgentBrowserTurn? BrowserTurn(string threadId, string turnId)
	{
		AgentTask task = ActiveTask(threadId);
		if (task == null)
		{
			return null;
		}
		lock (task.Gate)
		{
			if (task.TurnId != turnId || !task.IsActive)
			{
				return null;
			}
			string title = task.Title;
			string latestCommentary = (string.IsNullOrWhiteSpace(task.LatestCommentary) ? null : task.LatestCommentary);
			return new AgentBrowserTurn(task.Title, title, latestCommentary, task.ThinkingHeading, delegate
			{
				lock (task.Gate)
				{
					return task.TurnId == turnId && task.IsActive && ActiveTask(threadId) == task;
				}
			});
		}
	}

	private AgentTask? ActiveTask(string threadId)
	{
		return tasks.Values.FirstOrDefault((AgentTask t) => Lock(t, () => t.ThreadId == threadId && t.IsActive));
	}

	public void NoteFoundChromeNotConnected(string threadId)
	{
		AgentTask agentTask = ActiveTask(threadId);
		if (agentTask != null)
		{
			lock (agentTask.Gate)
			{
				agentTask.FoundChromeNotConnectedThisTurn = true;
			}
		}
	}

	public bool FoundChromeNotConnectedThisTurn(string threadId)
	{
		AgentTask task = ActiveTask(threadId);
		if (task != null)
		{
			return Lock(task, () => task.FoundChromeNotConnectedThisTurn);
		}
		return false;
	}

	private void AskAboutChromeIfStoppedForIt(AgentTask task)
	{
		string threadId;
		bool foundChromeNotConnectedThisTurn;
		string finalResponseMarkdown;
		string summary;
		lock (task.Gate)
		{
			threadId = task.ThreadId;
			foundChromeNotConnectedThisTurn = task.FoundChromeNotConnectedThisTurn;
			finalResponseMarkdown = task.FinalResponseMarkdown;
			summary = task.Summary;
		}
		if (threadId == null)
		{
			return;
		}
		bool stoppedForChrome = foundChromeNotConnectedThisTurn && ChromeTurnEnd.HelperStoppedForChrome(finalResponseMarkdown, summary) && !ChromeIsConnected();
		if (!foundChromeNotConnectedThisTurn || !ChromeTurnEnd.AsksAtTurnEnd(foundChromeNotConnectedThisTurn, stoppedForChrome, ThreadSkipsChrome(threadId)))
		{
			this.TaskNoLongerWaitsOnChrome?.Invoke(threadId);
			return;
		}
		lock (task.Gate)
		{
			task.NeedsChromeConnection = true;
		}
		this.TaskNeedsChromeConnection?.Invoke(threadId);
	}

	public async Task<bool> PassInformationToRunningTurnAsync(string threadId, string text, CancellationToken ct = default(CancellationToken))
	{
		if (ActiveTask(threadId) == null)
		{
			return false;
		}
		try
		{
			if (!session.IsHarnessTurnRunning(threadId))
			{
				return false;
			}
			return await session.SteerHarnessTurnAsync(threadId, text, ct, informational: true).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "passing information to " + threadId + "'s running turn failed: " + ex.Message);
			return false;
		}
	}

	public string? TaskIdForThread(string threadId)
	{
		return (from t in tasks.Values
			where Lock(t, () => t.ThreadId == threadId)
			orderby t.StartedAt descending
			select t).FirstOrDefault()?.Id;
	}

	public bool IsThreadRunning(string threadId)
	{
		return ActiveTask(threadId) != null;
	}

	public IReadOnlyList<string> ThreadsNeedingChrome()
	{
		return (from t in tasks.Values
			where Lock(t, () => t.NeedsChromeConnection && t.ThreadId != null)
			orderby t.StartedAt
			select Lock(t, () => t.ThreadId)).ToList();
	}

	public void ClearNeedsChromeConnection(string threadId)
	{
		foreach (AgentTask value in tasks.Values)
		{
			lock (value.Gate)
			{
				if (value.ThreadId == threadId)
				{
					value.NeedsChromeConnection = false;
				}
			}
		}
		RaiseTasksChanged();
	}

	public bool NeedsChromeConnection(string threadId)
	{
		return tasks.Values.Any((AgentTask t) => Lock(t, () => t.ThreadId == threadId && t.NeedsChromeConnection));
	}

	public bool IsTaskWaitingOnUserAnswer(string taskId)
	{
		if (!tasks.TryGetValue(taskId, out AgentTask task))
		{
			return false;
		}
		return Lock(task, () => task.PendingComputerUseRequest != null || task.RequiresExtraUsageApproval || task.NeedsChromeConnection);
	}

	private void NotifyBrowserTurnEnded(AgentTask task, TurnOutcome outcome)
	{
		string threadId;
		string turnId;
		lock (task.Gate)
		{
			threadId = task.ThreadId;
			turnId = task.TurnId;
		}
		if (threadId != null && turnId != null)
		{
			this.BrowserTurnEnded?.Invoke(threadId, turnId);
		}
	}

	private void CreateClickyApps()
	{
		ClickyApps = new ClickyAppStore(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(options.StateDirectory.TrimEnd('\\', '/'))) ?? options.StateDirectory, "ClickyApps"));
	}

	private AgentTask? ClickyAppLiveTask(string slug)
	{
		List<AgentTask> source = (from t in tasks.Values
			where !t.IsDemo
			orderby t.StartedAt descending
			select t).ToList();
		string boundThreadId = clickys.Get(slug)?.ThreadId;
		if (boundThreadId != null)
		{
			AgentTask agentTask = source.FirstOrDefault((AgentTask t) => t.ThreadId == boundThreadId);
			if (agentTask != null)
			{
				return agentTask;
			}
		}
		return source.FirstOrDefault((AgentTask t) => t.ClickySlug == slug);
	}

	public string? ClickyAppLiveThreadId(string slug)
	{
		AgentTask task = ClickyAppLiveTask(slug);
		object obj = ((task != null) ? Lock(task, () => task.ThreadId) : null);
		if (obj == null)
		{
			ClickyInfo? clickyInfo = clickys.Get(slug);
			if ((object)clickyInfo == null)
			{
				return null;
			}
			obj = clickyInfo.ThreadId;
		}
		return (string?)obj;
	}

	public ClickyAppLiveStatus LiveStatusForClicky(string slug)
	{
		AgentTask agentTask = ClickyAppLiveTask(slug);
		if (agentTask == null)
		{
			return ClickyAppLiveStatus.Idle;
		}
		lock (agentTask.Gate)
		{
			if (agentTask.PendingComputerUseRequest != null)
			{
				return new ClickyAppLiveStatus(ClickyAppLiveStatus.Phases.NeedsYou, "I need a yes to use your PC", Array.Empty<string>(), agentTask.StartedAt, null);
			}
			if (agentTask.RequiresExtraUsageApproval || agentTask.Status == AgentTaskStatus.WaitingForUser)
			{
				return new ClickyAppLiveStatus(ClickyAppLiveStatus.Phases.NeedsYou, "I need your OK to keep going", Array.Empty<string>(), agentTask.StartedAt, null);
			}
			switch (agentTask.Status)
			{
			case AgentTaskStatus.Starting:
			case AgentTaskStatus.Running:
			{
				List<string> source = (from a in agentTask.Activity
					where a.Kind == AgentActivityKind.Commentary
					select ClickyWorkReplyParts.FirstSentence(a.Text, 140) into l
					where l.Length > 0
					select l).ToList();
				string text3 = agentTask.ThinkingHeading?.Trim();
				string line3 = source.LastOrDefault() ?? (string.IsNullOrEmpty(text3) ? "Working on it" : text3);
				return new ClickyAppLiveStatus(ClickyAppLiveStatus.Phases.Working, line3, source.TakeLast(5).ToList(), agentTask.StartedAt, null);
			}
			case AgentTaskStatus.Finished:
			{
				string text = agentTask.Summary?.Trim();
				object obj;
				if (text == null || text.Length <= 0)
				{
					string text2 = agentTask.DoneTitle?.Trim();
					obj = ((text2 != null && text2.Length > 0) ? text2 : "Done");
				}
				else
				{
					obj = text;
				}
				string line2 = (string)obj;
				return new ClickyAppLiveStatus(ClickyAppLiveStatus.Phases.Finished, line2, Array.Empty<string>(), agentTask.StartedAt, clickyAppFinishedAt.GetValueOrDefault(agentTask.Id));
			}
			default:
			{
				string line = ((agentTask.Status == AgentTaskStatus.Stopped) ? "Stopped before I finished" : "Something went wrong");
				return new ClickyAppLiveStatus(ClickyAppLiveStatus.Phases.Failed, line, Array.Empty<string>(), agentTask.StartedAt, clickyAppFinishedAt.GetValueOrDefault(agentTask.Id));
			}
			}
		}
	}

	private void NoteClickyAppWorkFinished(AgentTask task)
	{
		clickyAppFinishedAt[task.Id] = DateTimeOffset.Now;
		bool isDemo;
		string text;
		lock (task.Gate)
		{
			isDemo = task.IsDemo;
			object obj = task.ClickySlug;
			if (obj == null)
			{
				string thread = task.ThreadId;
				obj = ((thread == null) ? null : clickys.Clickys.FirstOrDefault((ClickyInfo c) => c.ThreadId == thread)?.Slug);
			}
			text = (string)obj;
		}
		if (!(!ClickyAppsPreferences.IsEnabled || isDemo) && text != null)
		{
			ClickyWorkPiece clickyWorkPiece = ClickyAppWorkPiece(task, text);
			if ((object)clickyWorkPiece != null)
			{
				ClickyApps.Merge(new _003C_003Ez__ReadOnlySingleElementList<ClickyWorkPiece>(clickyWorkPiece), text);
			}
		}
	}

	private ClickyWorkPiece? ClickyAppWorkPiece(AgentTask task, string slug)
	{
		lock (task.Gate)
		{
			(string, string, string, IReadOnlyList<string>) tuple = ClickyWorkReplyParts.Parse(task.FinalResponseMarkdown ?? string.Join("\n\n", task.CompletedReplies));
			string item = tuple.Item1;
			IReadOnlyList<string> readOnlyList = ((task.Artifacts.Count == 0) ? tuple.Item4 : task.Artifacts);
			if (item.Length == 0 && readOnlyList.Count == 0)
			{
				return null;
			}
			string text = task.ThreadId ?? ("task-" + task.Id);
			string text2 = $"{task.Id}:{task.UserPrompts.Count}";
			string text3 = task.TurnId ?? text2;
			string text4 = task.UserPrompts.LastOrDefault() ?? "";
			string text5 = task.Summary?.Trim();
			string summary = ((text5 != null && text5.Length > 0) ? text5 : (tuple.Item3 ?? ClickyWorkReplyParts.FirstSentence(item)));
			string? text6 = task.DoneTitle?.Trim();
			ClickyWorkPiece obj = new ClickyWorkPiece
			{
				Id = text + "#" + text3,
				Slug = slug,
				ThreadId = task.ThreadId,
				ClientTurnKey = text2,
				Ask = DisplayAsk(text4),
				Title = ((text6 != null && text6.Length > 0) ? text6 : (tuple.Item2 ?? "")),
				Summary = summary,
				Reply = ((item.Length > 20000) ? item.Substring(0, 20000) : item),
				ArtifactPaths = readOnlyList.ToList(),
				FinishedAt = clickyAppFinishedAt.GetValueOrDefault(task.Id, DateTimeOffset.Now),
				IsRoutineRun = task.IsRoutineRun || RoutineHeader.IsMatch(text4.TrimStart())
			};
			return obj;
		}
	}

	public static IReadOnlyList<ClickyWorkPiece> ClickyAppWorkPieces(IReadOnlyList<AgentTranscriptTurn> turns, string threadId, string slug, Func<string, string>? displayAsk = null)
	{
		List<ClickyWorkPiece> list = new List<ClickyWorkPiece>();
		for (int i = 0; i < turns.Count; i++)
		{
			AgentTranscriptTurn agentTranscriptTurn = turns[i];
			if (!string.Equals(agentTranscriptTurn.Status, "completed", StringComparison.OrdinalIgnoreCase) || agentTranscriptTurn.ErrorMessage != null)
			{
				continue;
			}
			(string, string, string, IReadOnlyList<string>) tuple = ClickyWorkReplyParts.Parse(string.Join("\n\n", agentTranscriptTurn.AssistantMessages));
			IReadOnlyList<string> readOnlyList = ((agentTranscriptTurn.Artifacts.Count == 0) ? tuple.Item4 : agentTranscriptTurn.Artifacts);
			if (tuple.Item1.Length != 0 || readOnlyList.Count != 0)
			{
				string text = agentTranscriptTurn.UserMessages.FirstOrDefault() ?? "";
				string text2 = agentTranscriptTurn.TurnId ?? $"{i}-{(agentTranscriptTurn.StartedAt ?? DateTimeOffset.UnixEpoch).ToUnixTimeSeconds()}";
				string reply;
				if (tuple.Item1.Length <= 20000)
				{
					(reply, _, _, _) = tuple;
				}
				else
				{
					reply = tuple.Item1.Substring(0, 20000);
				}
				ClickyWorkPiece obj = new ClickyWorkPiece
				{
					Id = threadId + "#" + text2,
					Slug = slug,
					ThreadId = threadId,
					Ask = (displayAsk ?? ((Func<string, string>)((string a) => RoutinePreviewLine(a) ?? a.Trim())))(text),
					Title = (tuple.Item2 ?? ""),
					Summary = (tuple.Item3 ?? ClickyWorkReplyParts.FirstSentence(tuple.Item1)),
					Reply = reply,
					ArtifactPaths = readOnlyList.ToList(),
					FinishedAt = agentTranscriptTurn.FinishedAt ?? agentTranscriptTurn.StartedAt ?? DateTimeOffset.MinValue,
					IsRoutineRun = RoutineHeader.IsMatch(text.TrimStart())
				};
				list.Add(obj);
			}
		}
		return list;
	}

	public async Task GatherClickyAppWorkAsync(string slug, CancellationToken ct = default(CancellationToken))
	{
		ClickyApps.LoadIfNeeded(slug);
		List<ClickyWorkPiece> pieces = new List<ClickyWorkPiece>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string text = clickys.Get(slug)?.ThreadId;
		if (text != null)
		{
			hashSet.Add(text);
		}
		foreach (AgentTask value in tasks.Values)
		{
			if (value.ClickySlug == slug)
			{
				string threadId = value.ThreadId;
				if (threadId != null)
				{
					hashSet.Add(threadId);
				}
			}
		}
		foreach (string threadId2 in hashSet)
		{
			try
			{
				pieces.AddRange(ClickyAppWorkPieces(await ReadThreadAsync(threadId2, ct).ConfigureAwait(continueOnCapturedContext: false), threadId2, slug, DisplayAsk));
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				Log.Warn("Agents", "clicky app work for " + slug + ": " + ex.Message);
			}
		}
		AgentTask live = ClickyAppLiveTask(slug);
		if (live != null && Lock(live, () => live.Status == AgentTaskStatus.Finished))
		{
			ClickyWorkPiece clickyWorkPiece = ClickyAppWorkPiece(live, slug);
			if ((object)clickyWorkPiece != null)
			{
				pieces.Add(clickyWorkPiece);
			}
		}
		ClickyApps.Merge(pieces, slug);
	}

	private static string? RoutinePreviewLine(string rawAsk)
	{
		Match match = RoutineHeader.Match(rawAsk.TrimStart());
		if (match == null || !match.Success)
		{
			return null;
		}
		return match.Groups[1].Value + " · run " + match.Groups[2].Value;
	}

	private string DisplayAsk(string rawAsk)
	{
		string text = rawAsk.Trim();
		if (text.StartsWith("<clicky_agent_turn_lease_metadata>", StringComparison.Ordinal))
		{
			int num = text.IndexOf("</clicky_agent_turn_lease_metadata>", StringComparison.Ordinal);
			if (num >= 0)
			{
				string text2 = text;
				int num2 = num + "</clicky_agent_turn_lease_metadata>".Length;
				text = text2.Substring(num2, text2.Length - num2).Trim();
			}
		}
		return RoutinePreviewLine(text) ?? clickys.DisplayFor(text) ?? text;
	}

	public IReadOnlyList<string>? TakeLegacyClickyMigration()
	{
		if (AgentDefaults.Bool("clicky.clickyApps.legacyClickysAsJobs.v1"))
		{
			return null;
		}
		AgentDefaults.Set("clicky.clickyApps.legacyClickysAsJobs.v1", value: true);
		List<string> list = clickys.Clickys.Select((ClickyInfo c) => c.Slug).ToList();
		Log.Info("Agents", $"[migration] old Clickys marked read ({list.Count}), pins cleared");
		return list;
	}

	public IReadOnlyList<string>? TakeLegacyClickysToDock()
	{
		if (AgentDefaults.Bool("clicky.clickyApps.legacyClickysDocked.v1"))
		{
			return null;
		}
		IReadOnlyList<ClickyInfo> readOnlyList = clickys.Clickys;
		if (readOnlyList.Count == 0)
		{
			return null;
		}
		AgentDefaults.Set("clicky.clickyApps.legacyClickysDocked.v1", value: true);
		Dictionary<string, DateTimeOffset> rolloutUpdated;
		try
		{
			rolloutUpdated = (from t in CodexRolloutHistory.List(options.CodexHome)
				group t by t.ThreadId).ToDictionary((IGrouping<string, CodexRolloutHistory.RolloutThread> g) => g.Key, (IGrouping<string, CodexRolloutHistory.RolloutThread> g) => g.Max((CodexRolloutHistory.RolloutThread t) => t.UpdatedAt));
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "[migration] rollout list: " + ex.Message);
			rolloutUpdated = new Dictionary<string, DateTimeOffset>();
		}
		List<string> list = (from c in readOnlyList
			where c.ThreadId != null
			select (Slug: c.Slug, Date: LastUsed(c.ThreadId)) into c
			orderby c.Date descending
			select c.Slug).Distinct().Take(30).ToList();
		Log.Info("Agents", $"[migration] docked {list.Count} recent old Clickys");
		return list;
		DateTimeOffset LastUsed(string threadId)
		{
			if (rolloutUpdated.TryGetValue(threadId, out var value))
			{
				return value;
			}
			string text = HarnessRecordThreadId(threadId);
			if (text != null)
			{
				string text2 = ClickyHarnessRecord.RecordDirectory(HarnessRecordsRoot, text);
				if (text2 != null)
				{
					try
					{
						string path = Path.Combine(text2, "events.jsonl");
						return File.Exists(path) ? File.GetLastWriteTime(path) : Directory.GetLastWriteTime(text2);
					}
					catch (Exception)
					{
					}
				}
			}
			return DateTimeOffset.MinValue;
		}
	}

	public void BackfillLegacyClickyWorkIfNeeded()
	{
		if (AgentDefaults.Bool("clicky.clickyApps.legacyClickyWorkBackfilled.v1"))
		{
			return;
		}
		List<string> slugs = (from c in clickys.Clickys
			where c.ThreadId != null
			select c.Slug).ToList();
		if (slugs.Count == 0)
		{
			return;
		}
		Task.Run(async delegate
		{
			foreach (string slug in slugs)
			{
				try
				{
					await GatherClickyAppWorkAsync(slug, lifetime.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception ex2)
				{
					Log.Warn("Agents", "[migration] " + slug + "'s work: " + ex2.Message);
				}
			}
			AgentDefaults.Set("clicky.clickyApps.legacyClickyWorkBackfilled.v1", value: true);
			Log.Info("Agents", $"[migration] old Clickys' work read from their chats ({slugs.Count})");
		});
	}

	private void ForgetDeclinedStepsIfUserMessage(string taskId, string launchSource)
	{
		if ((launchSource == "text" || launchSource == "voice") ? true : false)
		{
			ForgetDeclinedComputerUseSteps?.Invoke(taskId);
		}
	}

	public string? PendingComputerUseRequest(string taskId)
	{
		if (!tasks.TryGetValue(taskId, out AgentTask task))
		{
			return null;
		}
		return Lock(task, () => task.PendingComputerUseRequest);
	}

	public void SetPendingComputerUseRequest(string taskId, string? request)
	{
		if (tasks.TryGetValue(taskId, out AgentTask value))
		{
			bool flag;
			lock (value.Gate)
			{
				flag = value.PendingComputerUseRequest != request;
				value.PendingComputerUseRequest = request;
			}
			if (request == null)
			{
				computerUseSummaries.TryRemove(taskId, out string _);
			}
			if (flag)
			{
				SyncPendingRequest(value);
				RaiseTasksChanged();
			}
		}
	}

	public string? PendingComputerUseSummary(string taskId)
	{
		return computerUseSummaries.GetValueOrDefault(taskId);
	}

	public void SetPendingComputerUseSummary(string taskId, string? summary)
	{
		if (summary == null)
		{
			computerUseSummaries.TryRemove(taskId, out string _);
		}
		else
		{
			computerUseSummaries[taskId] = summary;
		}
	}

	public bool DidAnnounceComputerUseRequest(string taskId)
	{
		return computerUseAnnounced.GetValueOrDefault(taskId);
	}

	public void SetDidAnnounceComputerUseRequest(string taskId, bool value)
	{
		computerUseAnnounced[taskId] = value;
	}

	public bool IsThreadActive(string threadId)
	{
		return session.IsThreadActive(threadId);
	}

	public string? TurnLeaseId(string taskId)
	{
		if (!tasks.TryGetValue(taskId, out AgentTask task))
		{
			return null;
		}
		return Lock(task, () => task.Lease?.LeaseId);
	}

	public async Task<string?> ContinueLeaseAfterComputerUseConsentAsync(string leaseId, CancellationToken ct = default(CancellationToken))
	{
		double[] array = new double[4] { 0.0, 0.8, 1.6, 3.0 };
		foreach (double num in array)
		{
			if (num > 0.0)
			{
				await Task.Delay(TimeSpan.FromSeconds(num), ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			(bool, AgentTurnLease, string)? tuple;
			try
			{
				string value = await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false);
				if (string.IsNullOrEmpty(value))
				{
					Log.Info("Agents", "[lease/computer-use-consent] no access token");
					continue;
				}
				tuple = await api.ContinueAfterComputerUseConsentAsync(value, leaseId, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				Log.Info("Agents", "[lease/computer-use-consent] lease=" + leaseId.Substring(0, Math.Min(8, leaseId.Length)) + " uncertain: " + ex.Message);
				continue;
			}
			if (tuple.HasValue)
			{
				(bool, AgentTurnLease, string) valueOrDefault = tuple.GetValueOrDefault();
				if (valueOrDefault.Item1)
				{
					AgentTurnLease item = valueOrDefault.Item2;
					if ((object)item != null)
					{
						reopenedLeases[item.LeaseId] = item;
						return item.LeaseId;
					}
				}
			}
			if (tuple.HasValue)
			{
				(bool, AgentTurnLease, string) valueOrDefault2 = tuple.GetValueOrDefault();
				if (!valueOrDefault2.Item1 && valueOrDefault2.Item3 != "no_computer_use_request")
				{
					return null;
				}
			}
			Log.Info("Agents", "[lease/computer-use-consent] lease=" + leaseId.Substring(0, Math.Min(8, leaseId.Length)) + " not reopened yet");
		}
		Log.Info("Agents", "[lease/computer-use-consent] lease=" + leaseId.Substring(0, Math.Min(8, leaseId.Length)) + " gave up; sending the answer as a new turn");
		return null;
	}

	public void CompleteLease(string leaseId, string? threadId, string status = "completed")
	{
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				string text = await supabaseAccessToken(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				if (text != null)
				{
					await api.CompleteLeaseAsync(text, leaseId, status, threadId, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Agents", "lease " + leaseId.Substring(0, Math.Min(8, leaseId.Length)) + " complete failed: " + ex.Message);
			}
		});
	}

	public async Task SubmitThreadFollowUpAsync(string threadId, string text, string? preauthorizedLeaseId, CancellationToken ct = default(CancellationToken), string? launchContext = null)
	{
		AgentTask task = (from t in tasks.Values
			where Lock(t, () => t.ThreadId) == threadId
			orderby t.StartedAt descending
			select t).FirstOrDefault();
		if (task == null)
		{
			Log.Warn("Agents", "follow-up for " + threadId + ": no task on that thread");
			return;
		}
		AgentTurnLease value;
		AgentTurnLease preauthorizedLease = ((preauthorizedLeaseId != null && reopenedLeases.TryRemove(preauthorizedLeaseId, out value)) ? value : null);
		if (Lock(task, () => task.IsActive) || session.IsThreadActive(threadId))
		{
			await SteerAsync(task.Id, text, ct).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		object obj;
		if (launchContext != null)
		{
			string text2 = Lock(task, () => task.ClickySlug);
			if (text2 != null)
			{
				ClickyInfo clickyInfo = clickys.Get(text2);
				if ((object)clickyInfo != null)
				{
					obj = TurnPromptBuilder.ClickyLaunchContext(clickyInfo, foundedForThisTask: false, launchContext.Trim() + " ");
					goto IL_01db;
				}
			}
			obj = launchContext;
		}
		else
		{
			obj = null;
		}
		goto IL_01db;
		IL_01db:
		string contextOverride = (string)obj;
		await StartFollowUpTurnAsync(task, text, null, "text", "companion", preauthorizedLease, ct, contextOverride).ConfigureAwait(continueOnCapturedContext: false);
	}

	public bool CanRetryAfterFailure(string taskId)
	{
		if (tasks.TryGetValue(taskId, out AgentTask task))
		{
			return Lock(task, () => task.Status == AgentTaskStatus.Failed && (task.ThreadId != null || (task.UserPrompts.LastOrDefault()?.Trim().Length ?? 0) > 0));
		}
		return false;
	}

	public bool CanRetryAfterInterruption(string taskId)
	{
		if (tasks.TryGetValue(taskId, out AgentTask task))
		{
			return Lock(task, () => task.Status == AgentTaskStatus.Stopped && !task.RequiresExtraUsageApproval && (task.ThreadId != null || (task.UserPrompts.LastOrDefault()?.Trim().Length ?? 0) > 0));
		}
		return false;
	}

	public bool RetryFailedTask(string taskId)
	{
		if (!CanRetryAfterFailure(taskId) && !CanRetryAfterInterruption(taskId))
		{
			return false;
		}
		AgentTask task = GetTask(taskId);
		string text = Lock(task, () => task.UserPrompts.LastOrDefault()?.Trim() ?? "");
		string retry = ((text.Length == 0) ? "Retry the task you were working on and finish it." : text);
		Task.Run(async delegate
		{
			try
			{
				await StartFollowUpTurnAsync(task, retry, null, "retry", "companion", null, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Warn("Agents", "retry " + taskId + ": " + ex.Message);
			}
		});
		return true;
	}

	public AgentService(ClickyEndpoints endpoints, Func<CancellationToken, Task<string?>> supabaseAccessToken, HttpClient http, ClientIdentity identity, AgentRuntimeOptions? options = null)
	{
		this.endpoints = endpoints;
		this.supabaseAccessToken = supabaseAccessToken;
		this.options = options ?? new AgentRuntimeOptions();
		AgentDefaults.Configure(this.options.StateDirectory);
		Jobs = new ClickyJobStore();
		Jobs.Changed += delegate
		{
			NameJobsWithoutNicknames();
		};
		LearnedHowTos = new ClickyLearnedHowTos();
		RestartInterruptions = new ClickyRestartInterruptions();
		TasksChanged += delegate
		{
			NoteRunningTurnsForRestart();
		};
		CreateClickyApps();
		api = new AgentWorkerApi(EnsureTrailingSlash(endpoints.WorkerBaseUrl), http);
		tokens = new AgentSessionTokenProvider(api, supabaseAccessToken, this.options.SessionTokenRefreshMargin);
		home = new CodexHomeManager(this.options);
		clickys = new ClickyStore(this.options.StateDirectory, this.options.ProjectsRoot);
		userMemory = new UserMemoryCache(this.options.StateDirectory);
		slots = new SemaphoreSlim(Math.Max(1, this.options.MaxConcurrentTasks));
		routines = new RoutineScheduler(this.options.StateDirectory, new RoutineHost(this));
		routines.Changed += delegate
		{
			this.RoutinesChanged?.Invoke(this, EventArgs.Empty);
		};
		RoutinesTool = new RoutinesTool(routines, Jobs, (string threadId) => clickys.ForThread(threadId)?.Slug, (string slug) => clickys.Get(slug)?.Name, () => ProjectsRoot);
		session.OnTaskEvent = delegate(AgentTask task, AgentTaskEventKind kind, string? text)
		{
			Raise(task, kind, text);
		};
		session.OnTurnEnded = OnTurnEnded;
		WireLiveSteps();
		WireIntegrationEvents();
		ConfigureAgentHarness();
		Jobs.JobIdsWithRoutines = routines.JobIdsWithRoutines;
	}

	public AgentService(ClickyEndpoints endpoints, Func<CancellationToken, Task<string?>> supabaseAccessToken, Func<HttpClient> httpFactory, ClientIdentity identity, AgentRuntimeOptions? options = null)
		: this(endpoints, supabaseAccessToken, httpFactory(), identity, options)
	{
	}

	private static Uri EnsureTrailingSlash(Uri u)
	{
		if (!u.AbsoluteUri.EndsWith('/'))
		{
			return new Uri(u.AbsoluteUri + "/");
		}
		return u;
	}

	private IReadOnlyList<(string User, string Assistant)>? FollowUpCompanionTurns(AgentLaunchRequest request, string? conversationKey)
	{
		string launchSource = request.LaunchSource;
		if ((launchSource == "text" || launchSource == "voice") ? true : false)
		{
			IReadOnlyList<(string, string)> priorCompanionTurns = request.PriorCompanionTurns;
			if (priorCompanionTurns == null || priorCompanionTurns.Count <= 0)
			{
				if (conversationKey != null)
				{
					IReadOnlyList<(string, string)> readOnlyList = CompanionTurnsSinceLastMessage?.Invoke(conversationKey);
					if (readOnlyList != null && readOnlyList.Count > 0)
					{
						return readOnlyList;
					}
				}
				return null;
			}
			return priorCompanionTurns;
		}
		return null;
	}

	private IReadOnlyList<(string User, string Assistant)>? FreshLaunchCompanionTurns(AgentLaunchRequest request)
	{
		IReadOnlyList<(string, string)> priorCompanionTurns = request.PriorCompanionTurns;
		if (priorCompanionTurns != null && priorCompanionTurns.Count > 0)
		{
			return priorCompanionTurns;
		}
		string launchSource = request.LaunchSource;
		object result;
		if ((launchSource == "text" || launchSource == "voice") ? true : false)
		{
			IReadOnlyList<(string, string)> readOnlyList = CompanionTurnsForFreshLaunch?.Invoke(request.Prompt);
			if (readOnlyList != null && readOnlyList.Count > 0)
			{
				result = readOnlyList;
				goto IL_006d;
			}
		}
		result = null;
		goto IL_006d;
		IL_006d:
		return (IReadOnlyList<(string User, string Assistant)>?)result;
	}

	private string? ConversationKey(AgentTask task)
	{
		return Lock(task, () => task.JobId ?? task.ThreadId);
	}

	private string? LaunchContextWithBuildsOn(AgentLaunchRequest request, string? receivingJobId)
	{
		string text = request.LaunchContext?.Trim();
		string text2 = ((text != null && text.Length > 0) ? text : null);
		IReadOnlyList<string> buildsOnJobIds = request.BuildsOnJobIds;
		string text3 = ((buildsOnJobIds != null && buildsOnJobIds.Count > 0) ? Jobs.BuildsOnContext((from id in buildsOnJobIds
			select id.Trim() into id
			where id.Length > 0
			select id).Distinct().Take(3), receivingJobId, EarlierWorkElsewhere) : null);
		if (text2 != null)
		{
			if (text3 != null)
			{
				return text2 + " " + text3;
			}
			return text2;
		}
		return text3;
	}

	public async Task EnsureRuntimeReadyAsync(CancellationToken ct = default(CancellationToken))
	{
		RefetchComposioIfConnectedAppsChanged();
		if (runtimeFilesPrepared && !configDirty && !ComposioSessionIsStale)
		{
			Func<bool>? customConnectorTokensExpiring = CustomConnectorTokensExpiring;
			if (customConnectorTokensExpiring == null || !customConnectorTokensExpiring())
			{
				return;
			}
		}
		await runtimeGate.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await PrepareRuntimeLockedAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			runtimeGate.Release();
		}
	}

	public Task<string> LaunchAsync(string prompt, IReadOnlyList<byte[]>? imagesJpeg = null, CancellationToken ct = default(CancellationToken))
	{
		return LaunchAsync(new AgentLaunchRequest
		{
			Prompt = prompt,
			ImagesJpeg = imagesJpeg
		}, ct);
	}

	public Task SteerAsync(string taskId, string text, CancellationToken ct = default(CancellationToken))
	{
		return SteerAsync(taskId, text, null, ct);
	}

	public Task SteerAsync(string taskId, string text, IReadOnlyList<byte[]>? imagesJpeg, CancellationToken ct = default(CancellationToken), bool carriesCompanionConversation = false)
	{
		AgentTask task = GetTask(taskId);
		if (carriesCompanionConversation)
		{
			ForgetDeclinedComputerUseSteps?.Invoke(taskId);
		}
		object obj;
		if (carriesCompanionConversation)
		{
			string text2 = ConversationKey(task);
			if (text2 != null)
			{
				obj = CompanionTurnsSinceLastMessage?.Invoke(text2);
				goto IL_0043;
			}
		}
		obj = null;
		goto IL_0043;
		IL_0043:
		IReadOnlyList<(string, string)> readOnlyList = (IReadOnlyList<(string, string)>)obj;
		return SteerAsync(task, text, imagesJpeg, (readOnlyList != null && readOnlyList.Count > 0) ? readOnlyList : null, ct);
	}

	private async Task SteerAsync(AgentTask task, string text, IReadOnlyList<byte[]>? imagesJpeg, IReadOnlyList<(string User, string Assistant)>? priorTurns, CancellationToken ct)
	{
		string taskId = task.Id;
		string threadId;
		AgentTaskStatus status;
		lock (task.Gate)
		{
			threadId = task.ThreadId;
			status = task.Status;
		}
		Stopwatch sw;
		if (status == AgentTaskStatus.Starting)
		{
			sw = Stopwatch.StartNew();
			while (sw.Elapsed < TimeSpan.FromSeconds(60L))
			{
				lock (task.Gate)
				{
					threadId = task.ThreadId;
					status = task.Status;
				}
				if (status != 0 || (threadId != null && session.IsThreadActive(threadId)))
				{
					break;
				}
				await Task.Delay(200, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		if (threadId == null || !session.IsHarnessTurnRunning(threadId))
		{
			await StartFollowUpTurnAsync(task, text, imagesJpeg, "text", "companion", null, ct, null, null, startsNewJobConversation: false, priorTurns).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		lock (task.Gate)
		{
			task.UserPrompts.Add(text);
		}
		bool flag;
		try
		{
			List<string> imagePaths = await WriteTaskImagesAsync(task, imagesJpeg, "steer", ct).ConfigureAwait(continueOnCapturedContext: false);
			string text2 = ((priorTurns == null) ? text : (text + "\n" + TurnPromptBuilder.PriorConversationSection(priorTurns)));
			flag = await session.SteerHarnessTurnAsync(threadId, text2, ct, informational: false, imagePaths).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception)
		{
			lock (task.Gate)
			{
				task.UserPrompts.RemoveAt(task.UserPrompts.Count - 1);
			}
			throw;
		}
		if (flag)
		{
			lock (task.Gate)
			{
				task.ProgressSummary = "Steering the agent…";
			}
			Raise(task, AgentTaskEventKind.ProgressChanged, "Steering the agent…");
			Raise(task, AgentTaskEventKind.SteerAccepted, text);
			LogAgentMessage(task, threadId, "user", text, isFollowUp: true);
			Log.Info("Agents", "steered harness task " + taskId);
			return;
		}
		lock (task.Gate)
		{
			task.UserPrompts.RemoveAt(task.UserPrompts.Count - 1);
		}
		sw = Stopwatch.StartNew();
		while ((session.IsThreadActive(threadId) || Lock(task, () => task.IsActive)) && sw.Elapsed < TimeSpan.FromSeconds(25L))
		{
			await Task.Delay(100, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		Log.Info("Agents", "harness task " + taskId + " had just finished: the message starts a new turn");
		await StartFollowUpTurnAsync(task, text, imagesJpeg, "text", "companion", null, ct, null, null, startsNewJobConversation: false, priorTurns).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<List<string>> WriteTaskImagesAsync(AgentTask task, IReadOnlyList<byte[]>? imagesJpeg, string prefix, CancellationToken ct)
	{
		List<string> paths = new List<string>();
		if (imagesJpeg == null || imagesJpeg.Count <= 0)
		{
			return paths;
		}
		string dir = home.TaskAssetsDirectory(task.NumericId);
		for (int n = 0; n < imagesJpeg.Count; n++)
		{
			string path = Path.Combine(dir, $"{prefix}-{DateTime.Now:HHmmss}-{n + 1}.jpg");
			await File.WriteAllBytesAsync(path, imagesJpeg[n], ct).ConfigureAwait(continueOnCapturedContext: false);
			paths.Add(path);
		}
		return paths;
	}

	public async Task StopAsync(string taskId)
	{
		AgentTask task = GetTask(taskId);
		AgentTurnLease lease;
		DateTimeOffset? leaseRecordedAt;
		lock (task.Gate)
		{
			lease = task.Lease;
			leaseRecordedAt = task.LeaseRecordedAt;
		}
		if ((object)lease != null && leaseRecordedAt.HasValue)
		{
			DateTimeOffset valueOrDefault = leaseRecordedAt.GetValueOrDefault();
			if (DateTimeOffset.Now - valueOrDefault <= EarlyStopRefundWindow)
			{
				try
				{
					string text = await supabaseAccessToken(lifetime.Token).ConfigureAwait(continueOnCapturedContext: false);
					bool flag = text != null;
					if (flag)
					{
						flag = await api.RefundEarlyStopAsync(text, lease.LeaseId, lifetime.Token).ConfigureAwait(continueOnCapturedContext: false);
					}
					if (flag)
					{
						lock (task.Gate)
						{
							task.DidRefundEarlyStop = true;
						}
						Raise(task, AgentTaskEventKind.EarlyStopRefunded, "Stopped. This didn't count toward your agent messages because you stopped it before it really got to work.");
					}
				}
				catch (Exception ex)
				{
					Log.Warn("Agents", "early-stop refund failed: " + ex.Message);
				}
			}
		}
		if (session.Interrupt(task, TurnOutcome.Interrupted, "Stopped."))
		{
			return;
		}
		CancellationTokenSource turnCts;
		lock (task.Gate)
		{
			turnCts = task.TurnCts;
		}
		turnCts?.Cancel();
		bool flag2 = false;
		lock (task.Gate)
		{
			if (task.IsActive || task.Status == AgentTaskStatus.WaitingForUser)
			{
				task.Status = AgentTaskStatus.Stopped;
				task.ProgressSummary = "Stopped.";
				task.RequiresExtraUsageApproval = false;
				flag2 = true;
			}
		}
		if (flag2)
		{
			Raise(task, AgentTaskEventKind.Stopped, "Stopped.");
			CompleteLeaseInBackground(task, "interrupted");
			RaiseTasksChanged();
		}
	}

	public Task<string> LaunchAsync(AgentLaunchRequest request, CancellationToken ct = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		string clickySlug = request.ClickySlug;
		ClickyInfo clicky = ((clickySlug != null) ? clickys.Get(clickySlug) : null);
		if (request.ClickySlug != null && (object)clicky == null)
		{
			throw new ArgumentException("Unknown Clicky '" + request.ClickySlug + "'.");
		}
		if ((object)clicky == null && request.JobId == null)
		{
			string targetThreadId = request.TargetThreadId;
			if (targetThreadId != null)
			{
				ClickyInfo clickyInfo = clickys.ForThread(targetThreadId);
				clicky = (((object)clickyInfo != null && !clickyInfo.IsArchived) ? clickyInfo : null);
			}
		}
		object obj;
		if ((object)clicky == null)
		{
			string jobId = request.JobId;
			if (jobId == null)
			{
				string targetThreadId2 = request.TargetThreadId;
				obj = ((targetThreadId2 != null) ? Jobs.JobForThread(targetThreadId2) : null);
			}
			else
			{
				obj = Jobs.Job(jobId) ?? throw new ArgumentException("Unknown job '" + jobId + "'.");
			}
		}
		else
		{
			obj = null;
		}
		ClickyJob job = (ClickyJob)obj;
		if ((object)job != null)
		{
			AgentTask existingJobTask = tasks.Values.FirstOrDefault((AgentTask t) => Lock(t, () => t.JobId) == job.Id);
			if (existingJobTask != null)
			{
				lock (existingJobTask.Gate)
				{
					existingJobTask.ProactiveSuggestionId = request.ProactiveSuggestionId;
					existingJobTask.IsDemo = request.UsageContext == "onboarding";
					existingJobTask.IsSentFromItsChat = request.IsHudFollowUp;
				}
				ForgetDeclinedStepsIfUserMessage(existingJobTask.Id, request.LaunchSource);
				Task.Run(async delegate
				{
					_ = 1;
					try
					{
						IReadOnlyList<(string, string)> priorTurns = FollowUpCompanionTurns(request, job.Id);
						if (Lock(existingJobTask, () => existingJobTask.IsActive))
						{
							await SteerAsync(existingJobTask, request.Prompt, request.ImagesJpeg, priorTurns, ct).ConfigureAwait(continueOnCapturedContext: false);
						}
						else
						{
							await StartFollowUpTurnAsync(existingJobTask, request.Prompt, request.ImagesJpeg, request.LaunchSource, request.UsageContext, null, ct, LaunchContextWithBuildsOn(request, job.Id), request.TargetThreadId, request.StartsNewJobConversation, priorTurns).ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					catch (Exception ex)
					{
						Log.Error("Agents", "follow-up to job " + job.Id + " failed", ex);
					}
				}, CancellationToken.None);
				return Task.FromResult(existingJobTask.Id);
			}
		}
		if ((object)clicky != null)
		{
			AgentTask existing = tasks.Values.FirstOrDefault((AgentTask t) => t.ClickySlug == clicky.Slug);
			if (existing != null)
			{
				lock (existing.Gate)
				{
					existing.ProactiveSuggestionId = request.ProactiveSuggestionId;
					existing.IsDemo = request.UsageContext == "onboarding";
					existing.IsSentFromItsChat = request.IsHudFollowUp;
				}
				ForgetDeclinedStepsIfUserMessage(existing.Id, request.LaunchSource);
				Task.Run(async delegate
				{
					_ = 1;
					try
					{
						IReadOnlyList<(string, string)> priorTurns2 = FollowUpCompanionTurns(request, ConversationKey(existing) ?? clicky.ThreadId);
						string text = LaunchContextWithBuildsOn(request, null);
						if (text == null || Lock(existing, () => existing.IsActive))
						{
							await SteerAsync(existing, request.Prompt, request.ImagesJpeg, priorTurns2, ct).ConfigureAwait(continueOnCapturedContext: false);
						}
						else
						{
							await StartFollowUpTurnAsync(existing, request.Prompt, request.ImagesJpeg, request.LaunchSource, request.UsageContext, null, ct, TurnPromptBuilder.ClickyLaunchContext(clicky, foundedForThisTask: false, text.Trim() + " "), null, startsNewJobConversation: false, priorTurns2).ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					catch (Exception ex2)
					{
						Log.Error("Agents", "follow-up to " + clicky.Slug + " failed", ex2);
					}
				}, CancellationToken.None);
				return Task.FromResult(existing.Id);
			}
		}
		string displayPrompt = request.DisplayPrompt;
		if (displayPrompt != null && displayPrompt.Length > 0)
		{
			clickys.RecordDisplayPrompt(request.Prompt, displayPrompt);
		}
		bool isNewJob = false;
		bool flag = false;
		if ((object)clicky == null && (object)job == null)
		{
			string text2 = request.Prompt.Trim();
			ClickyJobStore jobs = Jobs;
			string text3 = request.NewClickyName?.Trim();
			job = jobs.CreateJob((text3 != null && text3.Length > 0) ? text3 : text2, text2, options.ProjectsRoot);
			string targetThreadId3 = request.TargetThreadId;
			if (targetThreadId3 != null)
			{
				Jobs.RecordThreadId(targetThreadId3, job.Id);
				string launchSource = request.LaunchSource;
				bool flag2 = ((launchSource == "text" || launchSource == "voice") ? true : false);
				flag = flag2 && !AgentHarnessThreads.ContinuesOnNewConversationHarness(targetThreadId3) && !session.IsThreadActive(targetThreadId3);
			}
			isNewJob = true;
		}
		AgentTask task = new AgentTask
		{
			LaunchSource = request.LaunchSource,
			LaunchTimestamp = Stopwatch.GetTimestamp(),
			IsNewJobRun = flag
		};
		string text4 = clicky?.ThreadId ?? (((object)job != null && !request.StartsNewJobConversation && !flag) ? (request.TargetThreadId ?? job.ThreadId) : null);
		lock (task.Gate)
		{
			task.UserPrompts.Add(request.DisplayPrompt ?? request.Prompt);
			task.IsRoutineRun = request.LaunchSource == "routine";
			task.IsDemo = request.UsageContext == "onboarding";
			task.IsSentFromItsChat = request.IsHudFollowUp;
			task.ProactiveSuggestionId = request.ProactiveSuggestionId;
			if (text4 == null)
			{
				task.PendingInitialMessageLog = request.DisplayPrompt ?? request.Prompt;
			}
			if ((object)clicky != null)
			{
				task.ClickySlug = clicky.Slug;
				task.ClickyName = clicky.Name;
				task.Title = clicky.Name;
				task.ThreadId = clicky.ThreadId;
			}
			else if ((object)job != null)
			{
				task.JobId = job.Id;
				task.Title = job.Title;
				task.ThreadId = text4;
				task.WorkspacePath = job.FolderPath;
			}
			task.TurnCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
		}
		tasks[task.Id] = task;
		if (text4 != null)
		{
			string threadId = text4;
			LogAgentMessage(task, threadId, "user", request.DisplayPrompt ?? request.Prompt, isFollowUp: true);
		}
		RaiseTasksChanged();
		Raise(task, AgentTaskEventKind.StatusChanged, "Starting");
		Task.Run(() => RunLaunchAsync(task, request, clicky, job, isNewJob), CancellationToken.None);
		return Task.FromResult(task.Id);
	}

	public async Task<string?> TextStartCueAsync(string prompt, bool isFreshThread, CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			string text = await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (text == null)
			{
				return null;
			}
			return await api.TextStartCueAsync(text, prompt, isFreshThread, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			Log.Warn("Agents", "text start cue: " + ex.Message);
			return null;
		}
	}

	public AgentTaskDetail? GetTaskDetail(string taskId)
	{
		if (!tasks.TryGetValue(taskId, out AgentTask t))
		{
			return null;
		}
		AgentTaskDetail agentTaskDetail = Lock(t, () => t.Detail());
		if ((object)agentTaskDetail != null && agentTaskDetail.JobId == null && agentTaskDetail.ClickySlug == null)
		{
			AgentTaskSnapshot snapshot = agentTaskDetail.Snapshot;
			if ((object)snapshot != null)
			{
				string threadId = snapshot.ThreadId;
				if (threadId != null)
				{
					ClickyJob clickyJob = Jobs.JobForThread(threadId);
					if ((object)clickyJob != null)
					{
						return agentTaskDetail with
						{
							JobId = clickyJob.Id
						};
					}
				}
			}
		}
		return agentTaskDetail;
	}

	public void DismissTask(string taskId)
	{
		if (tasks.TryGetValue(taskId, out AgentTask t) && !Lock(t, () => t.IsActive) && tasks.TryRemove(taskId, out AgentTask _))
		{
			RaiseTasksChanged();
		}
	}

	public async Task ApproveExtraUsageAsync(string taskId, bool asManyAsNeeded, CancellationToken ct = default(CancellationToken), bool remembersAlways = true)
	{
		AgentTask task = GetTask(taskId);
		AgentTurnLease lease;
		lock (task.Gate)
		{
			lease = task.Lease;
		}
		if ((object)lease == null)
		{
			throw new InvalidOperationException("This task has no credit lease to continue.");
		}
		string text = await RequireSupabaseTokenAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		AgentTurnLease agentTurnLease;
		try
		{
			agentTurnLease = await api.ContinueLeaseAsync(text, lease.LeaseId, asManyAsNeeded, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (AgentBackendException ex) when (ex.IsPaywall)
		{
			RaisePaywall(ex, presents: true);
			CompanionError companionError = new CompanionError("agent_paywall", "You're out of agent messages.", ex.BodyJson);
			lock (task.Gate)
			{
				task.Error = companionError;
			}
			Raise(task, AgentTaskEventKind.NeedsExtraUsageApproval, companionError.Message);
			throw new AgentPaywallException(companionError);
		}
		if ((object)agentTurnLease == null || agentTurnLease.RequiresExtraEffort)
		{
			lock (task.Gate)
			{
				if ((object)agentTurnLease != null)
				{
					task.Lease = agentTurnLease;
				}
			}
			Raise(task, AgentTaskEventKind.NeedsExtraUsageApproval, "Still needs approval.");
			return;
		}
		Raise(task, AgentTaskEventKind.ExtraUsageApproved, null);
		if (asManyAsNeeded && remembersAlways && !options.AutoApproveExtraUsage)
		{
			options.AutoApproveExtraUsage = true;
			this.AutoApproveExtraUsageChanged?.Invoke(obj: true);
		}
		string contextOverride = (asManyAsNeeded ? "The user approved the extra usage to run this HeyClicky agent task to completion. Continue the same task from where it stopped." : "The user approved continuing this HeyClicky agent task once. Continue the same task from where it stopped.");
		await StartFollowUpTurnAsync(task, "Keep going.", null, "extra_effort", "companion", agentTurnLease, ct, contextOverride).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task PrewarmAsync(CancellationToken ct = default(CancellationToken))
	{
		SyncWorkOwnerStamp();
		if (options.EnableRoutines)
		{
			routines.Start();
		}
		RestorePendingAgentRequestsFromLastRun();
		StepWatcher.Reconcile();
		NameJobsWithoutNicknames();
		return Task.Run(async delegate
		{
			try
			{
				await EnsureRuntimeReadyAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Warn("Agents", "prewarm failed: " + ex.Message);
			}
			try
			{
				await AgentHarnessRunnerProcess.EnsureRunningAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex2)
			{
				Log.Warn("Harness", "runner prewarm failed: " + ex2.Message);
			}
		}, ct);
	}

	public Task SignOutAsync()
	{
		routines.Stop();
		foreach (AgentTask item in tasks.Values.ToList())
		{
			session.Interrupt(item, TurnOutcome.Interrupted, "Signed out.");
		}
		Interlocked.Increment(ref accountGeneration);
		Interlocked.Increment(ref composioGeneration);
		tokens.Invalidate();
		clickys.ClearHiddenThreads();
		composio = null;
		composioFetchedAt = default(DateTimeOffset);
		composioRefetchedForConnectedApps = null;
		lastIntegrationCatalog = null;
		tasks.Clear();
		RemoveJobsForSignOut();
		RaiseTasksChanged();
		return Task.CompletedTask;
	}

	public void NotifyResumedFromSleep()
	{
		routines.HandleFullWake("resume");
	}

	public void NotifySystemSleeping()
	{
		routines.NoteSystemSleeping();
	}

	public void NotifyDisplayPower(bool isOn)
	{
		routines.NoteDisplayPower(isOn);
	}

	public void SetCustomConnectors(IReadOnlyList<CustomConnectorRegistration> connectors)
	{
		customConnectors = connectors;
		InvalidateConnectedApps();
	}

	public void InvalidateConnectedApps()
	{
		composioFetchedAt = default(DateTimeOffset);
		composioRefetchedForConnectedApps = null;
		configDirty = true;
		Interlocked.Increment(ref composioGeneration);
	}

	private void RefetchComposioIfConnectedAppsChanged()
	{
		if (!ComposioSessionIsStale)
		{
			HashSet<string> sessionToolkits = (composio?.Toolkits ?? Array.Empty<string>()).Select((string t) => t.ToLowerInvariant()).ToHashSet();
			if (ComposioSessionReuse.ShouldRefetchForConnectedApps(ComposioSessionReuse.ConnectedToolkits(lastIntegrationCatalog), sessionToolkits, ref composioRefetchedForConnectedApps))
			{
				Log.Info("Agents", "composio session: the integrations list's connected apps differ from the session's; fetching a fresh one");
				composioFetchedAt = default(DateTimeOffset);
				configDirty = true;
			}
		}
	}

	private async Task<T> WithSessionTokenAsync<T>(Func<string, Task<T>> call, CancellationToken ct)
	{
		T result = default(T);
		int num;
		Func<string, Task<T>> func;
		try
		{
			func = call;
			result = await func(await tokens.GetAsync(ct).ConfigureAwait(continueOnCapturedContext: false)).ConfigureAwait(continueOnCapturedContext: false);
			return result;
		}
		catch (Exception ex) when (AgentWorkerApi.IsUnauthorized(ex))
		{
			num = 1;
		}
		if (num != 1)
		{
			return result;
		}
		func = call;
		return await func(await tokens.GetAsync(ct, forceRefresh: true).ConfigureAwait(continueOnCapturedContext: false)).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<AgentIntegrationCatalog> ListIntegrationsAsync(CancellationToken ct = default(CancellationToken))
	{
		return lastIntegrationCatalog = await WithSessionTokenAsync((string token) => api.ListIntegrationsAsync(token, ct), ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task<string> ConnectIntegrationAsync(string toolkit, CancellationToken ct = default(CancellationToken))
	{
		return WithSessionTokenAsync((string token) => api.ConnectIntegrationAsync(token, toolkit, ct), ct);
	}

	public async Task DisconnectIntegrationAsync(string toolkit, string? connectedAccountId = null, CancellationToken ct = default(CancellationToken))
	{
		await WithSessionTokenAsync(async delegate(string token)
		{
			await api.DisconnectIntegrationAsync(token, toolkit, connectedAccountId, ct).ConfigureAwait(continueOnCapturedContext: false);
			return true;
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		InvalidateConnectedApps();
	}

	public async Task LabelIntegrationAccountAsync(string toolkit, string connectedAccountId, string label, CancellationToken ct = default(CancellationToken))
	{
		await WithSessionTokenAsync(async delegate(string token)
		{
			await api.LabelIntegrationAccountAsync(token, toolkit, connectedAccountId, label, ct).ConfigureAwait(continueOnCapturedContext: false);
			return true;
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		InvalidateConnectedApps();
	}

	public async Task<double?> LeaseBilledCostAsync(string leaseId, CancellationToken ct = default(CancellationToken))
	{
		string text = await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false);
		if (text == null)
		{
			return null;
		}
		return (await api.LeaseStatusAsync(text, leaseId, ct).ConfigureAwait(continueOnCapturedContext: false))?.CostUsd;
	}

	public void SetProjectsRoot(string path)
	{
		if (!string.Equals(options.ProjectsRoot, path, StringComparison.OrdinalIgnoreCase))
		{
			Directory.CreateDirectory(path);
			options.ProjectsRoot = path;
			clickys.ProjectsRoot = path;
			configDirty = true;
			Log.Info("Agents", "agent folder: " + path);
		}
	}

	public ClickyInfo? FindClicky(string slug)
	{
		return clickys.Get(slug);
	}

	public Task<string> GetAgentSessionTokenAsync(bool forceRefresh = false, CancellationToken ct = default(CancellationToken))
	{
		return tokens.GetAsync(ct, forceRefresh);
	}

	public bool IsClickyBusy(string slug)
	{
		return tasks.Values.Any((AgentTask t) => t.ClickySlug == slug && Lock(t, () => t.IsActive));
	}

	public string? WorkspacePath(string clickySlug)
	{
		ClickyInfo clickyInfo = clickys.Get(clickySlug);
		if ((object)clickyInfo == null)
		{
			return null;
		}
		return clickys.WorkspacePath(clickyInfo.Slug);
	}

	public ClickyInfo CreateClicky(string name, string roleText, string oneLiner)
	{
		ClickyInfo clickyInfo = clickys.Found(name, roleText, oneLiner);
		clickys.EnsureWorkspace(clickyInfo);
		RaiseTasksChanged();
		return clickyInfo;
	}

	public bool KnowsClicky(string slug)
	{
		return (object)clickys.Get(slug) != null;
	}

	public async Task<(IReadOnlyList<AgentThreadSummary> Threads, string? NextCursor)> ListThreadsAsync(int limit = 50, string? cursor = null, string? cwd = null, CancellationToken ct = default(CancellationToken))
	{
		List<CodexRolloutHistory.RolloutThread> list = (await Task.Run(() => CodexRolloutHistory.List(options.CodexHome), ct).ConfigureAwait(continueOnCapturedContext: false)).Where((CodexRolloutHistory.RolloutThread t) => !clickys.IsThreadHidden(t.ThreadId) && (cwd == null || string.Equals(t.Cwd?.TrimEnd('\\', '/'), cwd.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))).ToList();
		int result;
		int num = ((int.TryParse(cursor, out result) && result > 0) ? result : 0);
		List<CodexRolloutHistory.RolloutThread> list2 = list.Skip(num).Take(Math.Max(1, limit)).ToList();
		List<AgentThreadSummary> list3 = new List<AgentThreadSummary>();
		foreach (CodexRolloutHistory.RolloutThread item in list2)
		{
			string text = PreviewText(item.FirstUserMessage);
			text = clickys.DisplayFor(text) ?? text;
			ClickyInfo clickyInfo = clickys.ForThread(item.ThreadId);
			list3.Add(new AgentThreadSummary(item.ThreadId, clickys.TitleForThread(item.ThreadId) ?? clickyInfo?.Name, text, item.CreatedAt, item.UpdatedAt, "idle", item.Cwd, clickyInfo?.Slug));
		}
		if (cursor == null)
		{
			StartPersonaNameBackfillOnce(list3);
		}
		int num2 = num + list2.Count;
		return (Threads: list3, NextCursor: (num2 < list.Count) ? num2.ToString(CultureInfo.InvariantCulture) : null);
	}

	private void StartPersonaNameBackfillOnce(IReadOnlyList<AgentThreadSummary> threads)
	{
		if (Interlocked.Exchange(ref personaNameBackfillStarted, 1) == 1)
		{
			return;
		}
		List<AgentThreadSummary> unnamed = threads.Where((AgentThreadSummary t) => clickys.TitleForThread(t.ThreadId) == null && (object)clickys.ForThread(t.ThreadId) == null && (string.IsNullOrWhiteSpace(t.Name) || t.Name.Trim() == "Untitled agent thread") && !string.IsNullOrWhiteSpace(t.Preview)).Take(15).ToList();
		if (unnamed.Count == 0)
		{
			return;
		}
		Task.Run(async delegate
		{
			int named = 0;
			foreach (AgentThreadSummary thread in unnamed)
			{
				try
				{
					string basis = thread.Preview;
					try
					{
						string text = (await ReadThreadAsync(thread.ThreadId, lifetime.Token).ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault()?.UserMessages.FirstOrDefault();
						if (text != null && text.Length > 0)
						{
							basis = text;
						}
					}
					catch (Exception ex)
					{
						Log.Warn("Agents", "persona backfill: reading " + thread.ThreadId + ": " + ex.Message);
					}
					basis = basis.Trim();
					if (basis.Length >= 12)
					{
						if (basis.Length > 600)
						{
							basis = basis.Substring(0, 600);
						}
						string text2 = await supabaseAccessToken(lifetime.Token).ConfigureAwait(continueOnCapturedContext: false);
						if (text2 == null)
						{
							break;
						}
						string text3 = await api.ThreadTitleAsync(text2, basis, lifetime.Token, TimeSpan.FromSeconds(20L)).ConfigureAwait(continueOnCapturedContext: false);
						if (!string.IsNullOrWhiteSpace(text3))
						{
							clickys.SetThreadTitle(thread.ThreadId, text3.Trim());
							named++;
						}
					}
				}
				catch (Exception ex2)
				{
					Log.Warn("Agents", "persona backfill stopped: " + ex2.Message);
					break;
				}
			}
			if (named == 0)
			{
				return;
			}
			Log.Info("Agents", $"persona backfill named {named} thread(s)");
			try
			{
				this.ThreadHistoryChanged?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex3)
			{
				Log.Warn("Agents", "ThreadHistoryChanged handler: " + ex3.Message);
			}
		});
	}

	public Task<IReadOnlyList<AgentTranscriptTurn>> ReadThreadAsync(string threadId, CancellationToken ct = default(CancellationToken))
	{
		if (!AgentHarnessPreferences.IsHarnessThread(threadId))
		{
			return Task.Run(() => RolloutTranscriptTurns(threadId), ct);
		}
		return Task.FromResult((IReadOnlyList<AgentTranscriptTurn>)Array.Empty<AgentTranscriptTurn>());
	}

	private IReadOnlyList<AgentTranscriptTurn> RolloutTranscriptTurns(string threadId)
	{
		List<AgentTranscriptTurn> list = new List<AgentTranscriptTurn>();
		foreach (CodexRolloutHistory.RolloutTurn item in CodexRolloutHistory.ReadTurns(options.CodexHome, threadId))
		{
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			List<string> list5 = new List<string>();
			foreach (string userMessage in item.UserMessages)
			{
				string text = DisplayUserText(userMessage);
				list2.Add(clickys.DisplayFor(text) ?? text);
			}
			foreach (string reply in item.Replies)
			{
				StructuredFinalReply structuredFinalReply = StructuredFinalReply.Parse(reply);
				if (structuredFinalReply.Text.Length > 0)
				{
					list3.Add(structuredFinalReply.Text);
				}
				list4.AddRange(structuredFinalReply.NextActions.Except(list4));
				list5.AddRange(structuredFinalReply.Artifacts.Except(list5));
			}
			list.Add(new AgentTranscriptTurn(null, list2, item.ProgressLines.ToList(), list3, list4, list5, "completed", null, item.StartedAt, item.FinishedAt));
		}
		return list;
	}

	public Task ArchiveThreadAsync(string threadId, CancellationToken ct = default(CancellationToken))
	{
		if (session.IsThreadActive(threadId))
		{
			throw new InvalidOperationException("Stop the agent before archiving its conversation.");
		}
		HideThreadFromHistory(threadId);
		ClickyInfo clickyInfo = clickys.ForThread(threadId);
		if ((object)clickyInfo != null && clickyInfo.ThreadId == threadId)
		{
			clickys.Archive(clickyInfo.Slug);
		}
		foreach (AgentTask item in tasks.Values.Where((AgentTask t) => t.ThreadId == threadId).ToList())
		{
			tasks.TryRemove(item.Id, out AgentTask _);
		}
		RaiseTasksChanged();
		return Task.CompletedTask;
	}

	public bool HideThreadFromHistory(string threadId)
	{
		if (!string.IsNullOrWhiteSpace(threadId))
		{
			return clickys.HideThread(threadId);
		}
		return false;
	}

	public bool IsThreadHidden(string threadId)
	{
		return clickys.IsThreadHidden(threadId);
	}

	public void ArchiveClicky(string slug)
	{
		if (tasks.Values.Any((AgentTask t) => t.ClickySlug == slug && Lock(t, () => t.IsActive)))
		{
			throw new InvalidOperationException("Stop the agent before archiving it.");
		}
		clickys.Archive(slug);
		foreach (AgentTask item in tasks.Values.Where((AgentTask t) => t.ClickySlug == slug).ToList())
		{
			tasks.TryRemove(item.Id, out AgentTask _);
		}
		RaiseTasksChanged();
	}

	public string OpenThread(string threadId)
	{
		AgentTask agentTask = tasks.Values.FirstOrDefault((AgentTask t) => t.ThreadId == threadId);
		if (agentTask != null)
		{
			return agentTask.Id;
		}
		ClickyInfo clickyInfo = clickys.ForThread(threadId);
		ClickyJob clickyJob = (((object)clickyInfo == null) ? Jobs.JobForThread(threadId) : null);
		AgentTask agentTask2 = new AgentTask
		{
			ThreadId = threadId,
			Status = AgentTaskStatus.Finished,
			ProgressSummary = ""
		};
		agentTask2.Title = clickys.TitleForThread(threadId) ?? clickyInfo?.Name ?? clickyJob?.Title ?? agentTask2.Title;
		agentTask2.ClickySlug = clickyInfo?.Slug;
		agentTask2.ClickyName = clickyInfo?.Name;
		agentTask2.JobId = clickyJob?.Id;
		agentTask2.WorkspacePath = (((object)clickyInfo != null) ? clickys.WorkspacePath(clickyInfo.Slug) : clickyJob?.FolderPath);
		tasks[agentTask2.Id] = agentTask2;
		RaiseTasksChanged();
		return agentTask2.Id;
	}

	private async Task PrepareRuntimeLockedAsync(CancellationToken ct)
	{
		Stopwatch sw = Stopwatch.StartNew();
		int account = Volatile.Read(in accountGeneration);
		string token = await tokens.GetAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		await RefreshConnectorTokensBeforePrepareAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		if (SignedOutSince())
		{
			return;
		}
		for (int attempt = 0; attempt < 2; attempt++)
		{
			if (!ComposioSessionIsStale)
			{
				break;
			}
			int generation = Volatile.Read(in composioGeneration);
			var (composioFetchKind, composioMcpConfiguration, ex) = await api.ComposioSessionAsync(token, ct).ConfigureAwait(continueOnCapturedContext: false);
			Log.Info("Agents", $"composio session: {composioFetchKind}{(((object)composioMcpConfiguration == null) ? "" : (" toolkits=[" + string.Join(",", composioMcpConfiguration.Toolkits) + "]"))}");
			if (SignedOutSince())
			{
				return;
			}
			configDirty = true;
			if (composioFetchKind == AgentWorkerApi.ComposioFetchKind.Unreachable)
			{
				composioUnreachable = (object)composio == null;
				Log.Warn("Agents", "composio unreachable: " + ex?.Message);
				break;
			}
			composio = composioMcpConfiguration;
			composioUnreachable = false;
			composioFetchedAt = ((generation == Volatile.Read(in composioGeneration)) ? DateTimeOffset.Now : default(DateTimeOffset));
		}
		if (!SignedOutSince() && (configDirty || !runtimeFilesPrepared))
		{
			home.PrepareRuntimeConfiguration(composio, composioUnreachable, customConnectors);
			configDirty = false;
			if (!runtimeFilesPrepared)
			{
				runtimeReadyMs = sw.ElapsedMilliseconds;
			}
			runtimeFilesPrepared = true;
		}
		bool SignedOutSince()
		{
			return account != Volatile.Read(in accountGeneration);
		}
	}

	private async Task RunLaunchAsync(AgentTask task, AgentLaunchRequest request, ClickyInfo? clicky, ClickyJob? job, bool isNewJob)
	{
		CancellationToken ct = task.TurnCts.Token;
		try
		{
			Task ready = EnsureRuntimeReadyAsync(ct);
			Task<string?> memory = FetchMemoryAsync(ct);
			AgentTask task2;
			string jobContext;
			IReadOnlyList<byte[]> imagesJpeg;
			IReadOnlyList<string> attachedFilePaths;
			string usageContext;
			string launchSource;
			string launchContext;
			bool autoApproveExtraUsage;
			IReadOnlyList<(string User, string Assistant)> priorTurns;
			bool isGeneratedTaskBrief;
			if ((object)job != null)
			{
				jobContext = LaunchContextWithBuildsOn(request, job.Id) ?? "";
				string threadId;
				lock (task.Gate)
				{
					threadId = task.ThreadId;
				}
				if (!isNewJob)
				{
					(string, string) tuple = PlanJobTurn(job, task, request.Prompt.Trim(), request.LaunchSource, hasPreauthorizedLease: false, threadId, request.StartsNewJobConversation);
					lock (task.Gate)
					{
						task.ThreadId = tuple.Item1;
					}
					jobContext += tuple.Item2;
				}
				else
				{
					if (Lock(task, () => task.IsNewJobRun))
					{
						lock (task.Gate)
						{
							task.PendingUserStep = null;
						}
						bool hasPreviousNote = ClickyWorkFolderFiles.RetireNote(job.FolderPath);
						jobContext = jobContext + "\n" + JobContinuationContext(job, hasPreviousNote);
						Jobs.BeginRun(job.Id, request.Prompt.Trim());
					}
					jobContext = jobContext + "\n" + ClickyJobBrief.WorkFolderContext;
					Jobs.MarkWorking(job.Id);
				}
				Directory.CreateDirectory(Path.Combine(job.FolderPath, "output"));
				Directory.CreateDirectory(Path.Combine(job.FolderPath, "tmp"));
				await ready.ConfigureAwait(continueOnCapturedContext: false);
				task2 = task;
				string prompt = request.Prompt;
				imagesJpeg = request.ImagesJpeg;
				attachedFilePaths = request.AttachedFilePaths;
				launchContext = jobContext.TrimStart('\n');
				launchSource = request.LaunchSource;
				usageContext = request.UsageContext;
				isGeneratedTaskBrief = request.IsGeneratedTaskBrief;
				priorTurns = (isNewJob ? FreshLaunchCompanionTurns(request) : FollowUpCompanionTurns(request, job.Id));
				autoApproveExtraUsage = request.AutoApproveExtraUsage;
				await RunTurnAsync(task2, new TurnSpec(prompt, imagesJpeg, attachedFilePaths, launchContext, launchSource, usageContext, isGeneratedTaskBrief, priorTurns, null, autoApproveExtraUsage, await memory.ConfigureAwait(continueOnCapturedContext: false)), ct).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			bool founded = false;
			if ((object)clicky == null)
			{
				string text = request.NewClickyName?.Trim();
				string text2 = ((text == null || text.Length <= 0) ? (await TitleAsync(request.Prompt, ct).ConfigureAwait(continueOnCapturedContext: false)) : text);
				string name = text2;
				string oneLiner = request.NewClickyOneLiner ?? OneLinerFrom(request.Prompt);
				clicky = clickys.Found(name, request.NewClickyRole, oneLiner);
				try
				{
					this.ClickyFounded?.Invoke(clicky.Slug);
				}
				catch (Exception ex)
				{
					Log.Warn("Agents", "ClickyFounded handler: " + ex.Message);
				}
				founded = true;
				lock (task.Gate)
				{
					task.ClickySlug = clicky.Slug;
					task.ClickyName = clicky.Name;
					task.Title = clicky.Name;
					task.ThreadId = clicky.ThreadId;
				}
				Raise(task, AgentTaskEventKind.TitleChanged, clicky.Name);
				RaiseTasksChanged();
			}
			string workspacePath = clickys.EnsureWorkspace(clicky);
			lock (task.Gate)
			{
				task.WorkspacePath = workspacePath;
			}
			await ready.ConfigureAwait(continueOnCapturedContext: false);
			string text3 = LaunchContextWithBuildsOn(request, null);
			string text4 = ((text3 != null) ? (text3 + " ") : null);
			string text5 = LeaveOldHarnessThread(task, request.LaunchSource, preauthorized: false);
			if (text5 != null)
			{
				text4 = text4 + ClickyFreshConversationContext(text5) + "\n";
			}
			string text6 = TurnPromptBuilder.ClickyLaunchContext(clicky, founded, text4, text5 != null);
			task2 = task;
			jobContext = request.Prompt;
			imagesJpeg = request.ImagesJpeg;
			attachedFilePaths = request.AttachedFilePaths;
			usageContext = text6;
			launchSource = request.LaunchSource;
			launchContext = request.UsageContext;
			autoApproveExtraUsage = request.IsGeneratedTaskBrief;
			string threadId2 = clicky.ThreadId;
			priorTurns = ((threadId2 != null) ? FollowUpCompanionTurns(request, threadId2) : FreshLaunchCompanionTurns(request));
			isGeneratedTaskBrief = request.AutoApproveExtraUsage;
			await RunTurnAsync(task2, new TurnSpec(jobContext, imagesJpeg, attachedFilePaths, usageContext, launchSource, launchContext, autoApproveExtraUsage, priorTurns, null, isGeneratedTaskBrief, await memory.ConfigureAwait(continueOnCapturedContext: false)), ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex2)
		{
			FailLaunch(task, ex2);
		}
	}

	private Task StartFollowUpTurnAsync(AgentTask task, string text, IReadOnlyList<byte[]>? images, string source, string usageContext, AgentTurnLease? preauthorizedLease, CancellationToken ct, string? contextOverride = null, string? requestedThreadId = null, bool startsNewJobConversation = false, IReadOnlyList<(string User, string Assistant)>? priorTurns = null)
	{
		ClickyInfo clicky;
		ClickyJob job;
		lock (task.Gate)
		{
			if (task.IsActive)
			{
				throw new InvalidOperationException("This job is still working on its current task.");
			}
			string clickySlug = task.ClickySlug;
			clicky = ((clickySlug != null) ? clickys.Get(clickySlug) : null);
			string jobId = task.JobId;
			job = ((jobId != null) ? Jobs.Job(jobId) : null);
		}
		string text2 = Lock(task, () => task.ThreadId);
		if (text2 != null)
		{
			ClickyPendingAgentRequestStore.Remove(text2);
			resultCards.TryRemove(text2, out ClickyAnswer _);
		}
		string jobContext = "";
		string threadId = null;
		if ((object)job != null)
		{
			(threadId, jobContext) = PlanJobTurn(job, task, text.Trim(), source, (object)preauthorizedLease != null, requestedThreadId ?? Lock(task, () => task.ThreadId), startsNewJobConversation);
		}
		lock (task.Gate)
		{
			task.ResetForNewTurn(text);
			task.LaunchSource = source;
			task.LaunchTimestamp = Stopwatch.GetTimestamp();
			task.TurnCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
			if ((object)job != null)
			{
				task.ThreadId = threadId;
				task.WorkspacePath = job.FolderPath;
			}
		}
		string leftThread = (((object)clicky != null) ? LeaveOldHarnessThread(task, source, (object)preauthorizedLease != null) : null);
		RaiseTasksChanged();
		Raise(task, AgentTaskEventKind.StatusChanged, "Starting");
		if (source != "extra_effort")
		{
			string threadId2;
			lock (task.Gate)
			{
				threadId2 = task.ThreadId;
			}
			if (threadId2 == null)
			{
				lock (task.Gate)
				{
					task.PendingInitialMessageLog = text;
				}
			}
			else
			{
				LogAgentMessage(task, threadId2, "user", text, isFollowUp: true);
			}
		}
		CancellationToken turnCt = task.TurnCts.Token;
		Task.Run(async delegate
		{
			_ = 2;
			try
			{
				Task<string?> memory = FetchMemoryAsync(turnCt);
				await EnsureRuntimeReadyAsync(turnCt).ConfigureAwait(continueOnCapturedContext: false);
				string text3 = contextOverride ?? (((object)clicky != null) ? TurnPromptBuilder.ClickyLaunchContext(clicky, foundedForThisTask: false, (leftThread == null) ? null : (ClickyFreshConversationContext(leftThread) + "\n"), leftThread != null) : (((object)job != null) ? "" : "The user sent a follow-up to this HeyClicky agent conversation."));
				text3 = (text3 + jobContext).TrimStart('\n');
				if ((object)clicky != null)
				{
					lock (task.Gate)
					{
						AgentTask agentTask = task;
						if (agentTask.WorkspacePath == null)
						{
							agentTask.WorkspacePath = clickys.EnsureWorkspace(clicky);
						}
					}
				}
				AgentTask task2 = task;
				string prompt = text;
				IReadOnlyList<byte[]> images2 = images;
				string launchContext = text3;
				string launchSource = source;
				string usageContext2 = usageContext;
				bool isGeneratedBrief = source == "extra_effort";
				IReadOnlyList<(string User, string Assistant)> priorTurns2 = priorTurns;
				AgentTurnLease preauthorizedLease2 = preauthorizedLease;
				string text4 = await memory.ConfigureAwait(continueOnCapturedContext: false);
				await RunTurnAsync(task2, new TurnSpec(prompt, images2, null, launchContext, launchSource, usageContext2, isGeneratedBrief, priorTurns2, preauthorizedLease2, AutoApproveExtraUsage: false, text4), turnCt).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				FailLaunch(task, ex);
			}
		}, CancellationToken.None);
		return Task.CompletedTask;
	}

	private async Task RunTurnAsync(AgentTask task, TurnSpec spec, CancellationToken ct)
	{
		if (!slots.Wait(0))
		{
			SetProgress(task, "Waiting for another agent to finish…");
			await slots.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		lock (task.Gate)
		{
			task.HoldsSlot = true;
		}
		try
		{
			string clientTurnId = Guid.NewGuid().ToString("D").ToUpperInvariant();
			AgentTurnLease lease = spec.PreauthorizedLease;
			string threadId;
			lock (task.Gate)
			{
				threadId = task.ThreadId;
			}
			if ((object)lease == null)
			{
				string text = await RequireSupabaseTokenAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				try
				{
					lease = await api.RecordAgentLaunchAsync(text, threadId, clientTurnId, task.NumericId, threadId != null, spec.LaunchSource, spec.AutoApproveExtraUsage || options.AutoApproveExtraUsage, spec.UsageContext, ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (AgentBackendException ex) when (ex.IsPaywall)
				{
					bool flag;
					lock (task.Gate)
					{
						flag = task.IsRoutineRun || spec.LaunchSource == "routine";
					}
					RaisePaywall(ex, !flag);
					throw new AgentPaywallException(new CompanionError("agent_paywall", "Oops, you're out of tokens! Beef up HeyClicky to continue.", ex.BodyJson));
				}
				lock (task.Gate)
				{
					task.LeaseRecordedAt = DateTimeOffset.Now;
				}
			}
			lock (task.Gate)
			{
				task.Lease = lease;
			}
			ct.ThrowIfCancellationRequested();
			string workspace;
			lock (task.Gate)
			{
				workspace = task.WorkspacePath;
			}
			string cwd = workspace ?? options.ProjectsRoot;
			bool flag2 = threadId == null;
			(AgentHarness Harness, AgentHarnessPlace Place) harness = AgentHarnessThreads.Choice(threadId);
			bool flag3 = threadId != null && !AgentHarnessPreferences.RuntimeChoice(threadId).HasValue && !AgentHarnessThreads.TakenOverBy(threadId).HasValue;
			if (threadId == null)
			{
				threadId = AgentHarnessPreferences.ThreadIdPrefix(harness.Harness, harness.Place) + Guid.NewGuid().ToString("D").ToLowerInvariant();
				lock (task.Gate)
				{
					task.ThreadId = threadId;
				}
				BindNewThread(task, threadId);
				string deliveredModelInstructions = home.DeliveredModelInstructions;
				if (deliveredModelInstructions != null)
				{
					clickys.MarkThreadStartedWithInstructions(threadId, deliveredModelInstructions);
				}
				clickys.SetThreadTitle(threadId, task.Title);
				RaiseTasksChanged();
			}
			else if (flag3)
			{
				Log.Info("Harness", "[harness] " + harness.Harness.DisplayName() + " takes over conversation " + threadId);
				string deliveredModelInstructions2 = home.DeliveredModelInstructions;
				if (deliveredModelInstructions2 != null)
				{
					clickys.MarkThreadStartedWithInstructions(threadId, deliveredModelInstructions2);
				}
			}
			bool startsHarnessConversation = flag2 || flag3;
			string launchContext = (flag3 ? (TakeoverContext(threadId, task) + "\n" + spec.LaunchContext) : spec.LaunchContext);
			FlushPendingInitialMessageLog(task, threadId);
			List<string> list = await WriteTaskImagesAsync(task, spec.Images, "screenshot", ct).ConfigureAwait(continueOnCapturedContext: false);
			string jobId;
			lock (task.Gate)
			{
				jobId = task.JobId;
			}
			bool flag4 = ClickyJobsPreferences.IsEnabled && jobId != null;
			if (flag4)
			{
				string text2 = LiveStepsKey(task);
				if (text2 != null)
				{
					LiveSteps.Clear(text2);
				}
			}
			string text3 = (flag4 ? LearnedHowTos.PromptSection(spec.Prompt) : "");
			bool flag5 = StandingContextLedger.NeedsCarrying(StandingContextLedger.Section.LearnedHowTos, text3, startsHarnessConversation ? null : threadId);
			string prompt = spec.Prompt;
			IReadOnlyList<string>? userAttachedFilePaths = spec.AttachedFiles ?? Array.Empty<string>();
			string? userMemoryMarkdown = spec.UserMemory;
			string clickySlug = task.ClickySlug;
			IReadOnlyList<string> routineLines;
			if (clickySlug == null)
			{
				if (jobId != null)
				{
					string jobId2 = jobId;
					routineLines = routines.PromptLinesForJob(jobId2);
				}
				else
				{
					IReadOnlyList<string> readOnlyList = Array.Empty<string>();
					routineLines = readOnlyList;
				}
			}
			else
			{
				routineLines = routines.PromptLines(clickySlug);
			}
			string userMessage = TurnPromptBuilder.Build(new TurnPromptBuilder.Input(launchContext, prompt, list, userAttachedFilePaths, workspace, userMemoryMarkdown, routineLines, spec.IsGeneratedBrief, spec.LaunchSource, startsHarnessConversation ? null : clickys.InstructionsToCarry(threadId, home.DeliveredModelInstructions), spec.PriorTurns)
			{
				IsJob = flag4,
				UserProfileFilePath = (flag4 ? ClickyHelperFiles.WriteUserProfile(spec.UserMemory) : null),
				LearnedHowTosSection = (flag5 ? text3 : ""),
				LiveNoteSection = (flag4 ? "\nIMPORTANT — THIS JOB'S PLAN\nYour plan tool (update_plan) is the job's progress bar: the user watches it while you work. First thing, in the same response as your first searches, call it with 3 to 5 steps of a few words each (under 40 characters: the card has one short line for each). The steps are the stages of the work in the order they will happen, each one a stage the user would recognize (\"Find where each is sold\", \"Check stock in San Francisco\", \"Compare the prices\", \"Build the PDF\"), never your own housekeeping (reading notes, files or memory about them, setting up, saving). Things you look into side by side go in one step together, so every step finishes before the next one starts. Keep exactly one step in progress; when it is done, mark it completed and the next one in progress. Never send an update on its own: put it in the same response as your next search, read or command. A long step (building a file, checking its pages) stays in progress while you're on it. Don't tick the last step: HeyClicky ticks it when your answer arrives. On a later message, plan just that message's work. There is no note to write: the answer goes in your final message." : ClickyLiveNoteSection(task, spec.Prompt, spec.LaunchSource, spec.IsGeneratedBrief, threadId, threadIsActive: false)),
				IsClickyDriveAvailable = (IsClickyDriveAvailable?.Invoke() ?? false)
			});
			if (flag5)
			{
				StandingContextLedger.RecordCarried(new Dictionary<StandingContextLedger.Section, string> { [StandingContextLedger.Section.LearnedHowTos] = text3 }, threadId);
			}
			(string, string)? obj;
			if (tokens.AllowsDeveloperModelSelection)
			{
				string clickySlug2 = task.ClickySlug;
				if (clickySlug2 != null)
				{
					obj = DeveloperModelSelection?.Invoke(clickySlug2);
					goto IL_0910;
				}
			}
			obj = null;
			goto IL_0910;
			IL_0910:
			(string, string)? developerModel = obj;
			lock (task.Gate)
			{
				task.DeveloperModel = developerModel;
			}
			if (developerModel.HasValue)
			{
				(string, string) valueOrDefault = developerModel.GetValueOrDefault();
				Log.Info("Agents", $"task {task.Id}: developer model {valueOrDefault.Item1} / {valueOrDefault.Item2}");
			}
			TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
			lock (task.Gate)
			{
				task.TurnCompletion = taskCompletionSource;
				task.Status = AgentTaskStatus.Running;
				task.ProgressSummary = "Sending your message…";
			}
			Raise(task, AgentTaskEventKind.StatusChanged, "Running");
			RaiseTasksChanged();
			session.StartHarnessTurn(harness, task, threadId, startsHarnessConversation, userMessage, list, cwd, LeaseMetadata(lease, task, threadId)?.ToDictionary((KeyValuePair<string, JsonNode> kv) => kv.Key, (KeyValuePair<string, JsonNode> kv) => kv.Value?.ToString() ?? ""), HarnessTurnContextAsync, ct);
			lock (task.Gate)
			{
				task.TurnStartedTimestamp = Stopwatch.GetTimestamp();
				launchToTurnStartedMs = (long)Stopwatch.GetElapsedTime(task.LaunchTimestamp).TotalMilliseconds;
			}
			Log.Info("Agents", $"task {task.Id} turn started on thread {threadId} ({launchToTurnStartedMs} ms after launch)");
			using CancellationTokenSource leasePoll = (((object)lease == null) ? null : CancellationTokenSource.CreateLinkedTokenSource(ct));
			if (leasePoll != null)
			{
				PollLeaseAsync(task, lease, leasePoll.Token);
			}
			await taskCompletionSource.Task.ConfigureAwait(continueOnCapturedContext: false);
			leasePoll?.Cancel();
		}
		finally
		{
			bool holdsSlot;
			lock (task.Gate)
			{
				holdsSlot = task.HoldsSlot;
				task.HoldsSlot = false;
			}
			if (holdsSlot)
			{
				slots.Release();
			}
		}
	}

	private void BindNewThread(AgentTask task, string threadId)
	{
		string clickySlug;
		string jobId;
		lock (task.Gate)
		{
			clickySlug = task.ClickySlug;
			jobId = task.JobId;
		}
		if (clickySlug != null)
		{
			clickys.BindThread(clickySlug, threadId);
		}
		else if (jobId != null)
		{
			Jobs.RecordThreadId(threadId, jobId);
		}
	}

	private async Task PollLeaseAsync(AgentTask task, AgentTurnLease lease, CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(3000, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			try
			{
				string text = await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false);
				if (text == null)
				{
					continue;
				}
				AgentTurnLease agentTurnLease = await api.LeaseStatusAsync(text, lease.LeaseId, ct).ConfigureAwait(continueOnCapturedContext: false);
				if ((object)agentTurnLease == null)
				{
					continue;
				}
				lock (task.Gate)
				{
					task.Lease = agentTurnLease;
				}
				if (!agentTurnLease.RequiresExtraEffort)
				{
					continue;
				}
				lock (task.Gate)
				{
					task.RequiresExtraUsageApproval = true;
				}
				session.Interrupt(task, TurnOutcome.ExtraEffortRequired, "This task requires extra effort");
				break;
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex3)
			{
				Log.Warn("Agents", "lease status poll: " + ex3.Message);
			}
		}
	}

	private void OnTurnEnded(AgentTask task, TurnOutcome outcome, string? message)
	{
		NotifyBrowserTurnEnded(task, outcome);
		string text;
		AgentTaskEventKind kind;
		string text2;
		string text3;
		string clickySlug;
		string jobId;
		string text6;
		lock (task.Gate)
		{
			text = task.FinishStreamingCommentary(task.CurrentNarrationItemId);
			switch (outcome)
			{
			case TurnOutcome.Completed:
			{
				StructuredFinalReply structuredFinalReply = StructuredFinalReply.Parse(task.ResponseAccumulator.ToString().Trim());
				task.FinalResponseMarkdown = ((structuredFinalReply.Text.Length > 0) ? structuredFinalReply.Text : null);
				task.Summary = structuredFinalReply.SummaryOrFallback;
				task.NextActions = structuredFinalReply.NextActions;
				task.DoneTitle = structuredFinalReply.DoneTitle;
				task.Artifacts = structuredFinalReply.Artifacts;
				task.Answer = structuredFinalReply.Answer;
				task.PendingUserStep = structuredFinalReply.UserStep;
				task.LearnedHowTos = structuredFinalReply.LearnedHowTos ?? Array.Empty<string>();
				string text4 = TakeUnansweredComputerUseStep?.Invoke(task.Id);
				string text5 = structuredFinalReply.ComputerUseRequest ?? text4;
				if (text5 != null)
				{
					Func<string, string, bool>? wasComputerUseStepRecentlyDeclined = WasComputerUseStepRecentlyDeclined;
					if (wasComputerUseStepRecentlyDeclined != null && wasComputerUseStepRecentlyDeclined(task.Id, text5))
					{
						text5 = null;
					}
				}
				task.PendingComputerUseRequest = text5 ?? task.PendingComputerUseRequest;
				task.Status = AgentTaskStatus.Finished;
				task.ProgressSummary = "Done";
				kind = AgentTaskEventKind.Finished;
				text2 = task.Summary;
				text3 = ((task.PendingComputerUseRequest == null) ? "completed" : "");
				break;
			}
			case TurnOutcome.ExtraEffortRequired:
				task.Status = AgentTaskStatus.WaitingForUser;
				task.RequiresExtraUsageApproval = true;
				task.ProgressSummary = "Needs your OK to keep going";
				kind = AgentTaskEventKind.NeedsExtraUsageApproval;
				text2 = "This task needs more effort than one agent message covers.";
				text3 = "";
				break;
			case TurnOutcome.Interrupted:
				task.Status = AgentTaskStatus.Stopped;
				task.ProgressSummary = message ?? "Stopped.";
				kind = AgentTaskEventKind.Stopped;
				text2 = message;
				text3 = "interrupted";
				break;
			default:
			{
				task.Status = AgentTaskStatus.Failed;
				task.FailureMessage = message ?? "Agent thread failed.";
				AgentTask agentTask = task;
				if ((object)agentTask.Error == null)
				{
					CompanionError companionError2 = (agentTask.Error = new CompanionError("agent_failed", task.FailureMessage, null));
				}
				task.ProgressSummary = task.FailureMessage;
				kind = AgentTaskEventKind.Failed;
				text2 = task.FailureMessage;
				text3 = "failed";
				break;
			}
			}
			if (task.TurnStartedTimestamp != 0L)
			{
				launchToFinishedMs = (long)Stopwatch.GetElapsedTime(task.LaunchTimestamp).TotalMilliseconds;
				if (task.FirstTokenTimestamp != 0L)
				{
					launchToFirstTokenMs = (long)((double)(task.FirstTokenTimestamp - task.LaunchTimestamp) * 1000.0 / (double)Stopwatch.Frequency);
				}
			}
			clickySlug = task.ClickySlug;
			jobId = task.JobId;
			text6 = task.Summary ?? task.FailureMessage;
		}
		Log.Info("Agents", $"task {task.Id} turn ended: {outcome}{((message == null) ? "" : (" (" + message + ")"))}");
		if (outcome == TurnOutcome.Failed && ComposioTurnErrors.IsListingFailure(message ?? text6))
		{
			Log.Info("Agents", "the turn couldn't list the Composio tools: the next turn fetches a fresh Composio session");
			InvalidateConnectedApps();
		}
		if (text != null)
		{
			Raise(task, AgentTaskEventKind.Commentary, text);
		}
		if (outcome == TurnOutcome.Completed)
		{
			AskAboutChromeIfStoppedForIt(task);
		}
		if (clickySlug != null || jobId != null)
		{
			routines.NoteTaskEnded(clickySlug, jobId, task.Id, text6, outcome != TurnOutcome.Completed, outcome == TurnOutcome.Interrupted, outcome == TurnOutcome.ExtraEffortRequired);
		}
		switch (outcome)
		{
		case TurnOutcome.Completed:
			NoteJobTaskEnded(task, ClickyJobOutcome.Done, text6);
			break;
		case TurnOutcome.Interrupted:
			NoteJobTaskEnded(task, ClickyJobOutcome.Stopped, message);
			break;
		case TurnOutcome.Failed:
			NoteJobTaskEnded(task, ClickyJobOutcome.Failed, text6);
			break;
		}
		if (outcome != TurnOutcome.ExtraEffortRequired && Lock(task, () => task.JobId != null))
		{
			string text7 = LiveStepsKey(task);
			if (text7 != null)
			{
				LiveSteps.Clear(text7);
			}
		}
		if (outcome == TurnOutcome.Completed)
		{
			NoteClickyAppWorkFinished(task);
		}
		SyncPendingRequest(task);
		Raise(task, kind, text2);
		if (outcome == TurnOutcome.Completed)
		{
			string threadId;
			lock (task.Gate)
			{
				threadId = task.ThreadId;
			}
			LogAgentMessage(task, threadId, "assistant", text6, isFollowUp: false);
		}
		if (text3.Length > 0)
		{
			CompleteLeaseInBackground(task, text3);
		}
		task.TurnCompletion?.TrySetResult(outcome.ToString());
		RaiseTasksChanged();
	}

	private void FailLaunch(AgentTask task, Exception ex)
	{
		bool flag = ex is OperationCanceledException;
		CompanionError companionError;
		if (!(ex is AgentPaywallException ex2))
		{
			if (!(ex is OperationCanceledException))
			{
				if (ex is AgentHarnessException ex3)
				{
					if (!ex3.IsNotAvailable)
					{
						goto IL_00e2;
					}
					companionError = new CompanionError("agent_runtime_missing", ex3.Message, null);
				}
				else if (!(ex is AgentBackendException ex4))
				{
					if (!(ex is HttpRequestException))
					{
						goto IL_00e2;
					}
					companionError = new CompanionError("agent_offline", AgentFailureDescription.UserMessage(ex), null);
				}
				else
				{
					companionError = new CompanionError("agent_backend_error", ex4.Message, ex4.BodyJson);
				}
			}
			else
			{
				companionError = new CompanionError("agent_cancelled", "Agent cancelled.", null);
			}
		}
		else
		{
			companionError = ex2.Error;
		}
		goto IL_00fe;
		IL_00e2:
		companionError = new CompanionError("agent_launch_failed", AgentFailureDescription.UserMessage(ex), null);
		goto IL_00fe;
		IL_00fe:
		CompanionError companionError2 = companionError;
		if (ex is AgentPaywallException ex5)
		{
			InterruptForPaywall(task, ex5.Error);
			return;
		}
		if (!flag)
		{
			Log.Error("Agents", "task " + task.Id + " launch failed", ex);
		}
		FlushPendingInitialMessageLog(task, Lock(task, () => task.ThreadId));
		string clickySlug;
		string jobId;
		lock (task.Gate)
		{
			if (!task.IsActive)
			{
				return;
			}
			task.Status = (flag ? AgentTaskStatus.Stopped : AgentTaskStatus.Failed);
			task.FailureMessage = (flag ? null : companionError2.Message);
			task.FailureDiagnostic = (flag ? null : AgentFailureDescription.Diagnostic(ex));
			task.Error = (flag ? null : companionError2);
			task.ProgressSummary = (flag ? "Stopped." : companionError2.Message);
			clickySlug = task.ClickySlug;
			jobId = task.JobId;
		}
		NoteJobTaskEnded(task, flag ? ClickyJobOutcome.Stopped : ClickyJobOutcome.Failed, companionError2.Message);
		Raise(task, flag ? AgentTaskEventKind.Stopped : AgentTaskEventKind.Failed, companionError2.Message);
		CompleteLeaseInBackground(task, flag ? "interrupted" : "failed");
		if (clickySlug != null || jobId != null)
		{
			RoutineScheduler routineScheduler = routines;
			string clickySlug2 = clickySlug;
			string jobId2 = jobId;
			string id = task.Id;
			string message = companionError2.Message;
			bool interrupted = flag;
			bool transientConnectivityFailure = ((ex is HttpRequestException || (ex is TaskCanceledException && ex.InnerException is TimeoutException)) ? true : false);
			routineScheduler.NoteTaskEnded(clickySlug2, jobId2, id, message, failed: true, interrupted, awaitingExtraUsage: false, transientConnectivityFailure);
		}
		task.TurnCompletion?.TrySetResult("failed");
		RaiseTasksChanged();
	}

	private void InterruptForPaywall(AgentTask task, CompanionError error)
	{
		Log.Info("Agents", "task " + task.Id + " hit the paywall before the turn started");
		FlushPendingInitialMessageLog(task, Lock(task, () => task.ThreadId));
		string clickySlug;
		string jobId;
		lock (task.Gate)
		{
			if (!task.IsActive)
			{
				return;
			}
			task.Status = AgentTaskStatus.Stopped;
			task.FailureMessage = "Oops, you're out of tokens! Beef up HeyClicky to continue.";
			task.Error = error;
			task.ProgressSummary = "Oops, you're out of tokens! Beef up HeyClicky to continue.";
			clickySlug = task.ClickySlug;
			jobId = task.JobId;
		}
		NoteJobTaskEnded(task, ClickyJobOutcome.Stopped, "Oops, you're out of tokens! Beef up HeyClicky to continue.");
		Raise(task, AgentTaskEventKind.Stopped, "Oops, you're out of tokens! Beef up HeyClicky to continue.");
		CompleteLeaseInBackground(task, "interrupted");
		if (clickySlug != null || jobId != null)
		{
			string text = PaywallReason(error);
			bool flag = ((text == null || text == "quota_exceeded") ? true : false);
			bool hitAgentMessageLimit = flag;
			routines.NoteTaskEnded(clickySlug, jobId, task.Id, "Oops, you're out of tokens! Beef up HeyClicky to continue.", failed: true, interrupted: true, PaywallReason(error) != "payment_problem", transientConnectivityFailure: false, hitAgentMessageLimit);
		}
		task.TurnCompletion?.TrySetResult("failed");
		RaiseTasksChanged();
	}

	private static string? PaywallReason(CompanionError error)
	{
		JsonElement? paywall = error.Paywall;
		if (paywall.HasValue)
		{
			JsonElement valueOrDefault = paywall.GetValueOrDefault();
			if (valueOrDefault.ValueKind == JsonValueKind.Object && valueOrDefault.TryGetProperty("reason", out var value) && value.ValueKind == JsonValueKind.String)
			{
				return value.GetString();
			}
		}
		return null;
	}

	private void RaisePaywall(AgentBackendException ex, bool presents)
	{
		try
		{
			this.PaywallReached?.Invoke(this, new AgentPaywallEventArgs(ex.StatusCode, ex.Body, presents));
		}
		catch (Exception ex2)
		{
			Log.Error("Agents", "PaywallReached handler threw", ex2);
		}
	}

	private void LogAgentMessage(AgentTask task, string? threadId, string role, string? content, bool isFollowUp)
	{
		string trimmed = content?.Trim();
		if (string.IsNullOrEmpty(trimmed))
		{
			return;
		}
		bool isDemo;
		long? suggestionId;
		lock (task.Gate)
		{
			isDemo = task.IsDemo;
			suggestionId = task.ProactiveSuggestionId;
		}
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				string text = await supabaseAccessToken(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				if (text != null)
				{
					await api.LogAgentMessageAsync(text, threadId, role, trimmed, isFollowUp, isDemo, suggestionId, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
					Log.Info("Agents", $"agent message logged: {role}{(isFollowUp ? " (follow-up)" : "")} thread={threadId ?? "none"}");
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Agents", "agent message log (" + role + ") failed: " + ex.Message);
			}
		});
	}

	private void FlushPendingInitialMessageLog(AgentTask task, string? threadId)
	{
		string pendingInitialMessageLog;
		lock (task.Gate)
		{
			pendingInitialMessageLog = task.PendingInitialMessageLog;
			task.PendingInitialMessageLog = null;
		}
		if (pendingInitialMessageLog != null)
		{
			LogAgentMessage(task, threadId, "user", pendingInitialMessageLog, isFollowUp: false);
		}
	}

	private void CompleteLeaseInBackground(AgentTask task, string status)
	{
		AgentTurnLease lease;
		string threadId;
		lock (task.Gate)
		{
			lease = task.Lease;
			threadId = task.ThreadId;
		}
		if ((object)lease == null)
		{
			return;
		}
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				string text = await supabaseAccessToken(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				if (text != null)
				{
					await api.CompleteLeaseAsync(text, lease.LeaseId, status, threadId, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Agents", "lease complete failed: " + ex.Message);
			}
		});
	}

	private async Task<string> TitleAsync(string prompt, CancellationToken ct)
	{
		_ = 1;
		try
		{
			string text = await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false);
			bool flag = text != null;
			string t = default(string);
			if (flag)
			{
				t = await api.ThreadTitleAsync(text, (prompt.Length > 2000) ? prompt.Substring(0, 2000) : prompt, ct, null).ConfigureAwait(continueOnCapturedContext: false);
				flag = t != null;
			}
			if (flag)
			{
				return t;
			}
		}
		catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
		{
			Log.Warn("Agents", "thread title failed: " + ex.Message);
		}
		string text2 = new string(prompt.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Where(char.IsLetterOrDigit).ToArray() ?? Array.Empty<char>());
		object result;
		if (text2.Length != 0)
		{
			ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text2[0]));
			string text3 = text2;
			result = string.Concat(readOnlySpan, text3.Substring(1, text3.Length - 1).ToLowerInvariant(), " Buddy");
		}
		else
		{
			result = "Clicky Buddy";
		}
		return (string)result;
	}

	private async Task<string?> FetchMemoryAsync(CancellationToken ct)
	{
		string userId = CurrentUserId?.Invoke();
		try
		{
			string text = await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (text == null)
			{
				return userMemory.Resolve(userId, UserMemoryFetchStatus.Failed, null);
			}
			var (status, markdown) = await api.UserMemoryAsync(text, ct).ConfigureAwait(continueOnCapturedContext: false);
			return userMemory.Resolve(userId, status, markdown);
		}
		catch (Exception) when (!ct.IsCancellationRequested)
		{
			return userMemory.Resolve(userId, UserMemoryFetchStatus.Failed, null);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private async Task<string> RequireSupabaseTokenAsync(CancellationToken ct)
	{
		return (await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Sign in to HeyClicky to use agents.");
	}

	private static string OneLinerFrom(string prompt)
	{
		string text = string.Join(' ', prompt.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
		int num = text.IndexOfAny(new char[3] { '.', '?', '!' });
		if (num > 0)
		{
			text = text.Substring(0, num);
		}
		if (text.Length <= 120)
		{
			return text;
		}
		return text.Substring(0, 120);
	}

	private static JsonObject? LeaseMetadata(AgentTurnLease? lease, AgentTask task, string threadId)
	{
		(string, string)? developerModel;
		lock (task.Gate)
		{
			developerModel = task.DeveloperModel;
		}
		if ((object)lease == null && !developerModel.HasValue)
		{
			return null;
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		if (developerModel.HasValue)
		{
			(string, string) valueOrDefault = developerModel.GetValueOrDefault();
			jsonObject["clicky_developer_model"] = valueOrDefault.Item1;
			jsonObject["clicky_developer_reasoning_effort"] = valueOrDefault.Item2;
		}
		if ((object)lease == null)
		{
			return jsonObject;
		}
		jsonObject["clicky_agent_turn_lease_id"] = lease.LeaseId;
		jsonObject["clicky_agent_turn_id"] = lease.TurnId;
		jsonObject["clicky_agent_thread_id"] = threadId;
		jsonObject["clicky_agent_task_id"] = task.NumericId.ToString("D").ToUpperInvariant();
		return jsonObject;
	}

	private static string PreviewText(string raw)
	{
		string text = raw.Trim();
		int num = text.IndexOf("User transcript:", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			string text2 = text;
			int num2 = num + "User transcript:".Length;
			string text3 = (from l in text2.Substring(num2, text2.Length - num2).Split('\n')
				select l.Trim()).FirstOrDefault((string l) => l.Length > 0);
			if (text3 != null)
			{
				return text3;
			}
		}
		if (text.Length != 0)
		{
			return text.Replace('\n', ' ');
		}
		return "Untitled agent thread";
	}

	private static string DisplayUserText(string raw)
	{
		int num = raw.LastIndexOf("User transcript:", StringComparison.Ordinal);
		string text;
		if (num < 0)
		{
			text = raw;
		}
		else
		{
			string text2 = raw;
			int num2 = num + "User transcript:".Length;
			text = text2.Substring(num2, text2.Length - num2);
		}
		string text3 = text;
		int num3 = text3.IndexOf("</clicky_agent_turn_lease_metadata>", StringComparison.Ordinal);
		if (num3 >= 0)
		{
			string text2 = text3;
			int num2 = num3 + "</clicky_agent_turn_lease_metadata>".Length;
			text3 = text2.Substring(num2, text2.Length - num2);
		}
		text3 = text3.Trim();
		if (num >= 0 && !text3.Contains("\nAttached files:\n", StringComparison.Ordinal))
		{
			List<string> list = (from p in (from l in raw.Substring(0, num).Split('\n')
					select l.Trim() into l
					where l.StartsWith("- File #", StringComparison.Ordinal) && l.IndexOf(": ", StringComparison.Ordinal) > 0
					select l).Select(delegate(string l)
				{
					int num4 = l.IndexOf(": ", StringComparison.Ordinal) + 2;
					return l.Substring(num4, l.Length - num4).Trim();
				})
				where p.Length > 0
				select p).ToList();
			if (list.Count > 0)
			{
				text3 = (text3 + "\n\nAttached files:\n" + string.Join("\n", list.Select((string f) => "- " + f))).TrimStart();
			}
		}
		return text3;
	}

	private AgentTask GetTask(string taskId)
	{
		if (!tasks.TryGetValue(taskId, out AgentTask value))
		{
			throw new KeyNotFoundException("No agent task " + taskId + ".");
		}
		return value;
	}

	private static T Lock<T>(AgentTask t, Func<T> f)
	{
		lock (t.Gate)
		{
			return f();
		}
	}

	private void SetProgress(AgentTask task, string summary)
	{
		lock (task.Gate)
		{
			task.ProgressSummary = summary;
		}
		Raise(task, AgentTaskEventKind.ProgressChanged, summary);
	}

	private void Raise(AgentTask task, AgentTaskEventKind kind, string? text)
	{
		if (kind == AgentTaskEventKind.ThinkingHeading && text != null && text.Length > 0)
		{
			string text2 = LiveStepsKey(task);
			if (text2 != null)
			{
				LiveSteps.NoteThinking(text, text2, null);
			}
		}
		try
		{
			this.TaskEvent?.Invoke(this, new AgentTaskEvent(task.Id, kind, text));
		}
		catch (Exception ex)
		{
			Log.Error("Agents", "TaskEvent handler threw", ex);
		}
		bool flag;
		switch (kind)
		{
		case AgentTaskEventKind.ProgressChanged:
		case AgentTaskEventKind.Commentary:
		case AgentTaskEventKind.TitleChanged:
		case AgentTaskEventKind.PlanUpdated:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			RaiseTasksChanged();
		}
	}

	private void RaiseTasksChanged()
	{
		if (Interlocked.Exchange(ref tasksChangedPending, 1) == 1)
		{
			return;
		}
		Task.Delay(50).ContinueWith(delegate
		{
			Volatile.Write(ref tasksChangedPending, 0);
			try
			{
				this.TasksChanged?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				Log.Error("Agents", "TasksChanged handler threw", ex);
			}
		}, TaskScheduler.Default);
	}

	public async ValueTask DisposeAsync()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		routines.Dispose();
		foreach (AgentTask item in tasks.Values.ToList())
		{
			session.Interrupt(item, TurnOutcome.Interrupted, "HeyClicky stopped before the agent task finished.");
		}
		lifetime.Cancel();
		await Task.Run((Action)AgentHarnessRunnerProcess.Stop).ConfigureAwait(continueOnCapturedContext: false);
		lifetime.Dispose();
	}

	private string? LiveNotePath(AgentTask task)
	{
		string clickySlug;
		string workspacePath;
		bool isDemo;
		bool flag;
		lock (task.Gate)
		{
			clickySlug = task.ClickySlug;
			workspacePath = task.WorkspacePath;
			isDemo = task.IsDemo;
			flag = task.JobId != null;
		}
		IClickyLiveNoteTurns liveNotes = LiveNotes;
		if (liveNotes == null || isDemo || flag || clickySlug == null)
		{
			return null;
		}
		return liveNotes.NotePath(clickySlug, workspacePath ?? "");
	}

	private string ClickyLiveNoteSection(AgentTask task, string prompt, string launchSource, bool isGeneratedBrief, string? threadId, bool threadIsActive)
	{
		IClickyLiveNoteTurns liveNotes = LiveNotes;
		if (liveNotes != null)
		{
			string text = LiveNotePath(task);
			if (text != null)
			{
				bool isRoutineRun;
				string title;
				lock (task.Gate)
				{
					isRoutineRun = task.IsRoutineRun;
					title = task.Title;
				}
				string userMessage = ((!isRoutineRun && !isGeneratedBrief && !threadIsActive && !(launchSource == "routine")) ? prompt : null);
				string routineRun = (isRoutineRun ? title : null);
				(AgentHarness, AgentHarnessPlace) tuple = AgentHarnessThreads.Choice(threadId);
				ClickyLiveNoteWriter writer = ((!AgentHarnessPreferences.UsesNoteTools(tuple.Item1, tuple.Item2)) ? ClickyLiveNoteWriter.FileTools : ClickyLiveNoteWriter.NoteTools);
				try
				{
					return liveNotes.PromptSection(text, userMessage, routineRun, writer);
				}
				catch (Exception ex)
				{
					Log.Warn("Agents", "live note section: " + ex.Message);
					return "";
				}
			}
		}
		return "";
	}

	private void NoteLiveNoteTaskEnded(AgentTask task)
	{
		IClickyLiveNoteTurns liveNotes = LiveNotes;
		if (liveNotes == null)
		{
			return;
		}
		string text = LiveNotePath(task);
		if (text == null)
		{
			return;
		}
		try
		{
			liveNotes.TurnEnded(text);
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "live note turn end: " + ex.Message);
		}
	}

	private void NoteLiveNoteFileActivity(AgentTask task, string path, string? streamedKind, string? streamedDiff)
	{
		IClickyLiveNoteTurns liveNotes = LiveNotes;
		if (liveNotes == null)
		{
			return;
		}
		string text = LiveNotePath(task);
		if (text == null)
		{
			return;
		}
		try
		{
			liveNotes.FileActivity(text, path, streamedKind, streamedDiff);
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "live note file: " + ex.Message);
		}
	}

	private void ConfigureAgentHarness()
	{
		AgentHarnessRunnerProcess.StateDirectory = Path.Combine(options.StateDirectory, "AgentHarness");
		AgentHarnessRunnerProcess.BundledRunnerDirectory = options.HarnessRuntimeDirectory;
		AgentHarnessRunnerProcess.InternalError += delegate(string subsystem, string stage, string detail, string key)
		{
			RaiseInternalError(subsystem, stage, detail, key);
		};
		session.OnHarnessTurnEnded = delegate(AgentHarness harness, AgentHarnessPlace place, string status, double seconds, IReadOnlyDictionary<string, double> stats)
		{
			try
			{
				this.HarnessTurnEnded?.Invoke(harness.RawValue(), place.RawValue(), status, seconds, stats);
			}
			catch (Exception ex)
			{
				Log.Warn("Harness", "HarnessTurnEnded handler threw: " + ex.Message);
			}
		};
		session.OnHarnessInternalError = delegate(string stage, string detail, string key)
		{
			RaiseInternalError("agent_harness", stage, detail, key);
		};
		session.OnHarnessConnectedAppsUnreachable = delegate
		{
			Log.Info("Agents", "a harness job couldn't reach the connected apps: the next turn fetches a fresh Composio session");
			InvalidateConnectedApps();
		};
	}

	private void RaiseInternalError(string subsystem, string stage, string detail, string key)
	{
		try
		{
			this.InternalError?.Invoke(subsystem, stage, detail, key);
		}
		catch (Exception ex)
		{
			Log.Warn("Harness", "InternalError handler threw: " + ex.Message);
		}
	}

	private string TakeoverContext(string threadId, AgentTask task)
	{
		string jobId;
		lock (task.Gate)
		{
			jobId = task.JobId;
		}
		ClickyJob clickyJob = ((jobId == null) ? null : Jobs.Job(jobId)) ?? Jobs.JobForThread(threadId);
		string value = (((object)clickyJob != null) ? JobConversationSoFar(clickyJob) : NewestExchanges(CachedConversationExchanges(new _003C_003Ez__ReadOnlySingleElementList<string>(threadId)), 24000));
		string value2 = (((object)clickyJob == null && clickys.Clickys.Any((ClickyInfo c) => c.ThreadId == threadId)) ? "Your workspace (the working folder) has AGENTS.md with your notes and everything you made earlier; read AGENTS.md first, and open an earlier file only when this message needs it." : "The working folder has everything made earlier; open an earlier file only when this message needs it.");
		return $"This conversation began in an earlier agent setup, and you're carrying it on fresh: you can't see those earlier turns, so here they are, oldest first:\n<earlier_conversation>\n{value}\n</earlier_conversation>\n{value2} Keep what was done; pick up from where it stopped.";
	}

	private async Task<AgentHarnessTurnContext> HarnessTurnContextAsync(string appThreadId, AgentTask task, string workingDirectory, CancellationToken ct)
	{
		await EnsureRuntimeReadyAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		string sessionToken = await tokens.GetAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		List<AgentHarnessMcpServer> servers = new List<AgentHarnessMcpServer>();
		string text = composio?.Url;
		if (text != null && text.Length > 0)
		{
			servers.Add(new AgentHarnessMcpServer("composio", text, sessionToken));
		}
		servers.AddRange(HarnessCustomConnectorServers());
		Func<CancellationToken, Task<AgentHarnessMcpServer>> browserMcpServerProvider = BrowserMcpServerProvider;
		if (browserMcpServerProvider != null)
		{
			try
			{
				AgentHarnessMcpServer agentHarnessMcpServer = await browserMcpServerProvider(ct).ConfigureAwait(continueOnCapturedContext: false);
				if ((object)agentHarnessMcpServer != null)
				{
					servers.Add(agentHarnessMcpServer with
					{
						SendsTurnIdentity = true
					});
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Harness", "browser endpoint: " + ex.Message);
			}
		}
		bool isJob;
		lock (task.Gate)
		{
			isJob = task.JobId != null;
		}
		string? notePath = (isJob ? null : LiveNotePath(task));
		(AgentHarness Harness, AgentHarnessPlace Place) choice = AgentHarnessThreads.Choice(appThreadId);
		IReadOnlyList<string> userSkillDirectories = await ClickyUserSkillFolders.SkillDirectoriesAsync().ConfigureAwait(continueOnCapturedContext: false);
		AgentHarnessTurnContext agentHarnessTurnContext = new AgentHarnessTurnContext
		{
			Instructions = (home.DeliveredModelInstructions ?? ""),
			McpServers = servers,
			SkillDirectories = HarnessSkillDirectories(),
			UserSkillDirectories = userSkillDirectories,
			WorkerBaseUrl = WorkerBase,
			SessionToken = sessionToken,
			NotePath = notePath,
			IsKeptThing = notePath != null && !isJob,
			WritesNoteWithTools = notePath != null && AgentHarnessPreferences.UsesNoteTools(choice.Harness, choice.Place),
			UsesPlanTool = isJob,
			Model = (tokens.AllowsDeveloperModelSelection ? AgentHarnessPreferences.Model : AgentHarnessModel.Standard)
		};
		return agentHarnessTurnContext;
	}

	private IReadOnlyList<AgentHarnessMcpServer> HarnessCustomConnectorServers()
	{
		List<AgentHarnessMcpServer> list = new List<AgentHarnessMcpServer>();
		Uri result = default(Uri);
		foreach (CustomConnectorRegistration customConnector in customConnectors)
		{
			if (customConnector.Transport is CustomConnectorTransport.Stdio stdio)
			{
				if (stdio.Command.Length == 0)
				{
					continue;
				}
				Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				foreach (string passthroughEnvironmentVariable in stdio.PassthroughEnvironmentVariables)
				{
					if (customConnector.Environment.TryGetValue(passthroughEnvironmentVariable, out string value))
					{
						dictionary[passthroughEnvironmentVariable] = value;
					}
				}
				dictionary["PATH"] = stdio.SearchPath;
				list.Add(new AgentHarnessMcpServer(customConnector.ServerKey, "", null)
				{
					LocalCommand = new AgentHarnessLocalMcpCommand(stdio.Command, stdio.Arguments, dictionary)
				});
				continue;
			}
			CustomConnectorTransport.Remote remote = customConnector.Transport as CustomConnectorTransport.Remote;
			bool flag = (object)remote == null || !Uri.TryCreate(remote.Url.Trim(), UriKind.Absolute, out result);
			string key;
			if (!flag)
			{
				key = result.Scheme;
				bool flag2 = ((key == "https" || key == "http") ? true : false);
				flag = !flag2;
			}
			if (flag)
			{
				continue;
			}
			string bearerTokenEnvironmentVariable = remote.BearerTokenEnvironmentVariable;
			string value2;
			string bearerToken = ((bearerTokenEnvironmentVariable != null && customConnector.Environment.TryGetValue(bearerTokenEnvironmentVariable, out value2) && value2.Length > 0) ? value2 : null);
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> headerEnvironmentVariable in remote.HeaderEnvironmentVariables)
			{
				headerEnvironmentVariable.Deconstruct(out key, out var value3);
				string key2 = key;
				string key3 = value3;
				if (customConnector.Environment.TryGetValue(key3, out string value4))
				{
					dictionary2[key2] = value4;
				}
			}
			list.Add(new AgentHarnessMcpServer(customConnector.ServerKey, result.AbsoluteUri, bearerToken)
			{
				Headers = dictionary2
			});
		}
		return list;
	}

	private IReadOnlyList<string> HarnessSkillDirectories()
	{
		List<string> list = new List<string>();
		string[] array = new string[2] { "skills", "power-up-skills" };
		foreach (string path in array)
		{
			string path2 = Path.Combine(options.CodexHome, path);
			if (!Directory.Exists(path2))
			{
				continue;
			}
			foreach (string item in Directory.GetDirectories(path2).OrderBy<string, string>((string d) => Path.GetFileName(d), StringComparer.Ordinal))
			{
				if (!Path.GetFileName(item).StartsWith('.') && File.Exists(Path.Combine(item, "SKILL.md")))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public ClickyInfo? RenameClicky(string slug, string name)
	{
		ClickyInfo clickyInfo = clickys.Rename(slug, name);
		if ((object)clickyInfo == null)
		{
			return null;
		}
		foreach (AgentTask item in tasks.Values.Where((AgentTask t) => t.ClickySlug == slug))
		{
			lock (item.Gate)
			{
				item.ClickyName = clickyInfo.Name;
			}
		}
		RaiseTasksChanged();
		return clickyInfo;
	}

	public ClickyInfo? UnarchiveClicky(string slug)
	{
		ClickyInfo clickyInfo = clickys.Unarchive(slug);
		if ((object)clickyInfo != null)
		{
			clickys.EnsureWorkspace(clickyInfo);
		}
		RaiseTasksChanged();
		return clickyInfo;
	}

	public bool IsThreadOwnedByClicky(string threadId)
	{
		return clickys.OwnsThread(threadId);
	}

	public bool MoveThreadIntoClicky(string threadId, string slug, string? notesMarkdown)
	{
		ClickyInfo clickyInfo = clickys.Get(slug);
		if ((object)clickyInfo == null || clickyInfo.IsArchived || clickys.OwnsThread(threadId))
		{
			return false;
		}
		clickys.EnsureWorkspace(clickyInfo);
		bool flag = clickyInfo.ThreadId == null;
		if (flag)
		{
			clickys.BindThread(slug, threadId);
		}
		else
		{
			clickys.FileThread(threadId, slug);
		}
		if (!string.IsNullOrWhiteSpace(notesMarkdown))
		{
			clickys.AppendNotes(clickyInfo, notesMarkdown);
		}
		RebindTasksForThread(threadId, clickys.Get(slug));
		Log.Info("Agents", $"moved thread {threadId.Substring(0, Math.Min(8, threadId.Length))} into {slug} (became chat: {flag})");
		RaiseTasksChanged();
		return flag;
	}

	public ClickyInfo? FoundClickyFromThread(string threadId, string name, string roleText, string oneLiner, string? notesMarkdown)
	{
		if (clickys.OwnsThread(threadId))
		{
			return null;
		}
		ClickyInfo clickyInfo = CreateClicky(name, string.IsNullOrWhiteSpace(roleText) ? "Agent" : roleText, oneLiner);
		clickys.BindThread(clickyInfo.Slug, threadId);
		if (!string.IsNullOrWhiteSpace(notesMarkdown))
		{
			clickys.AppendNotes(clickyInfo, notesMarkdown);
		}
		ClickyInfo clickyInfo2 = clickys.Get(clickyInfo.Slug);
		RebindTasksForThread(threadId, clickyInfo2);
		Log.Info("Agents", "founded " + clickyInfo2.Slug + " from thread " + threadId.Substring(0, Math.Min(8, threadId.Length)));
		RaiseTasksChanged();
		return clickyInfo2;
	}

	public void AppendClickyNotes(string slug, string markdown)
	{
		ClickyInfo clickyInfo = clickys.Get(slug);
		if ((object)clickyInfo != null)
		{
			clickys.AppendNotes(clickyInfo, markdown);
		}
	}

	public string? ClickyNotesPath(string slug)
	{
		if ((object)clickys.Get(slug) != null)
		{
			return clickys.NotesPath(slug);
		}
		return null;
	}

	private void RebindTasksForThread(string threadId, ClickyInfo clicky)
	{
		foreach (AgentTask item in tasks.Values.Where((AgentTask t) => t.ThreadId == threadId))
		{
			lock (item.Gate)
			{
				item.ClickySlug = clicky.Slug;
				item.ClickyName = clicky.Name;
				item.WorkspacePath = clickys.WorkspacePath(clicky.Slug);
			}
		}
	}

	private async Task RefreshConnectorTokensBeforePrepareAsync(CancellationToken ct)
	{
		Func<CancellationToken, Task<IReadOnlyList<CustomConnectorRegistration>>> refreshCustomConnectorTokensAsync = RefreshCustomConnectorTokensAsync;
		if (refreshCustomConnectorTokensAsync == null)
		{
			return;
		}
		try
		{
			IReadOnlyList<CustomConnectorRegistration> readOnlyList = await refreshCustomConnectorTokensAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (readOnlyList != null)
			{
				customConnectors = readOnlyList;
				configDirty = true;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "connector token refresh: " + ex.Message);
		}
	}

	private void WireIntegrationEvents()
	{
		session.OnIntegrationUsed = delegate(AgentTask task, string key)
		{
			string clickySlug;
			bool flag;
			lock (task.Gate)
			{
				clickySlug = task.ClickySlug;
				flag = task.IntegrationsUsedThisTurn.Add(key);
			}
			if (clickySlug != null)
			{
				RecordDataSource(clickySlug, key);
			}
			if (flag)
			{
				Raise(task, AgentTaskEventKind.IntegrationUsed, key);
			}
		};
	}

	public void SetObsidianVaultPath(string? path)
	{
		string text = (string.IsNullOrWhiteSpace(path) ? null : path.Trim());
		if (!string.Equals(options.ObsidianVaultPath, text, StringComparison.OrdinalIgnoreCase))
		{
			options.ObsidianVaultPath = text;
			InvalidateConnectedApps();
			Log.Info("Agents", (text == null) ? "obsidian vault cleared" : ("obsidian vault set (" + Path.GetFileName(text.TrimEnd('\\')) + ")"));
		}
	}

	private Dictionary<string, List<string>> LoadDataSources()
	{
		if (dataSources != null)
		{
			return dataSources;
		}
		dataSources = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		try
		{
			if (File.Exists(DataSourcesPath) && JsonNode.Parse(File.ReadAllText(DataSourcesPath), null) is JsonObject jsonObject)
			{
				foreach (var (key, jsonNode2) in jsonObject)
				{
					dataSources[key] = ((jsonNode2 as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).Select((JsonNode k) => k?.ToString()).OfType<string>().ToList();
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "data-sources.json unreadable: " + ex.Message);
		}
		return dataSources;
	}

	public void RecordDataSource(string slug, string providerKey)
	{
		string text = providerKey.Trim().ToLowerInvariant();
		if (text.Length == 0)
		{
			return;
		}
		lock (dataSourcesGate)
		{
			Dictionary<string, List<string>> dictionary = LoadDataSources();
			if (!dictionary.TryGetValue(slug, out var value))
			{
				value = (dictionary[slug] = new List<string>());
			}
			if (value.Contains(text))
			{
				return;
			}
			value.Add(text);
			try
			{
				Directory.CreateDirectory(options.StateDirectory);
				JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
				foreach (KeyValuePair<string, List<string>> item in dictionary)
				{
					item.Deconstruct(out var key, out var value2);
					string propertyName = key;
					List<string> source = value2;
					jsonObject[propertyName] = new JsonArray(((IEnumerable<string>)source).Select((Func<string, JsonNode>)((string x) => x)).ToArray());
				}
				File.WriteAllText(DataSourcesPath, jsonObject.ToJsonString(new JsonSerializerOptions
				{
					WriteIndented = true
				}));
			}
			catch (Exception ex)
			{
				Log.Warn("Agents", "data-sources.json write failed: " + ex.Message);
			}
		}
		Log.Info("Agents", "data source: " + slug + " used " + text);
		this.DataSourceRecorded?.Invoke(slug, text);
	}

	public void RemoveDataSource(string slug, string providerKey)
	{
		lock (dataSourcesGate)
		{
			Dictionary<string, List<string>> dictionary = LoadDataSources();
			if (!dictionary.TryGetValue(slug, out var value) || !value.Remove(providerKey.Trim().ToLowerInvariant()))
			{
				return;
			}
			if (value.Count == 0)
			{
				dictionary.Remove(slug);
			}
			try
			{
				JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
				foreach (KeyValuePair<string, List<string>> item in dictionary)
				{
					item.Deconstruct(out var key, out var value2);
					string propertyName = key;
					List<string> source = value2;
					jsonObject[propertyName] = new JsonArray(((IEnumerable<string>)source).Select((Func<string, JsonNode>)((string x) => x)).ToArray());
				}
				File.WriteAllText(DataSourcesPath, jsonObject.ToJsonString(new JsonSerializerOptions
				{
					WriteIndented = true
				}));
			}
			catch (Exception ex)
			{
				Log.Warn("Agents", "data-sources.json write failed: " + ex.Message);
			}
		}
	}

	public IReadOnlyList<string> DataSources(string slug)
	{
		lock (dataSourcesGate)
		{
			List<string> value;
			return LoadDataSources().TryGetValue(slug, out value) ? value.ToList() : new List<string>();
		}
	}

	private static string JobNamingBasis(ClickyJob job)
	{
		string text = job.Prompt.Trim();
		string text2 = ((text.Length == 0 || text == job.Title) ? job.Title : (job.Title + ". " + text));
		if (text2.Length <= 600)
		{
			return text2;
		}
		return text2.Substring(0, 600);
	}

	internal static bool IsSentenceRatherThanName(string name)
	{
		List<string> list = (from word in name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
			select new string(word.ToLowerInvariant().Where(char.IsLetter).ToArray())).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		if (!SentenceStartWords.Contains(list[0]))
		{
			return list.Any((string word) => word.Length == 1);
		}
		return true;
	}

	private IReadOnlyList<string> JobNamesInUse(string exceptJobId)
	{
		return (from job in Jobs.Jobs
			where job.Id != exceptJobId && job.IsArchived != true && !string.IsNullOrWhiteSpace(job.Nickname)
			orderby job.CreatedAt descending
			select job.Nickname.Trim()).ToList();
	}

	public void NameJobsWithoutNicknames()
	{
		if (Interlocked.Exchange(ref jobNamingRoundRunning, 1) == 1)
		{
			return;
		}
		Task.Run((Func<Task?>)async delegate
		{
			int account = Volatile.Read(in accountGeneration);
			try
			{
				foreach (ClickyJob item in Jobs.Jobs.Where((ClickyJob job) => !string.IsNullOrWhiteSpace(job.Nickname) && IsSentenceRatherThanName(job.Nickname)).ToList())
				{
					Jobs.SetNickname(item.Id, "");
				}
				while (!lifetime.IsCancellationRequested)
				{
					ClickyJob next;
					lock (jobIdsBeingNamed)
					{
						next = (from job in Jobs.Jobs
							where string.IsNullOrWhiteSpace(job.Nickname) && job.IsArchived != true && !jobIdsBeingNamed.Contains(job.Id)
							orderby job.CreatedAt descending
							select job).FirstOrDefault();
					}
					if ((object)next == null)
					{
						break;
					}
					lock (jobIdsBeingNamed)
					{
						jobIdsBeingNamed.Add(next.Id);
					}
					string basis = JobNamingBasis(next);
					if (basis.Length >= 8)
					{
						string text = await supabaseAccessToken(lifetime.Token).ConfigureAwait(continueOnCapturedContext: false);
						if (text == null || SignedOutSince())
						{
							break;
						}
						string text2 = await api.ThreadTitleAsync(text, basis, lifetime.Token, TimeSpan.FromSeconds(20L), JobNamesInUse(next.Id)).ConfigureAwait(continueOnCapturedContext: false);
						if (string.IsNullOrWhiteSpace(text2) || SignedOutSince())
						{
							break;
						}
						if (!IsSentenceRatherThanName(text2))
						{
							Jobs.SetNickname(next.Id, text2.Trim());
							next = null;
						}
					}
				}
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				Log.Warn("Agents", "job naming stopped: " + ex.Message);
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				lock (jobIdsBeingNamed)
				{
					jobIdsBeingNamed.Clear();
				}
				Interlocked.Exchange(ref jobNamingRoundRunning, 0);
			}
			bool SignedOutSince()
			{
				return account != Volatile.Read(in accountGeneration);
			}
		});
	}

	private string? LiveStepsKey(AgentTask task)
	{
		lock (task.Gate)
		{
			if (task.IsDemo)
			{
				return null;
			}
			string jobId = task.JobId;
			if (jobId != null)
			{
				return "job:" + jobId;
			}
		}
		return LiveNotePath(task);
	}

	private void WireLiveSteps()
	{
		session.OnTaskFilePatchStreaming = delegate(AgentTask task, string path, string kind, string diff)
		{
			NoteLiveNoteFileActivity(task, path, kind, diff);
			this.TaskFilePatchStreaming?.Invoke(task.Id, path, kind, diff);
		};
		session.OnTaskFileChangeCompleted = delegate(AgentTask task, IReadOnlyList<string> paths)
		{
			foreach (string path in paths)
			{
				NoteLiveNoteFileActivity(task, path, null, null);
			}
			this.TaskFileChangeCompleted?.Invoke(task.Id, paths);
		};
		session.OnTaskToolStep = delegate(AgentTask task, string text)
		{
			if (text.StartsWith("Reading:", StringComparison.Ordinal))
			{
				lock (task.Gate)
				{
					int length = "Reading:".Length;
					task.NoteSitesRead(new _003C_003Ez__ReadOnlySingleElementList<string>(text.Substring(length, text.Length - length)));
				}
			}
			string text2 = LiveStepsKey(task);
			if (text2 != null)
			{
				LiveSteps.Note(text, text2, null);
			}
		};
		session.OnTaskProgramBatch = delegate(AgentTask task, ClickyProgramBatch batch)
		{
			lock (task.Gate)
			{
				task.NoteSitesRead(batch.SitesRead);
			}
			string text3 = LiveStepsKey(task);
			if (text3 != null)
			{
				LiveSteps.NoteBatch(NamingApps(batch), text3, null);
			}
		};
		session.OnTaskPlanUpdated = delegate(AgentTask task, IReadOnlyList<AgentPlanStep> steps)
		{
			string text4 = LiveStepsKey(task);
			if (text4 != null)
			{
				LiveSteps.NotePlan(ClickyArcPlan.FromPlanTool(steps), text4);
			}
			Raise(task, AgentTaskEventKind.PlanUpdated, string.Join(" · ", steps.Select((AgentPlanStep s) => s.Text)));
		};
	}

	private ClickyProgramBatch NamingApps(ClickyProgramBatch batch)
	{
		if (batch.Apps.Count == 0)
		{
			return batch;
		}
		List<(string, string, IReadOnlyList<(string, string)>)> integrations = (lastIntegrationCatalog?.Integrations ?? Array.Empty<AgentIntegration>()).Select((AgentIntegration i) => ((string DisplayName, string LogoUrl, IReadOnlyList<(string, string)>))(DisplayName: i.DisplayName, LogoUrl: i.LogoUrl, i.Toolkits.Select((AgentIntegrationToolkit t) => (Slug: t.Slug, Logo: t.Logo)).ToList())).ToList();
		return batch.NamingApps(integrations);
	}

	private string ChromeAnswerMeanings()
	{
		return ChromeAnswerMeaningsForBrain?.Invoke() ?? "approve or show_how = they add it from the Chrome Web Store with you showing them each click";
	}

	private ClickyJobStepWatcher CreateStepWatcher()
	{
		ClickyJobStepWatcher clickyJobStepWatcher = new ClickyJobStepWatcher(Jobs);
		clickyJobStepWatcher.StepDone += delegate(string jobId, string address)
		{
			ContinueJobAfterUserStep(jobId, address);
		};
		return clickyJobStepWatcher;
	}

	private AgentTask? LiveTask(ClickyJob job)
	{
		AgentTask? agentTask = tasks.Values.FirstOrDefault((AgentTask t) => Lock(t, () => t.JobId) == job.Id);
		if (agentTask == null)
		{
			string threadId = job.ThreadId;
			if (threadId == null)
			{
				return null;
			}
			agentTask = tasks.Values.FirstOrDefault((AgentTask t) => Lock(t, () => t.ThreadId) == threadId);
		}
		return agentTask;
	}

	public string? LiveTaskId(string jobId)
	{
		ClickyJob clickyJob = Jobs.Job(jobId);
		if ((object)clickyJob == null)
		{
			return null;
		}
		return LiveTask(clickyJob)?.Id;
	}

	public ClickyJobRailPhase RailPhase(ClickyJob job)
	{
		if (job.IsHeldForChrome)
		{
			return ClickyJobRailPhase.NeedsYou;
		}
		AgentTask agentTask = LiveTask(job);
		if (agentTask != null)
		{
			AgentTaskStatus status;
			bool requiresExtraUsageApproval;
			bool flag;
			bool needsChromeConnection;
			lock (agentTask.Gate)
			{
				status = agentTask.Status;
				requiresExtraUsageApproval = agentTask.RequiresExtraUsageApproval;
				flag = (object)agentTask.PendingUserStep != null;
				needsChromeConnection = agentTask.NeedsChromeConnection;
			}
			if (requiresExtraUsageApproval || agentTask.PendingComputerUseRequest != null)
			{
				return ClickyJobRailPhase.NeedsYou;
			}
			if (needsChromeConnection)
			{
				return ClickyJobRailPhase.NeedsYou;
			}
			if (status == AgentTaskStatus.Finished && (flag || (object)job.WaitingStep != null))
			{
				return ClickyJobRailPhase.NeedsYou;
			}
			return status switch
			{
				AgentTaskStatus.Starting => ClickyJobRailPhase.Queued, 
				AgentTaskStatus.Running => ClickyJobRailPhase.Working, 
				AgentTaskStatus.Finished => ClickyJobRailPhase.Done, 
				AgentTaskStatus.Failed => ClickyJobRailPhase.Failed, 
				AgentTaskStatus.WaitingForUser => ClickyJobRailPhase.NeedsYou, 
				_ => ClickyJobRailPhase.Stopped, 
			};
		}
		if ((object)job.WaitingStep != null && job.Outcome == ClickyJobOutcome.Done)
		{
			return ClickyJobRailPhase.NeedsYou;
		}
		return job.Outcome switch
		{
			ClickyJobOutcome.Working => ClickyJobRailPhase.Working, 
			ClickyJobOutcome.Done => ClickyJobRailPhase.Done, 
			ClickyJobOutcome.Failed => ClickyJobRailPhase.Failed, 
			_ => ClickyJobRailPhase.Stopped, 
		};
	}

	public ClickyJobMessageRoute JobMessageRoute(ClickyJob job, string requestedThreadId, string source, bool hasPreauthorizedLease)
	{
		ClickyJobMessageRoute result = new ClickyJobMessageRoute(requestedThreadId, null);
		bool flag = !ClickyJobsPreferences.IsEnabled;
		if (!flag)
		{
			bool flag2 = ((source == "text" || source == "voice") ? true : false);
			flag = !flag2;
		}
		if (flag || hasPreauthorizedLease)
		{
			return result;
		}
		AgentTask liveRun = LiveTask(job);
		if (liveRun != null)
		{
			string text = Lock(liveRun, () => liveRun.ThreadId);
			if (text != null)
			{
				bool num = Lock(liveRun, () => liveRun.IsActive) || session.IsThreadActive(text);
				bool flag3 = Lock(liveRun, () => liveRun.RequiresExtraUsageApproval) || liveRun.PendingComputerUseRequest != null;
				if (num || flag3)
				{
					return new ClickyJobMessageRoute(text, null);
				}
			}
		}
		if (session.IsThreadActive(requestedThreadId))
		{
			return result;
		}
		string threadId = job.ThreadId;
		flag = threadId != null;
		if (flag)
		{
			ClickyJobOutcome outcome = job.Outcome;
			bool flag2 = ((outcome == ClickyJobOutcome.Done || outcome == ClickyJobOutcome.Stopped) ? true : false);
			flag = flag2;
		}
		if (flag && DateTimeOffset.Now - (job.FinishedAt ?? DateTimeOffset.MinValue) < JobResumeWindow && AgentHarnessThreads.ContinuesOnNewConversationHarness(threadId))
		{
			return new ClickyJobMessageRoute(threadId, null, ResumesFinishedRun: true);
		}
		return new ClickyJobMessageRoute(null, job.ThreadId ?? requestedThreadId);
	}

	private (string? ThreadId, string Context) PlanJobTurn(ClickyJob job, AgentTask task, string ask, string source, bool hasPreauthorizedLease, string? requestedThreadId, bool startsNewConversation = false)
	{
		string text = "";
		string text2 = (startsNewConversation ? null : (requestedThreadId ?? job.ThreadId));
		lock (task.Gate)
		{
			task.IsNewJobRun = false;
		}
		if (text2 != null)
		{
			ClickyJobMessageRoute clickyJobMessageRoute = JobMessageRoute(job, text2, source, hasPreauthorizedLease);
			if (clickyJobMessageRoute.NewRunFollowsThreadId != null)
			{
				lock (task.Gate)
				{
					task.PendingUserStep = null;
					task.IsNewJobRun = true;
				}
				bool hasPreviousNote = ClickyWorkFolderFiles.RetireNote(job.FolderPath);
				text = text + "\n" + JobContinuationContext(job, hasPreviousNote);
				Jobs.BeginRun(job.Id, ask);
				text2 = null;
			}
			else
			{
				if (clickyJobMessageRoute.ResumesFinishedRun)
				{
					lock (task.Gate)
					{
						task.PendingUserStep = null;
					}
					ClickyWorkFolderFiles.RetireNote(job.FolderPath);
					text += "\nThis is a new message to this job, in the conversation where you did the earlier work: build on it, and change what they ask now. Answer in the language of this newest message.";
					Jobs.BeginRun(job.Id, ask, job.ThreadId);
				}
				text2 = clickyJobMessageRoute.TargetThreadId;
			}
		}
		text = text + "\n" + ClickyJobBrief.WorkFolderContext;
		Jobs.MarkWorking(job.Id);
		return (ThreadId: text2, Context: text);
	}

	private string? LeaveOldHarnessThread(AgentTask task, string source, bool preauthorized)
	{
		if (source == "retry" || preauthorized)
		{
			return null;
		}
		string threadId;
		lock (task.Gate)
		{
			threadId = task.ThreadId;
		}
		if (threadId == null || session.IsThreadActive(threadId) || AgentHarnessThreads.ContinuesOnNewConversationHarness(threadId))
		{
			return null;
		}
		lock (task.Gate)
		{
			task.ThreadId = null;
		}
		Log.Info("Harness", $"[harness] {Lock(task, () => task.ClickyName)}'s conversation {threadId} ran on another harness: a fresh conversation takes this message");
		return threadId;
	}

	public string JobContinuationContext(ClickyJob job, bool hasPreviousNote = false)
	{
		string value = JobConversationSoFar(job);
		string value2 = (hasPreviousNote ? ".clicky/previous-note.md has the page the last run left, and the folder has everything made earlier." : "The folder has everything made earlier.");
		string value3 = job.Outcome switch
		{
			ClickyJobOutcome.Failed => " The last run failed partway; carry on from what it got done.", 
			ClickyJobOutcome.Stopped => " The user stopped the last run partway through.", 
			_ => "", 
		};
		return $"This message continues work done earlier in this folder. You're picking it up fresh; here is the conversation so far, oldest first:\n<earlier_conversation>\n{value}\n</earlier_conversation>\n{value2} Open an earlier file only when this message needs it.{value3} Keep what was done; change what the user asks now. {"Answer in the language of this newest message."}";
	}

	public string JobConversationSoFar(ClickyJob job, int characterLimit = 24000)
	{
		List<string> list = CachedConversationExchanges(job.AllThreadIds);
		if (list.Count == 0)
		{
			list = (job.Runs ?? Array.Empty<ClickyJobRun>()).Select((ClickyJobRun run) => "They asked: " + run.Ask + "\nIt ended: " + (run.Summary ?? run.Outcome.ToString().ToLowerInvariant())).ToList();
			string text = job.ReplyText?.Trim();
			if (text != null && text.Length > 0)
			{
				list.Add("Your last reply, in full:\n" + text);
			}
		}
		return NewestExchanges(list, characterLimit);
	}

	private List<string> CachedConversationExchanges(IReadOnlyList<string> threadIds)
	{
		List<string> list = new List<string>();
		foreach (string threadId in threadIds)
		{
			IReadOnlyList<AgentTranscriptTurn> readOnlyList;
			try
			{
				readOnlyList = CachedTranscript?.Invoke(threadId);
			}
			catch (Exception)
			{
				readOnlyList = null;
			}
			if (readOnlyList == null || readOnlyList.Count <= 0)
			{
				IReadOnlyList<AgentTranscriptTurn> readOnlyList2;
				try
				{
					readOnlyList2 = HarnessRecordTranscript(threadId);
				}
				catch (Exception ex2)
				{
					Log.Warn("Agents", "H1 record of " + threadId + ": " + ex2.Message);
					readOnlyList2 = null;
				}
				IReadOnlyList<AgentTranscriptTurn>? obj = RolloutTranscript(threadId) ?? Array.Empty<AgentTranscriptTurn>();
				IReadOnlyList<AgentTranscriptTurn> readOnlyList3 = readOnlyList2 ?? Array.Empty<AgentTranscriptTurn>();
				int num = 0;
				AgentTranscriptTurn[] array = new AgentTranscriptTurn[obj.Count + readOnlyList3.Count];
				foreach (AgentTranscriptTurn item in obj)
				{
					array[num] = item;
					num++;
				}
				foreach (AgentTranscriptTurn item2 in readOnlyList3)
				{
					array[num] = item2;
					num++;
				}
				readOnlyList = new global::_003C_003Ez__ReadOnlyArray<AgentTranscriptTurn>(array);
			}
			foreach (AgentTranscriptTurn item3 in readOnlyList ?? Array.Empty<AgentTranscriptTurn>())
			{
				string text = string.Join("\n", from m in item3.UserMessages
					select DisplayUserText(m).Trim() into m
					where m.Length > 0
					select m);
				string text2 = string.Join("\n\n", from m in item3.AssistantMessages
					select m.Trim() into m
					where m.Length > 0
					select m);
				if (text.Length != 0 || text2.Length != 0)
				{
					list.Add("They asked: " + ((text.Length == 0) ? "(nothing new)" : text) + "\nYou replied:\n" + ((text2.Length == 0) ? "(no reply)" : text2));
				}
			}
		}
		return list;
	}

	private IReadOnlyList<AgentTranscriptTurn>? RolloutTranscript(string threadId)
	{
		if (AgentHarnessPreferences.IsHarnessThread(threadId))
		{
			return null;
		}
		try
		{
			return RolloutTranscriptTurns(threadId);
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "rollout of " + threadId + ": " + ex.Message);
			return null;
		}
	}

	public static string NewestExchanges(IReadOnlyList<string> exchanges, int characterLimit)
	{
		List<string> list = new List<string>();
		int num = 0;
		foreach (string item in exchanges.Reverse())
		{
			int num2 = characterLimit - num;
			if (num2 <= 400)
			{
				break;
			}
			string text = ((item.Length > num2) ? (item.Substring(0, num2) + "…") : item);
			list.Insert(0, text);
			num += text.Length;
		}
		if (list.Count != 0)
		{
			return string.Join("\n\n", list);
		}
		return "(nothing recorded)";
	}

	public string ClickyFreshConversationContext(string oldThreadId)
	{
		string text = NewestExchanges(CachedConversationExchanges(new _003C_003Ez__ReadOnlySingleElementList<string>(oldThreadId)), 24000);
		return "This message continues your earlier conversation with the user, which ran in another agent setup. You're picking it up fresh; here is the conversation so far, oldest first:\n<earlier_conversation>\n" + text + "\n</earlier_conversation>\nYour workspace (the working folder) has AGENTS.md with your notes and everything you made earlier; read AGENTS.md first, and open an earlier file only when this message needs it. Keep what was done; change what the user asks now. Answer in the language of this newest message.";
	}

	private static string LiveNotePathForJob(ClickyJob job)
	{
		return Path.GetFullPath(Path.Combine(job.FolderPath, "note.md"));
	}

	public bool RetireClickysNoteForMovedRoutineJob(ClickyJob job)
	{
		string text = LiveNotePathForJob(job);
		bool flag = ClickyWorkFolderFiles.RetireNote(job.FolderPath);
		try
		{
			LiveNotes?.Forget(text);
		}
		catch (Exception ex)
		{
			Log.Warn("Routines", "moved routine's note versions not forgotten: " + ex.Message);
		}
		LiveSteps.Clear(text);
		if (flag)
		{
			Log.Info("Routines", "job " + job.Id + ": its Clicky's note.md set aside to .clicky\\previous-note.md");
		}
		return flag;
	}

	private void NoteJobTaskEnded(AgentTask task, ClickyJobOutcome outcome, string? summary)
	{
		NoteLiveNoteTaskEnded(task);
		string jobId;
		bool requiresExtraUsageApproval;
		string text;
		string summary2;
		string failureMessage;
		IReadOnlyList<string> artifacts;
		ClickyJobUserStep pendingUserStep;
		IReadOnlyList<string> learnedHowTos;
		string threadId;
		lock (task.Gate)
		{
			jobId = task.JobId;
			requiresExtraUsageApproval = task.RequiresExtraUsageApproval;
			text = task.FinalResponseMarkdown?.Trim();
			summary2 = task.Summary;
			failureMessage = task.FailureMessage;
			artifacts = task.Artifacts;
			pendingUserStep = task.PendingUserStep;
			learnedHowTos = task.LearnedHowTos;
			threadId = task.ThreadId;
		}
		if (jobId == null)
		{
			return;
		}
		ClickyJob clickyJob = Jobs.Job(jobId);
		if ((object)clickyJob != null && !(outcome == ClickyJobOutcome.Failed && requiresExtraUsageApproval))
		{
			string summary3 = ((outcome == ClickyJobOutcome.Done) ? summary2 : failureMessage) ?? summary;
			Jobs.MarkEnded(clickyJob.Id, outcome, summary3, string.IsNullOrEmpty(text) ? summary : text, artifacts);
			try
			{
				AppendJobReport(Jobs.Job(clickyJob.Id) ?? clickyJob, task, outcome, summary3);
			}
			catch (Exception ex)
			{
				Log.Warn("Jobs", "report.md for " + clickyJob.Id + ": " + ex.Message);
			}
			Jobs.SetWaitingStep(clickyJob.Id, (outcome == ClickyJobOutcome.Done) ? pendingUserStep : null);
			_ = StepWatcher;
			if (outcome == ClickyJobOutcome.Done)
			{
				LearnedHowTos.Record(learnedHowTos, clickyJob.Id);
			}
			RaiseJobAction("ended", threadId, new Dictionary<string, object>
			{
				["outcome"] = outcome.ToString().ToLowerInvariant(),
				["artifact_count"] = artifacts.Count
			});
		}
	}

	public void TrackJobAction(string action, string? threadId = null, IReadOnlyDictionary<string, object?>? properties = null)
	{
		RaiseJobAction(action, threadId, properties);
	}

	private void RaiseJobAction(string action, string? threadId, IReadOnlyDictionary<string, object?>? properties = null)
	{
		Dictionary<string, object> arg = new Dictionary<string, object>(properties ?? new Dictionary<string, object>()) { ["action"] = action };
		try
		{
			this.JobAction?.Invoke(action, threadId, arg);
		}
		catch (Exception ex)
		{
			Log.Warn("Jobs", "JobAction handler threw: " + ex.Message);
		}
	}

	public void DismissJob(string jobId)
	{
		ClickyJob clickyJob = Jobs.Job(jobId);
		if ((object)clickyJob != null)
		{
			Jobs.Dismiss(jobId);
			RaiseJobAction("dismissed", clickyJob.ThreadId);
		}
	}

	public void NoteJobCardClosed(string jobId)
	{
		ClickyJob clickyJob = Jobs.Job(jobId);
		if ((object)clickyJob != null && clickyJob.IsFinished && clickyJob.IsQuickActionWithoutOutput)
		{
			Jobs.MarkSeen(jobId);
		}
	}

	public void PinJob(string jobId)
	{
		ClickyJob clickyJob = Jobs.Job(jobId);
		if ((object)clickyJob != null)
		{
			Jobs.KeepOnDock(jobId);
			RaiseJobAction("pinned", clickyJob.ThreadId);
		}
	}

	public async Task StopJobAsync(string jobId)
	{
		ClickyJob job = Jobs.Job(jobId);
		if ((object)job != null)
		{
			AgentTask agentTask = LiveTask(job);
			if (agentTask != null)
			{
				await StopAsync(agentTask.Id).ConfigureAwait(continueOnCapturedContext: false);
				RaiseJobAction("stopped", job.ThreadId);
			}
		}
	}

	public void ContinueJobAfterUserStep(string jobId, string? reachedAddress = null)
	{
		ClickyJob clickyJob = Jobs.Job(jobId);
		if ((object)clickyJob == null)
		{
			return;
		}
		ClickyJobUserStep waitingStep = clickyJob.WaitingStep;
		if ((object)waitingStep != null)
		{
			string threadId = clickyJob.ThreadId;
			if (threadId != null)
			{
				string launchContext = ((reachedAddress != null) ? $"HeyClicky saw the user's browser reach {reachedAddress}, which you said means this step is done: \"{waitingStep.Step}\". Check it really went through (look at the page) before carrying on; if it didn't, say what's left." : ("The user tapped I'm done on the step you handed them: \"" + waitingStep.Step + "\". Check it went through before carrying on."));
				LaunchAsync(new AgentLaunchRequest
				{
					Prompt = "Done: " + waitingStep.Step,
					JobId = clickyJob.Id,
					TargetThreadId = threadId,
					LaunchContext = launchContext,
					LaunchSource = "text",
					IsHudFollowUp = true
				});
				RaiseJobAction("user_step_done", threadId, new Dictionary<string, object> { ["via"] = ((reachedAddress == null) ? "button" : "browser_page") });
			}
		}
	}

	public string? JobsContextNoteForBrainTurn(ISet<Guid>? liveSuggestionCardIds = null, Func<string, string?>? routineLine = null, IReadOnlyList<string>? extraLines = null)
	{
		if (!ClickyJobsPreferences.IsEnabled)
		{
			return null;
		}
		List<string> list = ClickyJobsNoteSelection.Select(Jobs.Jobs, liveSuggestionCardIds, RailPhase, (ClickyJob job) => Repeats(job) != null).Select(delegate(ClickyJob job)
		{
			if (job.IsWaitingToStart)
			{
				return "- \"" + job.Title + "\" — OFFERED, NOT STARTED: it waits on the dock until they click it. When they say yes to it, sessions_spawn with this exact title as the label: that starts this one, never a second job beside it.";
			}
			if (!job.IsHeldForChrome)
			{
				string value;
				switch (RailPhase(job))
				{
				case ClickyJobRailPhase.Queued:
					value = "STARTING";
					break;
				case ClickyJobRailPhase.Working:
					value = "WORKING NOW";
					break;
				case ClickyJobRailPhase.NeedsYou:
				{
					AgentTask live = LiveTask(job);
					string text = live?.PendingComputerUseRequest;
					if (text != null)
					{
						value = "WAITING ON THE USER to let you drive one step on their PC: " + Cap(text, 140) + " (their yes or no → sessions_answer_request, request computer_use, this job_id)";
					}
					else if (live != null && Lock(live, () => live.RequiresExtraUsageApproval))
					{
						value = "WAITING ON THE USER for more agent messages (their yes or no → sessions_answer_request, request extra_usage, this job_id)";
					}
					else if (live != null && Lock(live, () => live.NeedsChromeConnection))
					{
						value = "WAITING ON THE USER to choose about HeyClicky's Chrome extension, which it needs for a website (their answer → sessions_answer_request, request chrome_setup, this job_id: " + ChromeAnswerMeanings() + ", decline = it goes on without it; it carries on by itself once Chrome is connected)";
					}
					else
					{
						ClickyJobUserStep waitingStep = job.WaitingStep;
						value = "WAITING ON THE USER" + (((object)waitingStep != null) ? (" to: " + Cap(waitingStep.Step, 140) + " (when they say it's done, sessions_send it; " + ((waitingStep.DoneWhenUrlContains == null) ? "it waits for them)" : "it also continues by itself once their browser shows it's done)")) : "");
					}
					break;
				}
				case ClickyJobRailPhase.Done:
				{
					string obj = (job.IsSeen ? "done" : "done, not opened yet");
					string summary = job.Summary;
					value = obj + ((summary != null) ? (": " + Cap(summary, 140)) : "");
					break;
				}
				case ClickyJobRailPhase.Failed:
					value = "failed";
					break;
				default:
					value = "stopped";
					break;
				}
				string text2 = Repeats(job);
				string value2 = ((text2 != null) ? (", " + text2) : "");
				return $"- \"{job.Title}\" — job_id: {job.Id}, {value}{value2}";
			}
			return $"- \"{job.Title}\" — job_id: {job.Id}, not started, WAITING ON THE USER to choose about HeyClicky's Chrome extension before it starts " + "(their answer → sessions_answer_request, request chrome_setup, no job_id: " + ChromeAnswerMeanings() + ", decline = it starts now without it)";
		}).ToList();
		list.AddRange(extraLines ?? Array.Empty<string>());
		if (list.Count == 0)
		{
			int num = 1;
			List<string> list2 = new List<string>(num);
			CollectionsMarshal.SetCount(list2, num);
			Span<string> span = CollectionsMarshal.AsSpan(list2);
			int index = 0;
			span[index] = "(nothing needs attention; find_jobs finds earlier work)";
			list = list2;
		}
		return "[Your jobs] The jobs that need attention now (working, waiting on them, offered, not opened yet, repeating) and the six most recent others, oldest first. Address a job by its job_id; find_jobs finds any other job by what it was about. A job that repeats names its routine_id: its schedule is the routines tool's, never sessions_send." + "\n" + string.Join("\n", list);
		string? Repeats(ClickyJob job)
		{
			string text3 = routineLine?.Invoke(job.Id);
			if (text3 == null || text3.Length <= 0)
			{
				return null;
			}
			return text3;
		}
	}

	private static string Cap(string s, int n)
	{
		if (s.Length <= n)
		{
			return s;
		}
		return s.Substring(0, n);
	}

	public string? WorkContextNoteForBrainTurn(ISet<Guid>? liveSuggestionCardIds = null)
	{
		if (!ClickyJobsPreferences.IsEnabled)
		{
			return null;
		}
		string text = JobsContextNoteForBrainTurn(liveSuggestionCardIds, routines.JobRoutineLine, EarlierClickyJobLinesForBrain());
		return string.Join("\n\n", new string[2]
		{
			text,
			RoutinesTool.ContextNote()
		}.OfType<string>());
	}

	private IReadOnlyList<string> EarlierClickyJobLinesForBrain()
	{
		return clickys.Clickys.Select(delegate(ClickyInfo clicky)
		{
			string threadId = clicky.ThreadId;
			if (threadId == null)
			{
				return (string)null;
			}
			AgentTask live = tasks.Values.LastOrDefault((AgentTask t) => Lock(t, () => !t.IsDemo && (t.ClickySlug == clicky.Slug || t.ThreadId == threadId)));
			string text = live?.PendingComputerUseRequest;
			string value;
			if (text != null)
			{
				value = "WAITING ON THE USER to let you drive one step on their PC: " + Cap(text, 140) + " (their yes or no → sessions_answer_request, request computer_use, this job_id)";
			}
			else if (live != null && Lock(live, () => live.RequiresExtraUsageApproval))
			{
				value = "WAITING ON THE USER for more agent messages (their yes or no → sessions_answer_request, request extra_usage, this job_id)";
			}
			else
			{
				if (live == null || !Lock(live, () => live.IsActive))
				{
					return (string)null;
				}
				string text2 = LiveWorkDescription(live);
				value = "WORKING NOW" + ((text2 != null) ? (": " + text2) : "");
			}
			ClickyRoutine clickyRoutine = routines.ForClicky(clicky.Slug).FirstOrDefault();
			string value2 = ((clickyRoutine != null) ? (", repeats (routine_id " + clickyRoutine.Id + ")") : "");
			string text3 = clicky.OneLiner.Trim();
			return "- \"" + clicky.Name + "\"" + ((text3.Length == 0) ? "" : (" (" + text3 + ")")) + $" — job_id: {threadId}, {value}{value2}";
		}).OfType<string>().ToList();
	}

	private static string? LiveWorkDescription(AgentTask task)
	{
		lock (task.Gate)
		{
			List<string> list = new List<string>();
			string text2 = Clipped(task.UserPrompts.LastOrDefault() ?? "", 140);
			if (text2.Length > 0)
			{
				list.Add("on \"" + text2 + "\"");
			}
			int num = (int)(DateTimeOffset.Now - task.StartedAt).TotalMinutes;
			list.Add((num < 1) ? "for under a minute" : $"for {num} min");
			string latestCommentary = task.LatestCommentary;
			if (latestCommentary != null && latestCommentary.Length > 0)
			{
				list.Add("latest update \"" + Clipped(latestCommentary, 160) + "\"");
			}
			else if (task.Status == AgentTaskStatus.Starting)
			{
				list.Add("waiting to start");
			}
			return string.Join(", ", list);
		}
		static string Clipped(string text, int limit)
		{
			string text3 = text.Replace("\r", " ").Replace("\n", " ").Trim();
			if (text3.Length <= limit)
			{
				return text3;
			}
			return text3.Substring(0, limit) + "…";
		}
	}

	private void SyncPendingRequest(AgentTask task)
	{
		ClickyPendingAgentRequest request = null;
		string threadId;
		lock (task.Gate)
		{
			threadId = task.ThreadId;
			if (threadId == null || task.IsDemo)
			{
				return;
			}
			ClickyPendingAgentRequest.RequestKind? requestKind = ((task.PendingComputerUseRequest != null) ? new ClickyPendingAgentRequest.RequestKind?(ClickyPendingAgentRequest.RequestKind.ComputerUse) : (task.RequiresExtraUsageApproval ? new ClickyPendingAgentRequest.RequestKind?(ClickyPendingAgentRequest.RequestKind.ExtraUsage) : ((ClickyPendingAgentRequest.RequestKind?)null)));
			if (requestKind.HasValue)
			{
				ClickyPendingAgentRequest.RequestKind valueOrDefault = requestKind.GetValueOrDefault();
				request = new ClickyPendingAgentRequest
				{
					ThreadId = threadId,
					Kind = valueOrDefault,
					RequestText = (task.PendingComputerUseRequest ?? ""),
					LeaseId = task.Lease?.LeaseId,
					LeaseTurnId = task.Lease?.TurnId,
					ClickySlug = task.ClickySlug,
					JobId = task.JobId,
					Title = (task.DoneTitle ?? task.Title),
					Prompt = Cap(task.UserPrompts.LastOrDefault() ?? "", 4000),
					FinalSummary = task.Summary,
					AskedAt = DateTimeOffset.Now
				};
			}
		}
		ClickyPendingAgentRequestStore.Sync(request, threadId);
	}

	public void RestorePendingAgentRequestsFromLastRun()
	{
		IReadOnlyList<ClickyPendingAgentRequest> readOnlyList = ClickyPendingAgentRequestStore.All();
		if (readOnlyList.Count == 0)
		{
			return;
		}
		bool flag = false;
		foreach (ClickyPendingAgentRequest record in readOnlyList)
		{
			if (tasks.Values.Any((AgentTask t) => Lock(t, () => t.ThreadId) == record.ThreadId))
			{
				continue;
			}
			AgentTask agentTask = new AgentTask
			{
				ThreadId = record.ThreadId,
				StartedAt = record.AskedAt,
				Status = ((record.Kind == ClickyPendingAgentRequest.RequestKind.ComputerUse) ? AgentTaskStatus.Finished : AgentTaskStatus.WaitingForUser),
				ProgressSummary = ""
			};
			lock (agentTask.Gate)
			{
				agentTask.ClickySlug = record.ClickySlug;
				string clickySlug = record.ClickySlug;
				agentTask.ClickyName = ((clickySlug == null) ? null : clickys.Get(clickySlug)?.Name);
				agentTask.JobId = record.JobId;
				agentTask.Title = record.Title ?? agentTask.Title;
				agentTask.Summary = record.FinalSummary;
				agentTask.UserPrompts.Add(record.Prompt);
				string leaseId = record.LeaseId;
				if (leaseId != null)
				{
					agentTask.Lease = new AgentTurnLease(leaseId, record.LeaseTurnId ?? "", null, 0, 0, null, null, record.Kind == ClickyPendingAgentRequest.RequestKind.ExtraUsage, null);
				}
				if (record.Kind == ClickyPendingAgentRequest.RequestKind.ComputerUse)
				{
					agentTask.PendingComputerUseRequest = record.RequestText;
				}
				else
				{
					agentTask.RequiresExtraUsageApproval = true;
				}
				string jobId = record.JobId;
				object workspacePath;
				if (jobId != null)
				{
					workspacePath = Jobs.Job(jobId)?.FolderPath;
				}
				else
				{
					string clickySlug2 = record.ClickySlug;
					workspacePath = ((clickySlug2 != null) ? clickys.WorkspacePath(clickySlug2) : null);
				}
				agentTask.WorkspacePath = (string?)workspacePath;
			}
			tasks[agentTask.Id] = agentTask;
			flag = true;
		}
		if (flag)
		{
			Log.Info("Agents", $"[pending requests] restored {readOnlyList.Count} waiting question(s) from the last run");
			RaiseTasksChanged();
		}
	}

	public string? FullReplyText(string taskId)
	{
		if (!tasks.TryGetValue(taskId, out AgentTask value))
		{
			return null;
		}
		List<string> list;
		lock (value.Gate)
		{
			list = value.CompletedReplies.Select((string r) => StructuredFinalReply.Parse(r).Text).ToList();
			string finalResponseMarkdown = value.FinalResponseMarkdown;
			if (finalResponseMarkdown != null && finalResponseMarkdown.Trim() != list.LastOrDefault()?.Trim())
			{
				list.Add(finalResponseMarkdown);
			}
		}
		string text = string.Join("\n\n", from m in list
			select m.Trim() into m
			where m.Length > 0
			select m);
		if (text.Length == 0)
		{
			return null;
		}
		if (text.Length <= 40000)
		{
			return text;
		}
		return text.Substring(0, 39999) + "…";
	}

	public string? ResultReport(string taskId)
	{
		AgentTaskDetail taskDetail = GetTaskDetail(taskId);
		if ((object)taskDetail == null)
		{
			return null;
		}
		List<string> list = new List<string>();
		string text = taskDetail.Answer?.ReportJson;
		if (text != null)
		{
			list.Add("Its facts, to build their card from: " + text);
		}
		string text2 = FullReplyText(taskId);
		if (text2 != null)
		{
			string text3 = ((text2.Length > 6000) ? (text2.Substring(0, 6000) + "… (cut here; clicky_memory has the rest)") : text2);
			list.Add("Its full report, which they can open from the chat:\n<report>\n" + text3 + "\n</report>");
		}
		if (taskDetail.Artifacts.Count > 0)
		{
			list.Add("Files it made: " + string.Join(", ", taskDetail.Artifacts.Take(6)));
		}
		if (list.Count != 0)
		{
			return string.Join("\n", list);
		}
		return null;
	}

	public bool NoteResultCard(string threadId, ClickyAnswer card)
	{
		if (tasks.Values.FirstOrDefault((AgentTask t) => Lock(t, () => t.ThreadId) == threadId && Lock(t, () => t.Status) == AgentTaskStatus.Finished) == null)
		{
			return false;
		}
		resultCards[threadId] = card;
		ClickyJob clickyJob = Jobs.JobForThread(threadId);
		if ((object)clickyJob != null)
		{
			Jobs.SetResultCard(clickyJob.Id, card);
		}
		try
		{
			this.ResultCardShown?.Invoke(threadId);
		}
		catch (Exception ex)
		{
			Log.Warn("Jobs", "ResultCardShown handler threw: " + ex.Message);
		}
		return true;
	}

	public ClickyAnswer? ResultCard(string threadId)
	{
		if (!resultCards.TryGetValue(threadId, out ClickyAnswer value))
		{
			return Jobs.JobForThread(threadId)?.ResultCard;
		}
		return value;
	}

	private void NoteRunningTurnsForRestart()
	{
		List<(string, string, DateTimeOffset)> running = (from t in tasks.Values
			select Lock(t, () => (Status: t.Status, IsDemo: t.IsDemo, JobId: t.JobId, ClickySlug: t.ClickySlug, ThreadId: t.ThreadId, StartedAt: t.StartedAt)) into t
			where t.Status == AgentTaskStatus.Running && !t.IsDemo && t.JobId == null && t.ClickySlug != null
			select (t.ClickySlug, ThreadId: t.ThreadId, StartedAt: t.StartedAt)).ToList();
		RestartInterruptions.NoteRunning(running);
	}

	private void RemoveJobsForSignOut()
	{
		routines.RemoveAllJobRoutinesForSignOut();
		Jobs.RemoveAllForSignOut();
		Jobs.WorkFolderFiles.ResetForSignOut(JobsRootPath);
		ClickyHelperFiles.RemoveAllForSignOut();
		LearnedHowTos.RemoveAllForSignOut();
		ClickyPendingAgentRequestStore.RemoveAllForSignOut();
		RestartInterruptions.Reset();
	}

	public IReadOnlyList<string> DeleteAllLocalClickys()
	{
		if (tasks.Values.Any((AgentTask t) => Lock(t, () => t.IsActive)))
		{
			throw new InvalidOperationException("Stop the running agents first.");
		}
		IReadOnlyList<string> readOnlyList = clickys.DeleteAll();
		foreach (ClickyRoutine routine in routines.Routines)
		{
			routines.Delete(routine.Id, "debug_delete_all");
		}
		foreach (AgentTask item in tasks.Values.Where((AgentTask t) => t.ClickySlug != null).ToList())
		{
			tasks.TryRemove(item.Id, out AgentTask _);
		}
		RaiseTasksChanged();
		Log.Info("Agents", $"deleted all local Clickys ({readOnlyList.Count} workspace folder(s) to recycle)");
		return readOnlyList;
	}

	public string? SyncWorkOwnerStamp()
	{
		string text;
		try
		{
			text = ClickyWorkIndex.OwnerStamp(CurrentUserId?.Invoke());
		}
		catch (Exception)
		{
			text = null;
		}
		Jobs.WorkFolderFiles.OwnerStamp = text;
		return text;
	}

	public void ForgetWorkReportsForDeletedAccount(string? deletedAccountId)
	{
		Jobs.WorkFolderFiles.RemoveReports(deletedAccountId, JobsRootPath);
		Jobs.WorkFolderFiles.ResetForSignOut(JobsRootPath);
	}

	private void AppendJobReport(ClickyJob job, AgentTask task, ClickyJobOutcome outcome, string? summary)
	{
		string turnKey;
		string text;
		IReadOnlyList<string> artifacts;
		lock (task.Gate)
		{
			turnKey = task.TurnId ?? $"{task.Id}:{task.UserPrompts.Count}";
			text = StructuredFinalReply.Parse(task.FinalResponseMarkdown ?? "").Text;
			artifacts = task.Artifacts;
		}
		if (Jobs.WorkFolderFiles.ClaimReport(job.Id, turnKey))
		{
			SyncWorkOwnerStamp();
			string section = ClickyWorkIndex.ReportSection(DateTimeOffset.Now, ClickyWorkIndex.ReportOutcomeText(outcome), job.Runs?.LastOrDefault()?.Ask ?? job.Prompt, text, summary, artifacts, job.FolderPath);
			Jobs.WorkFolderFiles.AppendReportSection(section, job.Title, job.Id, job.FolderPath);
		}
	}

	public void BackfillWorkReportsIfNeeded()
	{
		string ownerStamp = SyncWorkOwnerStamp();
		if (ownerStamp == null)
		{
			return;
		}
		string flagKey = "clicky.work.reportsBackfilled.v2." + ownerStamp;
		ClickyWorkFolderFiles files = Jobs.WorkFolderFiles;
		lock (files)
		{
			if (AgentDefaults.Bool(flagKey) || files.IsBackfillingReports)
			{
				return;
			}
			files.IsBackfillingReports = true;
		}
		string jobsRootPath = ClickyWorkIndex.StandardizedPath(JobsRootPath);
		IReadOnlyList<ClickyJob> jobs = Jobs.Jobs;
		HashSet<string> accountFolders = jobs.Select((ClickyJob j) => ClickyWorkIndex.StandardizedPath(j.FolderPath)).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		files.StampFolders(accountFolders.ToList());
		Task.Run(async delegate
		{
			try
			{
				List<(string, IReadOnlyList<ClickyHarnessRecord.Turn>)> list = new List<(string, IReadOnlyList<ClickyHarnessRecord.Turn>)>();
				foreach (var item2 in ClickyHarnessRecord.AllRecords(HarnessRecordsRoot))
				{
					string text = ClickyWorkIndex.StandardizedPath(item2.Workspace);
					if (accountFolders.Contains(text) && text.StartsWith(jobsRootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && Directory.Exists(text) && !File.Exists(Path.Combine(text, "report.md")))
					{
						string text2 = ClickyHarnessRecord.RecordDirectory(HarnessRecordsRoot, item2.ThreadId);
						if (text2 != null)
						{
							list.Add((text, ClickyHarnessRecord.Turns(text2)));
						}
					}
				}
				Dictionary<string, List<(DateTimeOffset Date, string Text)>> sectionsByFolder = new Dictionary<string, List<(DateTimeOffset, string)>>(StringComparer.OrdinalIgnoreCase);
				string key;
				foreach (var (workspace, turns) in list)
				{
					await Task.Yield();
					foreach (ClickyHarnessRecord.Turn item3 in turns)
					{
						string finalText = item3.FinalText;
						if (finalText != null)
						{
							StructuredFinalReply structuredFinalReply = StructuredFinalReply.Parse(finalText);
							string status = item3.Status;
							key = status switch
							{
								"completed" => "done", 
								"interrupted" => "stopped by the user", 
								null => "ended", 
								_ => status, 
							};
							string outcome = key;
							DateTimeOffset dateTimeOffset = item3.EndedAt ?? DateTimeOffset.MinValue;
							string item = ClickyWorkIndex.ReportSection(dateTimeOffset, outcome, DisplayUserText(item3.RawAsk), structuredFinalReply.Text, structuredFinalReply.Summary, structuredFinalReply.Artifacts, workspace);
							if (!sectionsByFolder.TryGetValue(workspace, out List<(DateTimeOffset, string)> value))
							{
								value = (sectionsByFolder[workspace] = new List<(DateTimeOffset, string)>());
							}
							value.Add((dateTimeOffset, item));
						}
					}
				}
				foreach (ClickyJob job in jobs.Where((ClickyJob j) => !j.IsWaitingToStart))
				{
					string text3 = ClickyWorkIndex.StandardizedPath(job.FolderPath);
					if (!sectionsByFolder.ContainsKey(text3))
					{
						IReadOnlyList<ClickyJobRun> runs = job.Runs;
						if (runs != null && runs.Count > 0 && !File.Exists(Path.Combine(text3, "report.md")))
						{
							sectionsByFolder[text3] = runs.Select(delegate(ClickyJobRun run, int index)
							{
								DateTimeOffset? finishedAt = run.FinishedAt;
								if (!finishedAt.HasValue)
								{
									return ((DateTimeOffset, string)?)null;
								}
								DateTimeOffset valueOrDefault = finishedAt.GetValueOrDefault();
								bool flag = index == runs.Count - 1;
								string outcome2 = ClickyWorkIndex.ReportOutcomeText(run.Outcome);
								string ask = run.Ask;
								string reply = (flag ? (job.ReplyText ?? "") : "");
								string? summary = run.Summary;
								IReadOnlyList<string> files2;
								if (!flag)
								{
									IReadOnlyList<string> readOnlyList = Array.Empty<string>();
									files2 = readOnlyList;
								}
								else
								{
									files2 = job.ArtifactEntries;
								}
								return (valueOrDefault, ClickyWorkIndex.ReportSection(valueOrDefault, outcome2, ask, reply, summary, files2, job.FolderPath));
							}).OfType<(DateTimeOffset, string)>().ToList();
						}
					}
				}
				if (!(files.OwnerStamp != ownerStamp))
				{
					foreach (KeyValuePair<string, List<(DateTimeOffset, string)>> item4 in sectionsByFolder)
					{
						item4.Deconstruct(out key, out var value2);
						string folder = key;
						List<(DateTimeOffset Date, string Text)> list3 = value2;
						if (list3.Count != 0)
						{
							ClickyJob? clickyJob = jobs.FirstOrDefault((ClickyJob j) => string.Equals(ClickyWorkIndex.StandardizedPath(j.FolderPath), folder, StringComparison.OrdinalIgnoreCase));
							string fileName = Path.GetFileName(folder);
							string jobId = clickyJob?.Id ?? ClickyWorkIndex.JobIdFromFolderName(fileName);
							string title = clickyJob?.Title ?? ClickyWorkIndex.TitleFromFolderName(fileName);
							files.WriteReportIfMissing(ClickyWorkIndex.ReportHeader(title, jobId) + "\n" + string.Join("\n", from s in list3
								orderby s.Date
								select s.Text), folder);
						}
					}
					AgentDefaults.Set(flagKey, value: true);
					files.Refresh(Jobs.Jobs, force: true);
					Log.Info("Jobs", $"[work] report backfill wrote {sectionsByFolder.Count<KeyValuePair<string, List<(DateTimeOffset, string)>>>((KeyValuePair<string, List<(DateTimeOffset Date, string Text)>> s) => s.Value.Count > 0)} folder(s)");
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Jobs", "[work] report backfill: " + ex.Message);
			}
			finally
			{
				lock (files)
				{
					files.IsBackfillingReports = false;
				}
			}
		});
	}

	private IReadOnlyList<ClickyWorkEntry> ListedWorkEntries()
	{
		List<ClickyWorkEntry> list3 = Jobs.Jobs.Select(delegate(ClickyJob job)
		{
			List<string> list = (from r in job.Runs ?? Array.Empty<ClickyJobRun>()
				select r.Ask into a
				where a.Length > 0
				select a).ToList();
			List<string> list2;
			if (list.Count != 0)
			{
				list2 = list;
			}
			else
			{
				int num = 1;
				list2 = new List<string>(num);
				CollectionsMarshal.SetCount(list2, num);
				Span<string> span = CollectionsMarshal.AsSpan(list2);
				int index = 0;
				span[index] = job.Prompt;
			}
			ClickyWorkEntry obj = new ClickyWorkEntry
			{
				Id = job.Id,
				Title = job.Title,
				LastActive = (job.FinishedAt ?? job.Runs?.LastOrDefault()?.StartedAt ?? job.CreatedAt),
				Asks = list2,
				Result = job.Summary,
				State = WorkStateText(job),
				FolderPath = job.FolderPath
			};
			return obj;
		}).ToList();
		foreach (ClickyInfo clicky in clickys.Clickys)
		{
			string? threadId = clicky.ThreadId;
			if (threadId != null)
			{
				IReadOnlyList<string> asks;
				if (clicky.OneLiner.Length != 0)
				{
					IReadOnlyList<string> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<string>(clicky.OneLiner);
					asks = readOnlyList;
				}
				else
				{
					IReadOnlyList<string> readOnlyList = Array.Empty<string>();
					asks = readOnlyList;
				}
				ClickyWorkEntry obj2 = new ClickyWorkEntry
				{
					Id = threadId,
					Title = clicky.Name,
					LastActive = ClickyLastActive(clicky),
					Asks = asks,
					State = (IsClickyWorking(clicky.Slug) ? "working now" : "done earlier"),
					FolderPath = clickys.WorkspacePath(clicky.Slug),
					IsEarlierClicky = true
				};
				list3.Add(obj2);
			}
		}
		return list3;
	}

	private DateTimeOffset ClickyLastActive(ClickyInfo clicky)
	{
		DateTimeOffset dateTimeOffset = (from t in tasks.Values
			where Lock(t, () => t.ClickySlug == clicky.Slug || (clicky.ThreadId != null && t.ThreadId == clicky.ThreadId))
			select Lock(t, () => t.StartedAt)).DefaultIfEmpty(DateTimeOffset.MinValue).Max();
		if (!(dateTimeOffset > clicky.CreatedAt))
		{
			return clicky.CreatedAt;
		}
		return dateTimeOffset;
	}

	private bool IsClickyWorking(string slug)
	{
		return tasks.Values.Any((AgentTask t) => Lock(t, () => t.ClickySlug == slug && t.IsActive));
	}

	public string WorkStateText(ClickyJob job)
	{
		if (job.IsWaitingToStart)
		{
			return "offered, not started";
		}
		if (job.IsHeldForChrome)
		{
			return "waiting on their Chrome choice before it starts";
		}
		return RailPhase(job) switch
		{
			ClickyJobRailPhase.Queued => "starting", 
			ClickyJobRailPhase.Working => "working now", 
			ClickyJobRailPhase.NeedsYou => "waiting on the user", 
			ClickyJobRailPhase.Done => "done", 
			ClickyJobRailPhase.Failed => "failed", 
			_ => "stopped", 
		};
	}

	public async Task<JsonObject> FindJobsAsync(string query, int limit, bool forHelper, string? callingThreadId = null)
	{
		IReadOnlyList<ClickyWorkEntry> listed = ListedWorkEntries();
		List<string> listedFolders = listed.Select((ClickyWorkEntry e) => e.FolderPath).OfType<string>().ToList();
		string jobsRootPath = JobsRootPath;
		string callingJobId = ((callingThreadId == null) ? null : (Jobs.JobForThread(callingThreadId)?.Id ?? callingThreadId));
		string ownerStamp = SyncWorkOwnerStamp();
		JsonArray jsonArray = new JsonArray(((IEnumerable<ClickyWorkEntry>)(await Task.Run(() => ClickyWorkIndex.Search(ClickyWorkIndex.WithReports(listed).Concat(ClickyWorkIndex.UnlistedFolderEntries(jobsRootPath, listedFolders, ownerStamp)).ToList(), query, limit)).ConfigureAwait(continueOnCapturedContext: false))).Select((Func<ClickyWorkEntry, JsonNode>)((ClickyWorkEntry m) => ClickyWorkIndex.ResultObject(m, forHelper, m.Id == callingJobId))).ToArray());
		string text = query.Trim();
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = ((jsonArray.Count == 0) ? "none" : "found"),
			["jobs"] = jsonArray
		};
		if (jsonArray.Count == 0)
		{
			jsonObject["message"] = ((text.Length == 0) ? "No earlier work on this PC yet." : ("Nothing matches \"" + text + "\". Try other words (a name, a site, a topic)."));
		}
		return jsonObject;
	}

	private string? UnlistedJobFolder(string jobId, string ownerStamp)
	{
		string[] source;
		try
		{
			source = (Directory.Exists(JobsRootPath) ? Directory.GetDirectories(JobsRootPath) : Array.Empty<string>());
		}
		catch (Exception)
		{
			source = Array.Empty<string>();
		}
		return source.Where(delegate(string path)
		{
			string fileName = Path.GetFileName(path);
			return !fileName.StartsWith('.') && (ClickyWorkIndex.JobIdFromFolderName(fileName) == jobId || fileName == jobId);
		}).FirstOrDefault((string path) => ClickyWorkIndex.FolderOwnerStamp(path) == ownerStamp);
	}

	public (string Id, string Title, string? Folder)? EarlierWorkElsewhere(string jobId)
	{
		jobId = jobId.Trim();
		if (jobId.Length == 0)
		{
			return null;
		}
		ClickyInfo clickyInfo = clickys.Clickys.FirstOrDefault((ClickyInfo c) => c.ThreadId == jobId);
		if ((object)clickyInfo != null)
		{
			return (jobId, clickyInfo.Name, clickys.WorkspacePath(clickyInfo.Slug));
		}
		string text = SyncWorkOwnerStamp();
		if (text != null)
		{
			string text2 = UnlistedJobFolder(jobId, text);
			if (text2 != null)
			{
				return (jobId, ClickyWorkIndex.TitleFromFolderName(Path.GetFileName(text2.TrimEnd('\\', '/'))), text2);
			}
		}
		return null;
	}

	public async Task<JsonObject> ReadJobAsync(string rawJobId, string detail)
	{
		string jobId = rawJobId.Trim();
		if (jobId.Length == 0)
		{
			return new JsonObject((JsonNodeOptions?)null) { ["error"] = "Say which job: its job_id from find_jobs." };
		}
		ClickyJob job = Jobs.Job(jobId) ?? Jobs.JobForThread(jobId);
		ClickyInfo clicky = (((object)job == null) ? clickys.Clickys.FirstOrDefault((ClickyInfo c) => c.ThreadId == jobId) : null);
		string text = null;
		if ((object)job == null && (object)clicky == null)
		{
			string ownerStamp = SyncWorkOwnerStamp();
			if (ownerStamp != null)
			{
				text = await Task.Run(() => UnlistedJobFolder(jobId, ownerStamp)).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		string folderPath = job?.FolderPath ?? text ?? (((object)clicky == null) ? null : clickys.WorkspacePath(clicky.Slug));
		if ((object)job == null && (object)clicky == null && text == null)
		{
			return new JsonObject((JsonNodeOptions?)null) { ["error"] = "No job with that job_id. find_jobs lists them." };
		}
		string title = job?.Title ?? clicky?.Name ?? ClickyWorkIndex.TitleFromFolderName((folderPath == null) ? jobId : Path.GetFileName(folderPath.TrimEnd('\\', '/')));
		if (detail != "conversation")
		{
			string text3 = await Task.Run(delegate
			{
				try
				{
					string text2 = ((folderPath == null) ? null : Path.Combine(folderPath, "report.md"));
					return (text2 != null && File.Exists(text2)) ? File.ReadAllText(text2) : null;
				}
				catch (Exception)
				{
					return (string)null;
				}
			}).ConfigureAwait(continueOnCapturedContext: false);
			if (text3 != null && text3.Length > 0)
			{
				return new JsonObject((JsonNodeOptions?)null)
				{
					["job_id"] = job?.Id ?? jobId,
					["title"] = title,
					["report"] = ClickyWorkIndex.TrimmedReport(text3, 24000)
				};
			}
			if ((object)job != null)
			{
				string text6 = string.Join("\n", (job.Runs ?? Array.Empty<ClickyJobRun>()).Select(delegate(ClickyJobRun run)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 2);
					defaultInterpolatedStringHandler.AppendLiteral("- ");
					DateTimeOffset? finishedAt = run.FinishedAt;
					object value;
					if (finishedAt.HasValue)
					{
						DateTimeOffset valueOrDefault = finishedAt.GetValueOrDefault();
						value = ClickyWorkIndex.DateText(valueOrDefault);
					}
					else
					{
						value = "working";
					}
					defaultInterpolatedStringHandler.AppendFormatted((string?)value);
					defaultInterpolatedStringHandler.AppendLiteral(" · asked: ");
					defaultInterpolatedStringHandler.AppendFormatted(ClickyWorkIndex.OneLine(run.Ask, 400));
					defaultInterpolatedStringHandler.AppendLiteral(" · ");
					string text4 = defaultInterpolatedStringHandler.ToStringAndClear();
					string text5 = ClickyWorkIndex.ReportOutcomeText(run.Outcome);
					string summary = run.Summary;
					return text4 + text5 + ((summary != null) ? (": " + summary) : "");
				}));
				return new JsonObject((JsonNodeOptions?)null)
				{
					["job_id"] = job.Id,
					["title"] = title,
					["report"] = text6 + "\n\nLatest reply:\n" + (job.ReplyText ?? "(none)")
				};
			}
			if ((object)clicky == null)
			{
				return new JsonObject((JsonNodeOptions?)null)
				{
					["job_id"] = jobId,
					["title"] = title,
					["report"] = "(no report kept for this folder; read its conversation or its output/ files)"
				};
			}
		}
		object obj = job?.AllThreadIds;
		if (obj == null)
		{
			if ((object)clicky == null)
			{
				IReadOnlyList<string> readOnlyList = Array.Empty<string>();
				obj = readOnlyList;
			}
			else
			{
				IReadOnlyList<string> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<string>(jobId);
				obj = readOnlyList;
			}
		}
		IReadOnlyList<string> threadIds = (IReadOnlyList<string>)obj;
		string folderForRecords = ((folderPath == null) ? null : ClickyWorkIndex.StandardizedPath(folderPath));
		List<(string, IReadOnlyList<ClickyHarnessRecord.Turn>)> list3 = await Task.Run(delegate
		{
			List<string> list = threadIds.ToList();
			List<(string, IReadOnlyList<ClickyHarnessRecord.Turn>)> list2 = new List<(string, IReadOnlyList<ClickyHarnessRecord.Turn>)>();
			if (list.Count == 0 && folderForRecords != null)
			{
				foreach (var item in from r in ClickyHarnessRecord.AllRecords(HarnessRecordsRoot)
					where string.Equals(ClickyWorkIndex.StandardizedPath(r.Workspace), folderForRecords, StringComparison.OrdinalIgnoreCase)
					select r)
				{
					string text7 = ClickyHarnessRecord.RecordDirectory(HarnessRecordsRoot, item.Item1);
					if (text7 != null)
					{
						list2.Add((item.Item1, ClickyHarnessRecord.Turns(text7)));
					}
				}
				return list2;
			}
			foreach (string item2 in list)
			{
				string text8 = HarnessRecordThreadId(item2);
				if (text8 != null)
				{
					string text9 = ClickyHarnessRecord.RecordDirectory(HarnessRecordsRoot, text8);
					if (text9 != null)
					{
						list2.Add((item2, ClickyHarnessRecord.Turns(text9)));
					}
				}
			}
			return list2;
		}).ConfigureAwait(continueOnCapturedContext: false);
		HashSet<string> harnessThreadIds = list3.Select<(string, IReadOnlyList<ClickyHarnessRecord.Turn>), string>(((string ThreadId, IReadOnlyList<ClickyHarnessRecord.Turn> Turns) r) => r.ThreadId).ToHashSet();
		List<string> list4 = new List<string>();
		foreach (var item3 in list3)
		{
			foreach (ClickyHarnessRecord.Turn item4 in item3.Item2)
			{
				string text10 = "They asked: " + DisplayUserText(item4.RawAsk).Trim();
				if (item4.ToolLines.Count > 0)
				{
					text10 = text10 + "\nIt did:\n" + string.Join("\n", item4.ToolLines);
				}
				string finalText = item4.FinalText;
				string text11 = ((finalText != null) ? StructuredFinalReply.Parse(finalText).Text : "");
				string[] obj2 = new string[5] { text10, "\nIt replied", null, null, null };
				string status = item4.Status;
				obj2[2] = ((status != null) ? (" (" + status + ")") : "");
				obj2[3] = ":\n";
				obj2[4] = ((text11.Length == 0) ? "(no reply)" : text11);
				text10 = string.Concat(obj2);
				list4.Add(text10);
			}
		}
		list4.AddRange(CachedConversationExchanges(threadIds.Where((string t) => !harnessThreadIds.Contains(t)).ToList()));
		if (list4.Count == 0)
		{
			return new JsonObject((JsonNodeOptions?)null)
			{
				["job_id"] = job?.Id ?? jobId,
				["title"] = title,
				["conversation"] = "(nothing recorded)"
			};
		}
		return new JsonObject((JsonNodeOptions?)null)
		{
			["job_id"] = job?.Id ?? jobId,
			["title"] = title,
			["conversation"] = NewestExchanges(list4, 40000)
		};
	}

	private static string? HarnessRecordThreadId(string appThreadId)
	{
		if (!AgentHarnessPreferences.IsHarnessThread(appThreadId))
		{
			return AgentHarnessThreads.ExistingHarnessThreadId(appThreadId);
		}
		return appThreadId;
	}

	private IReadOnlyList<AgentTranscriptTurn>? HarnessRecordTranscript(string appThreadId)
	{
		string text = HarnessRecordThreadId(appThreadId);
		if (text != null)
		{
			string text2 = ClickyHarnessRecord.RecordDirectory(HarnessRecordsRoot, text);
			if (text2 != null)
			{
				List<AgentTranscriptTurn> list = (from t in ClickyHarnessRecord.Turns(text2).Select(delegate(ClickyHarnessRecord.Turn turn)
					{
						string text3 = DisplayUserText(turn.RawAsk);
						string text4 = clickys.DisplayFor(text3) ?? text3;
						string finalText = turn.FinalText;
						StructuredFinalReply structuredFinalReply = ((finalText != null) ? StructuredFinalReply.Parse(finalText) : null);
						IReadOnlyList<string> userMessages;
						IReadOnlyList<string> readOnlyList;
						if (text4.Trim().Length != 0)
						{
							readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<string>(text4);
							userMessages = readOnlyList;
						}
						else
						{
							readOnlyList = Array.Empty<string>();
							userMessages = readOnlyList;
						}
						List<string> toolLines = turn.ToolLines;
						IReadOnlyList<string> assistantMessages;
						if ((object)structuredFinalReply != null)
						{
							string text5 = structuredFinalReply.Text;
							if (text5 != null && text5.Length > 0)
							{
								readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<string>(structuredFinalReply.Text);
								assistantMessages = readOnlyList;
								goto IL_008e;
							}
						}
						readOnlyList = Array.Empty<string>();
						assistantMessages = readOnlyList;
						goto IL_008e;
						IL_008e:
						return new AgentTranscriptTurn(null, userMessages, toolLines, assistantMessages, structuredFinalReply?.NextActions ?? Array.Empty<string>(), structuredFinalReply?.Artifacts ?? Array.Empty<string>(), turn.Status ?? "completed", null, null, turn.EndedAt);
					})
					where t.UserMessages.Count > 0 || t.AssistantMessages.Count > 0
					select t).ToList();
				if (list.Count != 0)
				{
					return list;
				}
				return null;
			}
		}
		return null;
	}

	public string OneBrainJobId(string threadId)
	{
		return Jobs.JobForThread(threadId)?.Id ?? threadId;
	}

	public (bool IsJob, string? ThreadId) ThreadIdForOneBrainJobId(string jobId)
	{
		ClickyJob clickyJob = Jobs.Job(jobId);
		if ((object)clickyJob == null)
		{
			return (IsJob: false, ThreadId: null);
		}
		return (IsJob: true, ThreadId: clickyJob.ThreadId);
	}
}
