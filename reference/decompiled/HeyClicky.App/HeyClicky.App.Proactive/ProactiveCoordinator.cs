using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Routines;
using HeyClicky.App.Notch;
using HeyClicky.App.OneBrain;
using HeyClicky.App.Portraits;
using HeyClicky.App.Services;
using HeyClicky.App.Settings;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Dispatching;
using WinRT;
using WinRT.HeyClickyVtableClasses;

namespace HeyClicky.App.Proactive;

[WinRTRuntimeClassName("Windows.Foundation.IClosable")]
[WinRTExposedType(typeof(HeyClicky_App_Proactive_ProactiveCoordinatorWinRTTypeDetails))]
public sealed class ProactiveCoordinator : IDisposable
{
	public sealed record ApprovalResult(string ClickySlug, string ClickyName, ProactiveQueueStatus Status);

	private sealed record PendingConnection(string Slug, int Baseline, DateTimeOffset Started);

	private const string Area = "Proactive";

	private static readonly TimeSpan SlotsPollInterval = TimeSpan.FromSeconds(30L);

	private static readonly TimeSpan ActiveRecency = TimeSpan.FromSeconds(30L);

	private static readonly TimeSpan JobPollInterval = TimeSpan.FromSeconds(4L);

	private static readonly TimeSpan JobFastPollWindow = TimeSpan.FromMinutes(15L);

	private static readonly TimeSpan JobSlowPollInterval = TimeSpan.FromSeconds(30L);

	private static readonly TimeSpan JobReceiptLifetime = TimeSpan.FromHours(6);

	private static readonly TimeSpan StatusSyncInterval = TimeSpan.FromSeconds(60L);

	public const int DenyReasonMaximumCharacters = 500;

	private readonly AppHost host;

	private readonly RealStack real;

	private readonly DispatcherQueue dq;

	private readonly ProactiveStore store = new ProactiveStore();

	private readonly ProactiveAgentsClient client;

	private readonly AgentBackendClient backend;

	private readonly DispatcherQueueTimer slotsTimer;

	private readonly DispatcherQueueTimer expiryTimer;

	private readonly Dictionary<string, Guid> launchedTasks = new Dictionary<string, Guid>();

	private readonly Random random = new Random();

	private readonly TimeSpan clockOffset;

	private List<ProactiveSuggestionCard> cards = new List<ProactiveSuggestionCard>();

	private List<QueuedProactiveTask> queue = new List<QueuedProactiveTask>();

	private string? accountKey;

	private CancellationTokenSource? jobCts;

	private Guid? jobRunId;

	private DateTimeOffset? statusSyncLastRunAt;

	private bool statusSyncInFlight;

	private bool wasOnboardingCompleted;

	private bool started;

	private int adjustmentAnswerCount;

	private Guid? currentCardId;

	private const string OnboardingFirstAskIntegrationNote = "This ask was written during onboarding to show what you can do, so it may reach for an app (Gmail, Notion, Google Calendar, Sheets, Linear, GitHub) the user has not connected. Assume NOTHING is connected unless your own tool list says otherwise. Never stall on that: do the strongest app-free version now with web research and files you make (the people in a CSV, the emails drafted right here in the chat, the plan as a doc), and then, plainly and eagerly, tell the user which one app to connect to finish the job and exactly what you would do the moment they do (\"Connect Gmail and I'll send these as drafts to your inbox\"). One sentence, at the end, no nagging.";

	private const string OnboardingFirstJobRunsOnItsOwnNote = "This is one of their very first jobs, picked during onboarding so it needs nothing from them: finish it start to finish without asking anything (this overrides any ask-first guidance above). Where something is open, make the sensible choice from the brief and the web and say in one line what you assumed; never stop to ask and never end on a question.";

	private volatile string? liveNoteSnapshot;

	private static readonly TimeSpan PostOnboardingHeadsUpQuiet = TimeSpan.FromMinutes(5L);

	private static readonly TimeSpan IntegrationSuggestionAutoHide = TimeSpan.FromSeconds(18L);

	private static readonly TimeSpan CatalogRefreshInterval = TimeSpan.FromMinutes(30L);

	private readonly IntegrationSuggestionRules integrationRules = new IntegrationSuggestionRules();

	private IReadOnlyList<AgentIntegrationInfo> catalog = Array.Empty<AgentIntegrationInfo>();

	private DateTimeOffset catalogFetchedAt;

	private Task<IReadOnlyList<AgentIntegrationInfo>>? catalogLoad;

	private DispatcherQueueTimer? integrationTimer;

	private bool integrationSampleInFlight;

	private IntegrationCandidate? activeIntegrationSuggestion;

	private bool spotifyTipActive;

	private string? lastSuppression;

	private (string Slug, DateTimeOffset At)? pendingSuggestionConnect;

	private PendingConnection? pendingConnection;

	private static readonly TimeSpan PendingConnectionLifetime = TimeSpan.FromMinutes(5L);

	private readonly UpcomingMeetingsClient meetingsClient;

	private readonly HashSet<string> announcedMeetings = new HashSet<string>();

	private DispatcherQueueTimer? meetingsTimer;

	private IReadOnlyList<UpcomingCalendarEvent> upcomingEvents = Array.Empty<UpcomingCalendarEvent>();

	private UpcomingCalendarEvent? activeMeeting;

	private DateTimeOffset? meetingsRefreshedAt;

	private DateTimeOffset? meetingsFailedAt;

	private bool meetingsBackendConnected;

	private bool meetingsRefreshInFlight;

	private int meetingsAuthGeneration;

	private static readonly TimeSpan InvitationRecheck = TimeSpan.FromSeconds(30L);

	private MorningSuggestionsNotice? morningNotice;

	private Guid? claimedNoticeId;

	private Guid? chimedNoticeId;

	private DateTimeOffset? morningSweepStartedAt;

	private DispatcherQueueTimer? readinessTimer;

	private bool claimInFlight;

	private readonly HashSet<string> heldBackReasons = new HashSet<string>();

	public const bool MorningSuggestionsInvitationHasASurface = false;

	public const bool IsMorningInvitationPaused = true;

	public static readonly bool MorningInvitationHasASurface = false;

	private DispatcherQueueTimer? greetingTimer;

	private DispatcherQueueTimer? greetingArmTimer;

	private bool greetingFiring;

	private string? armedGreetingDay;

	private DispatcherQueueTimer? notificationsTimer;

	private bool notificationsPollInFlight;

	private IReadOnlyList<AgentNotification> notifications = Array.Empty<AgentNotification>();

	private TapVoiceCapture? remoteTaskVoice;

	public static ProactiveCoordinator? Current { get; private set; }

	public bool IsFetching { get; private set; }

	public string? FetchError { get; private set; }

	public DateTimeOffset? LastFetchedAt { get; private set; }

	public Guid? AdjustingCardId { get; private set; }

	public Guid? CurrentCardId
	{
		get
		{
			return currentCardId;
		}
		set
		{
			currentCardId = value;
			if (IsSuggestionsPageVisible)
			{
				UpdateLiveStateHook();
			}
		}
	}

	public bool IsSuggestionsPageVisible { get; set; }

	public DispatcherQueue Dispatcher => dq;

	public SettingsStore CoreSettings => real.Core.Settings;

	public DateTimeOffset Now => DateTimeOffset.Now + clockOffset;

	private bool IsSignedIn
	{
		get
		{
			if (real.Core.Auth.IsSignedIn)
			{
				return real.Core.Auth.UserId != null;
			}
			return false;
		}
	}

	public bool IsEnabled => ProactiveLanePause.IsLaneEnabled(real.Core.Settings.Read((AppSettings s) => s.ProactiveAgentSuggestionsEnabled));

	private bool HasCompletedOnboarding => real.Core.Settings.Read((AppSettings s) => s.Onboarding.HasCompletedOnboarding);

	private bool CanAdvancePastPermissions
	{
		get
		{
			if (host.Services.Permissions.GetStatus(PermissionKind.Microphone) == PermissionStatus.Granted)
			{
				return host.Services.Permissions.GetStatus(PermissionKind.ScreenCapture) == PermissionStatus.Granted;
			}
			return false;
		}
	}

	private bool IsTutorialEngaged => real.Tutorial.IsEngaged;

	private bool IsWithinPostOnboardingQuietPeriod
	{
		get
		{
			DateTimeOffset? dateTimeOffset = store.Read((ProactiveStoreData d) => d.OnboardingFirstCompletedAt);
			if (dateTimeOffset.HasValue)
			{
				return ProactiveCadenceRules.IsBeforeFirstMorning(dateTimeOffset.GetValueOrDefault().LocalDateTime, Now.LocalDateTime);
			}
			return false;
		}
	}

	private ProactiveCadence Cadence => ProactiveCadence.Daily;

	public IReadOnlyList<ProactiveSuggestionCard> VisibleCards
	{
		get
		{
			DateTimeOffset now = Now;
			return ClickyRoster.Shared.StarterCards.Select(FromStarter).Concat(cards.Where((ProactiveSuggestionCard c) => !c.IsExpired(now))).ToList();
		}
	}

	public IReadOnlyList<QueuedProactiveTask> Queue => queue;

	public bool HasUnseenCards
	{
		get
		{
			DateTimeOffset dateTimeOffset = VisibleCards.Select((ProactiveSuggestionCard c) => c.CreatedAt).DefaultIfEmpty().Max();
			if (dateTimeOffset == default(DateTimeOffset))
			{
				return false;
			}
			string text = accountKey;
			DateTimeOffset? dateTimeOffset2 = ((text != null) ? store.ReadAccount(text, (ProactiveAccountState a) => a.SuggestionsRowLastSeenAt) : ((DateTimeOffset?)null));
			if (dateTimeOffset2.HasValue)
			{
				DateTimeOffset value = dateTimeOffset;
				DateTimeOffset? dateTimeOffset3 = dateTimeOffset2;
				return value > dateTimeOffset3;
			}
			return true;
		}
	}

	public IReadOnlyList<AgentIntegrationInfo> Integrations => catalog;

	public bool IsSweepRunning => jobCts != null;

	public string? AccountKey => accountKey;

	public string? UserId => real.Core.Auth.UserId;

	private bool IsWithinPostOnboardingHeadsUpQuietPeriod
	{
		get
		{
			DateTimeOffset? dateTimeOffset = store.Read((ProactiveStoreData d) => d.OnboardingFirstCompletedAt);
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				if (Now - valueOrDefault < PostOnboardingHeadsUpQuiet)
				{
					return true;
				}
			}
			double? num = real.Core.Settings.Read((AppSettings s) => s.Onboarding.TutorialCompletedAtEpochSeconds);
			if (num.HasValue)
			{
				double valueOrDefault2 = num.GetValueOrDefault();
				return (double)DateTimeOffset.Now.ToUnixTimeSeconds() - valueOrDefault2 < PostOnboardingHeadsUpQuiet.TotalSeconds;
			}
			return false;
		}
	}

	public bool IsUpcomingMeetingsEnabled => real.Core.Settings.Read((AppSettings s) => s.UpcomingMeetingsEnabled);

	public IReadOnlyList<UpcomingCalendarEvent> UpcomingEvents => upcomingEvents;

	public UpcomingCalendarEvent? ActiveMeeting => activeMeeting;

	public MorningSuggestionsNotice? MorningNotice => morningNotice;

	private bool MorningPaperCardWouldShow => GoodMorningBrief.HasTodaysResearch(real.Core.Settings, real.Core.Auth.UserId);

	private bool IsMorningLaneResting => MorningSuggestionsInvitationPolicy.IsResting(store.Read((ProactiveStoreData d) => d.MorningDismissals), Now);

	private DateTimeOffset? GreetingPresentedAtToday
	{
		get
		{
			DateTimeOffset? dateTimeOffset = store.Read((ProactiveStoreData d) => d.MorningGreetingLastPresentedAt);
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				if (valueOrDefault.LocalDateTime.Date == Now.LocalDateTime.Date)
				{
					return valueOrDefault;
				}
			}
			return null;
		}
	}

	private bool IsMorningShowing
	{
		get
		{
			if (host.Services.Presenter.Presentation is NotchPresentation.LiveEvent liveEvent)
			{
				return liveEvent.Event is NotchLiveEvent.MorningSuggestions;
			}
			return false;
		}
	}

	private bool HasGreetedToday => store.Read((ProactiveStoreData d) => d.MorningGreetingLastDay) == MorningGreetingPolicy.DayKey(Now.LocalDateTime);

	public IReadOnlyList<AgentNotification> Notifications => notifications;

	public bool IsRemoteTasksEnabled => real.Core.Settings.Read((AppSettings s) => s.RemoteTasksEnabled);

	public string? PendingRemoteTaskContext { get; private set; }

	public bool IsRemoteTaskVoiceActive => remoteTaskVoice?.IsActive ?? false;

	public event Action? Changed;

	public event Action? NotificationsChanged;

	public ProactiveCoordinator(AppHost host, RealStack real)
	{
		ProactiveCoordinator proactiveCoordinator = this;
		this.host = host;
		this.real = real;
		dq = host.DispatcherQueue;
		client = new ProactiveAgentsClient(real.Core.Worker);
		backend = new AgentBackendClient(real.Core.Http, async (bool force, CancellationToken ct) => await real.Agents.GetAgentSessionTokenAsync(force, ct).ConfigureAwait(continueOnCapturedContext: false));
		meetingsClient = new UpcomingMeetingsClient(real.Core.Worker);
		clockOffset = ReadTestClockOffset();
		slotsTimer = dq.CreateTimer();
		slotsTimer.Interval = SlotsPollInterval;
		slotsTimer.Tick += delegate
		{
			proactiveCoordinator.EvaluateSlots();
		};
		expiryTimer = dq.CreateTimer();
		expiryTimer.IsRepeating = false;
		expiryTimer.Tick += delegate
		{
			proactiveCoordinator.ExpireOutdatedCards();
			proactiveCoordinator.ScheduleExpiry();
		};
		Current = this;
	}

	private static double IdleSeconds()
	{
		return QuietContext.SecondsSinceLastInput();
	}

	private static TimeSpan ReadTestClockOffset()
	{
		return TimeSpan.Zero;
	}

	public void Start()
	{
		if (started)
		{
			return;
		}
		started = true;
		wasOnboardingCompleted = real.Core.Settings.Read((AppSettings s) => s.Onboarding.HasCompletedOnboarding);
		real.Core.Auth.Changed += delegate
		{
			dq.TryEnqueue(OnAuthOrSettingsChanged);
		};
		real.Core.Settings.Changed += delegate
		{
			dq.TryEnqueue(OnAuthOrSettingsChanged);
		};
		real.Agents.TaskEvent += delegate(object? _, AgentTaskEvent e)
		{
			dq.TryEnqueue(delegate
			{
				OnAgentTaskEvent(e);
			});
		};
		real.Agents.TasksChanged += delegate
		{
			dq.TryEnqueue(DrainQueue);
		};
		real.Companion.TurnCompleted += delegate
		{
			dq.TryEnqueue(NoteAdjustmentTurnCompleted);
		};
		ClickyRoster.Shared.Changed += delegate
		{
			dq.TryEnqueue(RaiseChanged);
		};
		host.NotchUi.Window.RawMessage += delegate(uint msg, nint w, nint _)
		{
			if (msg == 536 && (w == 18 || w == 7))
			{
				dq.TryEnqueue(OnResumedFromSleep);
			}
		};
		OnAuthOrSettingsChanged();
		Log.Info("Proactive", "started" + ((clockOffset == TimeSpan.Zero) ? "" : $" (test clock {Now.LocalDateTime:yyyy-MM-dd HH:mm})"));
	}

	private void OnResumedFromSleep()
	{
		EvaluateSlots();
		ScheduleNextGreetingCheck(evaluatingNow: true);
	}

	private void OnAuthOrSettingsChanged()
	{
		bool hasCompletedOnboarding = HasCompletedOnboarding;
		if (hasCompletedOnboarding && !wasOnboardingCompleted && !store.Read((ProactiveStoreData d) => d.OnboardingFirstCompletedAt).HasValue)
		{
			store.Update(delegate(ProactiveStoreData d)
			{
				d.OnboardingFirstCompletedAt = Now;
			});
		}
		wasOnboardingCompleted = hasCompletedOnboarding;
		string text = (IsSignedIn ? (real.Core.Endpoints.WorkerBaseUrl.AbsoluteUri.TrimEnd('/') + "|" + real.Core.Auth.UserId) : null);
		bool flag = false;
		if (text != accountKey)
		{
			flag = text != null;
			CancelJob();
			accountKey = text;
			cards = ((text == null) ? new List<ProactiveSuggestionCard>() : store.ReadAccount(text, (ProactiveAccountState a) => a.Cards.ToList()));
			queue = ((text == null) ? new List<QueuedProactiveTask>() : store.ReadAccount(text, (ProactiveAccountState a) => a.Queue.Select((QueuedProactiveTask e) => e with
			{
				IsPaused = true,
				IsLaunching = false
			}).ToList()));
			LastFetchedAt = ((text == null) ? ((DateTimeOffset?)null) : store.ReadAccount(text, (ProactiveAccountState a) => a.SuggestionsLastFetchedAt));
			DismissMorningNotice();
			ResetMeetingsForAuthChange();
			StopNotifications(clear: true);
			if (text != null)
			{
				PersistQueue();
			}
		}
		if (text == null || !IsEnabled)
		{
			StopSuggestionLane(text == null);
			if (text != null)
			{
				ExpireOutdatedCards();
			}
		}
		else
		{
			StartSuggestionLane();
		}
		if (flag)
		{
			real.Agents.Jobs.RelinkWaitingJobsToRestoredCards(cards.Select((ProactiveSuggestionCard c) => (Id: c.Id, Description: c.Description)).ToList());
		}
		UpdateGreetingWatcher();
		UpdateMeetingsScheduler();
		UpdateNotificationPolling();
		UpdateIntegrationMonitor();
		RaiseChanged();
	}

	private void StartSuggestionLane()
	{
		ExpireOutdatedCards();
		ScheduleExpiry();
		StartJob(ProactiveCheckReason.HomeSpace, resumeOnly: true);
		RebuildMorningInvitationIfNeeded("launch");
		if (!slotsTimer.IsRunning)
		{
			slotsTimer.Start();
		}
		RefreshIntegrationsAsync();
		EvaluateSlots();
	}

	private void StopSuggestionLane(bool clearCards)
	{
		slotsTimer.Stop();
		CancelJob();
		DismissMorningNotice();
		if (clearCards)
		{
			cards = new List<ProactiveSuggestionCard>();
			queue = new List<QueuedProactiveTask>();
			expiryTimer.Stop();
		}
	}

	public void Dispose()
	{
		slotsTimer.Stop();
		expiryTimer.Stop();
		CancelJob();
		DisposeMeetings();
		StopNotifications(clear: false);
		StopIntegrationMonitor();
		greetingTimer?.Stop();
		if (Current == this)
		{
			Current = null;
		}
	}

	private void RaiseChanged()
	{
		if (AdjustingCardId.HasValue || IsSuggestionsPageVisible)
		{
			UpdateLiveStateHook();
		}
		try
		{
			this.Changed?.Invoke();
		}
		catch (Exception ex)
		{
			Log.Error("Proactive", "Changed handler threw", ex);
		}
	}

	private void EvaluateSlots()
	{
		if (accountKey != null && IsEnabled)
		{
			ExpireOutdatedCards();
			SyncStatusesFromServer();
			RebuildMorningInvitationIfNeeded("rebuilt from today's cards");
			if (!(IdleSeconds() > ActiveRecency.TotalSeconds))
			{
				EnsureMorningSweepStarted();
			}
		}
	}

	private void EnsureMorningSweepStarted()
	{
		ExpireOutdatedCards();
		string text = accountKey;
		if (text == null || !IsEnabled || !HasCompletedOnboarding || !CanAdvancePastPermissions || IsTutorialEngaged || IsWithinPostOnboardingQuietPeriod || IsMorningLaneResting)
		{
			return;
		}
		(ProactiveSlotDayState, DateTimeOffset?) tuple = store.ReadAccount(text, (ProactiveAccountState a) => (Slots: a.Slots, IgnoredSuggestionsBreakUntil: a.IgnoredSuggestionsBreakUntil));
		DateTimeOffset? item = tuple.Item2;
		if (item.HasValue)
		{
			DateTimeOffset valueOrDefault = item.GetValueOrDefault();
			if (Now < valueOrDefault)
			{
				return;
			}
		}
		if (Cadence.IsDue(Now.LocalDateTime, tuple.Item1.MorningFiredAt?.LocalDateTime, tuple.Item1.RetryAfter?.LocalDateTime))
		{
			StartJob(ProactiveCheckReason.Morning);
		}
	}

	private void RecordAttempt()
	{
		UpdateSlots((ProactiveSlotDayState s) => s with
		{
			RetryAfter = Now + TimeSpan.FromMinutes(5L)
		});
	}

	private void RecordFailure()
	{
		UpdateSlots(delegate(ProactiveSlotDayState s)
		{
			int num = s.ConsecutiveFailedAttempts.GetValueOrDefault() + 1;
			return s with
			{
				ConsecutiveFailedAttempts = num,
				RetryAfter = Now + ProactiveSlotDayState.RetryDelay(num)
			};
		});
	}

	private void RecordSuccess()
	{
		UpdateSlots((ProactiveSlotDayState _) => new ProactiveSlotDayState(Now, null, null));
	}

	private void UpdateSlots(Func<ProactiveSlotDayState, ProactiveSlotDayState> change)
	{
		string text = accountKey;
		if (text != null)
		{
			store.UpdateAccount(text, delegate(ProactiveAccountState a)
			{
				a.Slots = change(a.Slots);
			});
		}
	}

	public bool StartJob(ProactiveCheckReason reason, bool resumeOnly = false)
	{
		if (jobCts == null)
		{
			string key = accountKey;
			if (key != null && IsEnabled && HasCompletedOnboarding && CanAdvancePastPermissions && !IsTutorialEngaged)
			{
				PendingSuggestionJob pending = store.ReadAccount(key, (ProactiveAccountState a) => a.PendingJob);
				if ((object)pending != null && DateTimeOffset.Now - pending.StartedAt >= JobReceiptLifetime)
				{
					store.UpdateAccount(key, delegate(ProactiveAccountState a)
					{
						a.PendingJob = null;
					});
					pending = null;
				}
				if (resumeOnly && (object)pending == null)
				{
					return false;
				}
				ProactiveCheckReason origin = pending?.CheckReason ?? reason;
				if (origin == ProactiveCheckReason.Morning && IsWithinPostOnboardingQuietPeriod)
				{
					return false;
				}
				if (origin == ProactiveCheckReason.Morning)
				{
					morningSweepStartedAt = pending?.StartedAt ?? Now;
				}
				Guid runId = Guid.NewGuid();
				jobRunId = runId;
				CancellationTokenSource cts = (jobCts = new CancellationTokenSource());
				IsFetching = true;
				FetchError = null;
				RecordAttempt();
				RaiseChanged();
				Log.Info("Proactive", ((object)pending == null) ? ("suggestion sweep starting (" + origin.Wire() + ")") : $"resuming suggestion job {pending.JobId} ({origin.Wire()})");
				Task.Run(async delegate
				{
					IReadOnlyList<ProactiveSuggestionCard> result = null;
					string error = null;
					try
					{
						IReadOnlyList<ProactiveSuggestionCard> readOnlyList = (((object)pending != null) ? (await PollJobAsync(pending, key, cts.Token)) : (await RunJobAsync(origin, key, cts.Token)));
						result = readOnlyList;
					}
					catch (OperationCanceledException)
					{
					}
					catch (Exception ex2)
					{
						error = ex2.Message;
						Log.Warn("Proactive", "suggestion sweep failed: " + ex2.Message);
					}
					dq.TryEnqueue(delegate
					{
						FinishJob(runId, key, origin, result, error, cts.IsCancellationRequested);
					});
				});
				return true;
			}
		}
		return false;
	}

	private void FinishJob(Guid runId, string key, ProactiveCheckReason origin, IReadOnlyList<ProactiveSuggestionCard>? result, string? error, bool cancelled)
	{
		if (jobRunId != runId)
		{
			return;
		}
		jobRunId = null;
		jobCts = null;
		IsFetching = false;
		if (cancelled || accountKey != key)
		{
			RaiseChanged();
			return;
		}
		if (result == null)
		{
			RecordFailure();
			FetchError = error ?? "Could not load suggestions. We’ll try again in a little while.";
			RaiseChanged();
			return;
		}
		LastFetchedAt = Now;
		store.UpdateAccount(key, delegate(ProactiveAccountState a)
		{
			a.SuggestionsLastFetchedAt = LastFetchedAt;
		});
		RecordSuccess();
		if (result.Count > 0)
		{
			PresentCards(result, origin == ProactiveCheckReason.Morning);
		}
		else
		{
			Log.Info("Proactive", "suggestion sweep chose silence (" + origin.Wire() + ")");
		}
		RaiseChanged();
	}

	private async Task<IReadOnlyList<ProactiveSuggestionCard>?> RunJobAsync(ProactiveCheckReason reason, string key, CancellationToken ct)
	{
		ProactiveRequestInputs inputs = await BuildRequestInputsAsync(reason, ct).ConfigureAwait(continueOnCapturedContext: false);
		ProactiveJobStatus proactiveJobStatus = await client.StartJobAsync(inputs, Now, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (proactiveJobStatus.IsSettled)
		{
			return SettledCards(proactiveJobStatus, key);
		}
		PendingSuggestionJob job = new PendingSuggestionJob(proactiveJobStatus.JobId, DateTimeOffset.Now, reason);
		store.UpdateAccount(key, delegate(ProactiveAccountState a)
		{
			a.PendingJob = job;
		});
		Log.Info("Proactive", $"suggestion job {job.JobId} started ({reason.Wire()})");
		return await PollJobAsync(job, key, ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<IReadOnlyList<ProactiveSuggestionCard>?> PollJobAsync(PendingSuggestionJob job, string key, CancellationToken ct)
	{
		int failures = 0;
		while (DateTimeOffset.Now - job.StartedAt < JobReceiptLifetime)
		{
			await Task.Delay((DateTimeOffset.Now - job.StartedAt >= JobFastPollWindow) ? JobSlowPollInterval : JobPollInterval, ct).ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				ProactiveJobStatus proactiveJobStatus = await client.PollJobAsync(job.JobId, job.CheckReason, Now, ct).ConfigureAwait(continueOnCapturedContext: false);
				failures = 0;
				if (!proactiveJobStatus.IsSettled)
				{
					continue;
				}
				store.UpdateAccount(key, delegate(ProactiveAccountState a)
				{
					a.PendingJob = null;
				});
				Log.Info("Proactive", $"suggestion job {job.JobId} settled: {proactiveJobStatus.State}");
				return SettledCards(proactiveJobStatus, key);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex2)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 2);
				defaultInterpolatedStringHandler.AppendLiteral("suggestion job poll failed (");
				int num = failures + 1;
				failures = num;
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral("): ");
				defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
				Log.Info("Proactive", defaultInterpolatedStringHandler.ToStringAndClear());
				if (failures >= 8)
				{
					return null;
				}
			}
		}
		store.UpdateAccount(key, delegate(ProactiveAccountState a)
		{
			a.PendingJob = null;
		});
		Log.Info("Proactive", "suggestion job timed out client-side");
		return null;
	}

	private IReadOnlyList<ProactiveSuggestionCard>? SettledCards(ProactiveJobStatus status, string key)
	{
		DateTimeOffset? breakUntil = status.BreakUntil;
		if (breakUntil.HasValue)
		{
			DateTimeOffset until = breakUntil.GetValueOrDefault();
			Log.Info("Proactive", $"morning suggestions on an ignored-suggestions break until {until:u}");
			store.UpdateAccount(key, delegate(ProactiveAccountState a)
			{
				a.IgnoredSuggestionsBreakUntil = until;
			});
		}
		if (status.State == ProactiveJobState.Failed)
		{
			Log.Info("Proactive", "suggestion job " + status.JobId + " failed: " + (status.Error ?? "unknown"));
			return null;
		}
		IReadOnlyList<ProactiveSuggestionCard> result = status.Cards ?? Array.Empty<ProactiveSuggestionCard>();
		string userId = real.Core.Auth.UserId;
		if (userId != null)
		{
			GoodMorningBrief.Shared?.NoteSettledSweep(status, userId, result);
		}
		return result;
	}

	private void CancelJob()
	{
		jobCts?.Cancel();
		jobCts = null;
		jobRunId = null;
		IsFetching = false;
		FetchError = null;
	}

	internal async Task<ProactiveRequestInputs> BuildRequestInputsAsync(ProactiveCheckReason reason, CancellationToken ct)
	{
		IReadOnlyList<AgentIntegrationInfo> source = ((catalog.Count <= 0) ? (await RefreshIntegrationsAsync().ConfigureAwait(continueOnCapturedContext: false)) : catalog);
		List<string> list = (from i in source
			where i.IsActiveConnection
			select i.DisplayName).ToList();
		IReadOnlyList<ClickyRoutine> routines = real.Agents.Routines.Routines;
		if (ObsidianVault.ConfiguredPath != null)
		{
			list.Add("Obsidian");
		}
		List<(string, string)> customConnectors = (from c in CustomConnectorStore.Load()
			orderby c.CreatedAt
			select (Name: c.Name, EffectiveCapabilityDescription: c.EffectiveCapabilityDescription)).ToList();
		ProactiveRequestInputs proactiveRequestInputs = new ProactiveRequestInputs(list, store.Read((ProactiveStoreData d) => d.History.ToList()), reason, Array.Empty<ProactiveClickyDescriptor>(), customConnectors);
		ProactiveRequestInputs proactiveRequestInputs2 = proactiveRequestInputs;
		bool flag = (uint)reason <= 1u;
		proactiveRequestInputs2.ReadsForMorningPaper = flag && GoodMorningBrief.IsMorningPaperEnabled(real.Core.Settings);
		proactiveRequestInputs.ExistingRoutines = routines.Select((ClickyRoutine r) => (Title: r.Title, IntervalMinutes: r.IntervalMinutes)).ToList();
		return proactiveRequestInputs;
	}

	private static Guid StarterId(StarterTaskCard s)
	{
		return new Guid(MD5.HashData(Encoding.UTF8.GetBytes("starter|" + s.Title + "|" + s.AgentPrompt)));
	}

	private static ProactiveSuggestionCard FromStarter(StarterTaskCard s)
	{
		ProactiveSuggestionCard obj = new ProactiveSuggestionCard
		{
			Id = StarterId(s),
			Title = s.Title,
			Description = s.Description,
			AgentPrompt = s.AgentPrompt,
			CheckReason = ProactiveCheckReason.Onboarding,
			ServerSuggestionId = s.SuggestionId,
			ClickySlug = s.ClickySlug
		};
		int? routineEveryMinutes = s.RoutineEveryMinutes;
		obj.Routine = ((routineEveryMinutes.HasValue && routineEveryMinutes.GetValueOrDefault() > 0) ? new ProactiveRoutine(s.RoutineEveryMinutes.Value, s.RoutineTitle ?? s.Title) : null);
		return obj;
	}

	private ProactiveSuggestionCard? FindCard(Guid id)
	{
		return cards.FirstOrDefault((ProactiveSuggestionCard c) => c.Id == id) ?? ClickyRoster.Shared.StarterCards.Where((StarterTaskCard s) => StarterId(s) == id).Select(FromStarter).FirstOrDefault();
	}

	private void RemoveCard(Guid id)
	{
		StarterTaskCard starterTaskCard = ClickyRoster.Shared.StarterCards.FirstOrDefault((StarterTaskCard s) => StarterId(s) == id);
		if ((object)starterTaskCard != null)
		{
			ClickyRoster.Shared.RemoveStarterCard(starterTaskCard);
		}
		cards.RemoveAll((ProactiveSuggestionCard c) => c.Id == id);
		PersistCards();
		ReconcileMorningNotice();
	}

	private void PersistCards()
	{
		string text = accountKey;
		if (text != null)
		{
			List<ProactiveSuggestionCard> snapshot = cards.ToList();
			store.UpdateAccount(text, delegate(ProactiveAccountState a)
			{
				a.Cards = snapshot;
			});
		}
	}

	private void PersistQueue()
	{
		string text = accountKey;
		if (text != null)
		{
			List<QueuedProactiveTask> snapshot = queue.ToList();
			store.UpdateAccount(text, delegate(ProactiveAccountState a)
			{
				a.Queue = snapshot;
			});
		}
	}

	private void PresentCards(IReadOnlyList<ProactiveSuggestionCard> fresh, bool announce)
	{
		Dictionary<int, ProactiveSuggestionCard> kept = cards.Where(delegate(ProactiveSuggestionCard c)
		{
			int? serverSuggestionId = c.ServerSuggestionId;
			if (serverSuggestionId.HasValue)
			{
				int id = serverSuggestionId.GetValueOrDefault();
				return fresh.Any((ProactiveSuggestionCard f) => f.ServerSuggestionId == id);
			}
			return false;
		}).ToDictionary((ProactiveSuggestionCard c) => c.ServerSuggestionId.Value);
		fresh = fresh.Select(delegate(ProactiveSuggestionCard f)
		{
			int? serverSuggestionId2 = f.ServerSuggestionId;
			if (serverSuggestionId2.HasValue)
			{
				int valueOrDefault = serverSuggestionId2.GetValueOrDefault();
				if (kept.TryGetValue(valueOrDefault, out var value))
				{
					return value;
				}
			}
			return f;
		}).ToList();
		List<ProactiveSuggestionCard> stale = cards.Where((ProactiveSuggestionCard c) => !fresh.Any((ProactiveSuggestionCard f) => f.Id == c.Id)).ToList();
		DateTimeOffset now = Now;
		if (stale.Count > 0)
		{
			store.Update(delegate(ProactiveStoreData d)
			{
				foreach (ProactiveSuggestionCard item in stale)
				{
					d.History = ProactiveHistory.RecordUndecided(d.History, item, ProactiveResolution.Replaced, now);
				}
			});
			Report(stale, "replaced");
			ClickyAnalytics.TrackProactiveAgentsUnactioned(stale.Select((ProactiveSuggestionCard c) => c.Title).ToList(), "replaced", stale[0].CheckReason.Wire());
		}
		Log.Info("Proactive", $"presenting {fresh.Count} suggestion card(s) ({fresh[0].CheckReason.Wire()})");
		cards = fresh.ToList();
		PersistCards();
		if (announce && fresh.Any((ProactiveSuggestionCard c) => c.CheckReason == ProactiveCheckReason.Morning && !kept.ContainsKey(c.ServerSuggestionId ?? (-1))))
		{
			QueueMorningNotice();
		}
		ScheduleExpiry();
		List<ProactiveSuggestionCard> list = fresh.Where((ProactiveSuggestionCard f) => !kept.ContainsKey(f.ServerSuggestionId ?? (-1))).ToList();
		Report(list, "presented");
		if (list.Count > 0)
		{
			ClickyAnalytics.TrackProactiveAgentsRecommended(list.Select((ProactiveSuggestionCard c) => c.Title).ToList(), list[0].CheckReason.Wire());
		}
		RaiseChanged();
	}

	public bool AppendConversationOffer(ProactiveSuggestionCard card)
	{
		if (cards.Any((ProactiveSuggestionCard c) => string.Equals(c.Title, card.Title, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		cards.Insert(0, card with
		{
			CheckReason = ProactiveCheckReason.Conversation,
			CreatedAt = Now
		});
		PersistCards();
		Report(new global::_003C_003Ez__ReadOnlySingleElementList<ProactiveSuggestionCard>(card), "presented");
		ClickyAnalytics.TrackProactiveAgentsRecommended(new global::_003C_003Ez__ReadOnlySingleElementList<string>(card.Title), ProactiveCheckReason.Conversation.Wire());
		RaiseChanged();
		return true;
	}

	private void Report(IReadOnlyList<ProactiveSuggestionCard> list, string status, string? denyReason = null)
	{
		List<int> list2 = (from c in list
			select c.ServerSuggestionId into i
			where i.HasValue
			select i).Cast<int>().ToList();
		if (list2.Count > 0)
		{
			client.ReportResolutionAsync(list2, status, denyReason);
		}
	}

	public void MarkViewed(Guid id)
	{
		int num = cards.FindIndex((ProactiveSuggestionCard c) => c.Id == id);
		if (num >= 0 && !cards[num].ViewedAt.HasValue)
		{
			cards[num] = cards[num]with
			{
				ViewedAt = Now
			};
			PersistCards();
			Report(new global::_003C_003Ez__ReadOnlySingleElementList<ProactiveSuggestionCard>(cards[num]), "viewed");
		}
	}

	private void ScheduleExpiry()
	{
		expiryTimer.Stop();
		DateTimeOffset dateTimeOffset = (from c in cards
			select c.ExpiresAt into e
			where e.HasValue
			select e.Value).DefaultIfEmpty().Min();
		if (!(dateTimeOffset == default(DateTimeOffset)))
		{
			TimeSpan timeSpan = dateTimeOffset - Now;
			expiryTimer.Interval = ((timeSpan < TimeSpan.FromSeconds(1L)) ? TimeSpan.FromSeconds(1L) : ((timeSpan > TimeSpan.FromHours(6)) ? TimeSpan.FromHours(6) : timeSpan));
			expiryTimer.Start();
		}
	}

	public void ExpireOutdatedCards()
	{
		DateTimeOffset now = Now;
		List<ProactiveSuggestionCard> expired = cards.Where((ProactiveSuggestionCard c) => c.IsExpired(now)).ToList();
		if (expired.Count == 0)
		{
			return;
		}
		store.Update(delegate(ProactiveStoreData d)
		{
			foreach (ProactiveSuggestionCard item in expired)
			{
				d.History = ProactiveHistory.RecordUndecided(d.History, item, ProactiveResolution.Expired, now);
			}
		});
		Report(expired, "expired");
		ClickyAnalytics.TrackProactiveAgentsUnactioned(expired.Select((ProactiveSuggestionCard c) => c.Title).ToList(), "expired", expired[0].CheckReason.Wire());
		HashSet<Guid> ids = expired.Select((ProactiveSuggestionCard c) => c.Id).ToHashSet();
		cards.RemoveAll((ProactiveSuggestionCard c) => ids.Contains(c.Id));
		PersistCards();
		ReconcileMorningNotice();
		Log.Info("Proactive", $"{expired.Count} suggestion card(s) expired");
		RaiseChanged();
	}

	public void SyncStatusesFromServer(bool force = false)
	{
		if (statusSyncInFlight)
		{
			return;
		}
		if (!force)
		{
			DateTimeOffset? dateTimeOffset = statusSyncLastRunAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				if (DateTimeOffset.Now - valueOrDefault < StatusSyncInterval)
				{
					return;
				}
			}
		}
		List<int> ids = (from c in cards
			select c.ServerSuggestionId into i
			where i.HasValue
			select i).Cast<int>().ToList();
		if (ids.Count == 0)
		{
			return;
		}
		statusSyncLastRunAt = DateTimeOffset.Now;
		statusSyncInFlight = true;
		Task.Run(async delegate
		{
			IReadOnlyDictionary<int, string> statuses = await client.FetchStatusesAsync(ids).ConfigureAwait(continueOnCapturedContext: false);
			dq.TryEnqueue(delegate
			{
				statusSyncInFlight = false;
				if (statuses != null)
				{
					HashSet<int> decided = (from s in statuses.Where<KeyValuePair<int, string>>(delegate(KeyValuePair<int, string> s)
						{
							string value = s.Value;
							return (value == "approved" || value == "denied") ? true : false;
						})
						select s.Key).ToHashSet();
					int num = cards.RemoveAll(delegate(ProactiveSuggestionCard c)
					{
						int? serverSuggestionId = c.ServerSuggestionId;
						if (serverSuggestionId.HasValue)
						{
							int valueOrDefault2 = serverSuggestionId.GetValueOrDefault();
							return decided.Contains(valueOrDefault2);
						}
						return false;
					});
					if (num != 0)
					{
						Log.Info("Proactive", $"dropping {num} suggestion card(s) decided on another device");
						PersistCards();
						ReconcileMorningNotice();
						RaiseChanged();
					}
				}
			});
		});
	}

	public static ClickyAnalytics.SuggestionCardInfo AnalyticsCard(ProactiveSuggestionCard card)
	{
		return new ClickyAnalytics.SuggestionCardInfo(card.ServerSuggestionId?.ToString() ?? card.Id.ToString().ToUpperInvariant(), card.CheckReason.Wire(), (object)card.NewClicky != null, (object)card.Routine != null, card.ClickySlug);
	}

	public JsonObject AnalyticsContext(string inputMethod, string? inputSurface = null)
	{
		string inputSurface2 = (string)(inputSurface ?? (inputMethod switch
		{
			"voice" => "global_voice_input", 
			"text" => "notch_text_box", 
			"scheduled" => "routine", 
			_ => "hud_button", 
		}));
		NotchPresentation presentation = host.Services.Presenter.Presentation;
		bool flag = ((presentation is NotchPresentation.OnboardingPinned || presentation is NotchPresentation.TextInput || presentation is NotchPresentation.TextResponse) ? true : false);
		bool notchWasExpanded = flag || (host.Services.Presenter.Presentation is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.AgentPeek);
		return ClickyAnalytics.InteractionContext(inputMethod, inputSurface2, "closed", homeWasOpen: false, homeWasVisible: false, notchWasExpanded);
	}

	public void TrackSuggestionAction(Guid cardId, string action, JsonObject context, bool hasFeedback = false)
	{
		ProactiveSuggestionCard proactiveSuggestionCard = FindCard(cardId);
		if ((object)proactiveSuggestionCard != null)
		{
			ClickyAnalytics.TrackSuggestionInteraction("home_space_suggestion_action", AnalyticsCard(proactiveSuggestionCard), action, hasFeedback, context);
		}
	}

	public ApprovalResult? Approve(Guid cardId, string? adjustmentText = null, bool announce = true, string inputMethod = "button")
	{
		ProactiveSuggestionCard card = FindCard(cardId);
		if ((object)card == null || card.IsExpired(Now))
		{
			return null;
		}
		JsonObject jsonObject = ClickyAnalytics.SuggestionInteractionProperties(AnalyticsCard(card), AnalyticsContext(inputMethod));
		jsonObject["was_adjusted"] = !string.IsNullOrWhiteSpace(adjustmentText);
		NoteSuggestionApprovedForMorningStreak();
		if (AdjustingCardId == cardId)
		{
			AdjustingCardId = null;
			adjustmentAnswerCount = 0;
			UpdateLiveStateHook();
		}
		RemoveCard(cardId);
		ScheduleExpiry();
		DateTimeOffset now = Now;
		store.Update(delegate(ProactiveStoreData d)
		{
			d.History = ProactiveHistory.Record(d.History, new ProactiveHistoryEntry(card.Title, card.Description, ProactiveResolution.Approved, now, null, true), now);
		});
		Report(new global::_003C_003Ez__ReadOnlySingleElementList<ProactiveSuggestionCard>(card), "approved");
		Func<ProactiveJobLaunch, bool> launchApprovedCardAsJob = ProactiveHooks.LaunchApprovedCardAsJob;
		if (launchApprovedCardAsJob != null)
		{
			bool flag = launchApprovedCardAsJob(JobLaunchFor(card, string.IsNullOrWhiteSpace(adjustmentText) ? null : adjustmentText.Trim()));
			jsonObject["queued"] = false;
			jsonObject["launch_dispatched"] = flag;
			jsonObject["launched_as_job"] = true;
			ClickyAnalytics.TrackProactiveAgentApproved(card.Title, card.Description, (DateTimeOffset.Now - card.CreatedAt).TotalSeconds, flag, card.CheckReason.Wire(), jsonObject);
			ProactiveQueueStatus status = new ProactiveQueueStatus(flag ? ProactiveQueueStatusKind.Starting : ProactiveQueueStatusKind.Paused);
			Log.Info("Proactive", $"approved \"{card.Title}\" → a job ({status.Title})");
			if (flag)
			{
				real.Core.BrainSession.NoteForNextTurn("[App] \"" + card.Title + "\", the job its card offered, is running now (see [Your jobs]): never start it again.");
			}
			if (announce && flag)
			{
				Func<bool>? isInterviewSendOffInProgress = FlowHooks.IsInterviewSendOffInProgress;
				if (isInterviewSendOffInProgress == null || !isInterviewSendOffInProgress())
				{
					Speak(status.Acknowledgment);
				}
			}
			RaiseChanged();
			return new ApprovalResult("", "a job on the right edge of the screen", status);
		}
		(string Slug, string Name, bool Founded, bool Restored) tuple = ResolveClicky(card);
		string item = tuple.Slug;
		string item2 = tuple.Name;
		bool item3 = tuple.Founded;
		bool item4 = tuple.Restored;
		QueuedProactiveTask queuedProactiveTask = new QueuedProactiveTask(card, item, string.IsNullOrWhiteSpace(adjustmentText) ? null : adjustmentText.Trim(), item3, item4);
		ProactiveQueueStatus proactiveQueueStatus = StatusFor(queuedProactiveTask);
		ProactiveQueueStatus status2;
		bool flag2;
		if (proactiveQueueStatus.Kind != 0)
		{
			queue.Add(queuedProactiveTask);
			PersistQueue();
			status2 = proactiveQueueStatus;
			flag2 = true;
		}
		else if (Launch(queuedProactiveTask))
		{
			status2 = new ProactiveQueueStatus(ProactiveQueueStatusKind.Starting);
			flag2 = true;
		}
		else
		{
			queue.Add(queuedProactiveTask with
			{
				IsPaused = true
			});
			PersistQueue();
			status2 = new ProactiveQueueStatus(ProactiveQueueStatusKind.Paused);
			flag2 = false;
		}
		Log.Info("Proactive", $"approved \"{card.Title}\" → {item2} ({status2.Title}){((queuedProactiveTask.AdjustmentText == null) ? "" : " with an adjustment")}");
		jsonObject["queued"] = proactiveQueueStatus.Kind != ProactiveQueueStatusKind.Ready;
		jsonObject["launch_dispatched"] = proactiveQueueStatus.Kind == ProactiveQueueStatusKind.Ready && flag2;
		jsonObject["clicky_id"] = ClickyAnalytics.ProductClickyIdentifier(item);
		ClickyAnalytics.TrackProactiveAgentApproved(card.Title, card.Description, (DateTimeOffset.Now - card.CreatedAt).TotalSeconds, flag2, card.CheckReason.Wire(), jsonObject);
		if (announce)
		{
			Speak(status2.Acknowledgment);
		}
		RaiseChanged();
		return new ApprovalResult(item, item2, status2);
	}

	public void Deny(Guid cardId, string? reason = null, string inputMethod = "button")
	{
		ProactiveSuggestionCard card = FindCard(cardId);
		if ((object)card != null)
		{
			string text = reason?.Trim();
			if (text != null && text.Length > 500)
			{
				text = text.Substring(0, 500);
			}
			string denyReason = (string.IsNullOrEmpty(text) ? null : text);
			JsonObject jsonObject = ClickyAnalytics.SuggestionInteractionProperties(AnalyticsCard(card), AnalyticsContext(inputMethod));
			jsonObject["has_feedback"] = denyReason != null;
			ClickyAnalytics.TrackProactiveAgentDenied(card.Title, card.Description, (DateTimeOffset.Now - card.CreatedAt).TotalSeconds, card.CheckReason.Wire(), denyReason, jsonObject);
			if (AdjustingCardId == cardId)
			{
				FinishAdjusting();
			}
			RemoveCard(cardId);
			ScheduleExpiry();
			DateTimeOffset now = Now;
			store.Update(delegate(ProactiveStoreData d)
			{
				d.History = ProactiveHistory.Record(d.History, new ProactiveHistoryEntry(card.Title, card.Description, ProactiveResolution.Denied, now, denyReason, true), now);
			});
			Report(new global::_003C_003Ez__ReadOnlySingleElementList<ProactiveSuggestionCard>(card), "denied", denyReason);
			Log.Info("Proactive", "denied \"" + card.Title + "\"" + ((denyReason == null) ? "" : " with a reason"));
			RaiseChanged();
		}
	}

	private (string Slug, string Name, bool Founded, bool Restored) ResolveClicky(ProactiveSuggestionCard card)
	{
		IReadOnlyList<ClickyInfo> clickys = real.Agents.Clickys;
		string requested = card.ClickySlug;
		if (requested != null)
		{
			ClickyInfo clickyInfo = clickys.FirstOrDefault((ClickyInfo c) => c.Slug == requested);
			if ((object)clickyInfo != null)
			{
				return (Slug: clickyInfo.Slug, Name: clickyInfo.Name, Founded: false, Restored: false);
			}
		}
		ProactiveNewClicky newClicky = card.NewClicky;
		string name2;
		string roleText;
		string oneLiner;
		if ((object)newClicky != null)
		{
			string name = newClicky.Name;
			if (name != null && name.Length > 0)
			{
				name2 = newClicky.Name;
				roleText = ((newClicky.Role.Length == 0) ? "Agent" : newClicky.Role);
				oneLiner = ((newClicky.Description.Length == 0) ? card.Description : newClicky.Description);
				goto IL_00e0;
			}
		}
		(string Name, string Role) tuple = FallbackClickyIdentity.For(card.Title);
		name2 = tuple.Name;
		roleText = tuple.Role;
		oneLiner = card.Description;
		goto IL_00e0;
		IL_00e0:
		string normalized = FallbackClickyIdentity.NormalizedName(name2);
		ClickyInfo clickyInfo2 = clickys.FirstOrDefault((ClickyInfo c) => FallbackClickyIdentity.NormalizedName(c.Name) == normalized);
		if ((object)clickyInfo2 != null)
		{
			return (Slug: clickyInfo2.Slug, Name: clickyInfo2.Name, Founded: false, Restored: false);
		}
		ClickyInfo clickyInfo3 = real.Agents.CreateClicky(name2, roleText, oneLiner);
		bool flag = DateTimeOffset.Now - clickyInfo3.CreatedAt > TimeSpan.FromMinutes(1L);
		Log.Info("Proactive", flag ? ("revived archived Clicky " + clickyInfo3.Name + " for a suggestion") : ("founded Clicky " + clickyInfo3.Name + " for a suggestion"));
		if (flag)
		{
			ClickyAnalytics.TrackProductEvent("clicky_restored", clickyInfo3.Slug);
		}
		else
		{
			ClickyRoster.Shared.TrackCreated(clickyInfo3.Slug, "agent");
		}
		return (Slug: clickyInfo3.Slug, Name: clickyInfo3.Name, Founded: !flag, Restored: flag);
	}

	private ProactiveQueueStatus StatusFor(QueuedProactiveTask entry)
	{
		return ProactiveQueueStatus.Resolve(entry, queue, real.Agents.IsClickyBusy(entry.ClickySlug));
	}

	public ProactiveQueueStatus QueueStatus(QueuedProactiveTask entry)
	{
		return StatusFor(entry);
	}

	private static ProactiveJobLaunch JobLaunchFor(ProactiveSuggestionCard card, string? adjustmentText)
	{
		string text;
		if (adjustmentText != null)
		{
			text = card.AgentPrompt + "\n\nThe user adjusted this task before starting it. In their words: \"" + adjustmentText + "\"\nFollow the adjustment wherever it differs from the brief above; keep everything it doesn't touch.";
		}
		else
		{
			text = card.AgentPrompt;
		}
		string agentBrief = text;
		string transcript = SuggestionApprovalMarker.Compose(card.Title, card.Description, agentBrief);
		string text2 = "The user approved a proactively suggested background task on its card, so HeyClicky is launching this Codex agent with the prebuilt brief. The brief was written by HeyClicky from a signal it picked up about the user's world, not by the user, so it may rest on something HeyClicky never actually had; the user's Yes approves the work, it does not fill in those blanks — if the brief hinges on one, ask for it in the chat instead of guessing. Their Yes on the card is also their approval for exactly what its title and pitch say it will do, a message or post it names included: do that without asking again. Anything the card didn't name, and any payment, refund, deletion or archiving, still waits for their yes on the exact thing.";
		if (card.CheckReason == ProactiveCheckReason.Onboarding)
		{
			text2 += " This ask was written during onboarding to show what you can do, so it may reach for an app (Gmail, Notion, Google Calendar, Sheets, Linear, GitHub) the user has not connected. Assume NOTHING is connected unless your own tool list says otherwise. Never stall on that: do the strongest app-free version now with web research and files you make (the people in a CSV, the emails drafted right here in the chat, the plan as a doc), and then, plainly and eagerly, tell the user which one app to connect to finish the job and exactly what you would do the moment they do (\"Connect Gmail and I'll send these as drafts to your inbox\"). One sentence, at the end, no nagging. This is one of their very first jobs, picked during onboarding so it needs nothing from them: finish it start to finish without asking anything (this overrides any ask-first guidance above). Where something is open, make the sensible choice from the brief and the web and say in one line what you assumed; never stop to ask and never end on a question.";
		}
		ProactiveRoutine routine = card.Routine;
		if ((object)routine != null)
		{
			text2 = text2 + " The card repeats (" + routine.CadenceLabel.ToLowerInvariant() + "): this is its first run, so do the whole task now; HeyClicky runs this job again on that schedule. Don't emit a <ROUTINE> block for it.";
		}
		return new ProactiveJobLaunch(card, transcript, text2);
	}

	private bool Launch(QueuedProactiveTask approval)
	{
		ClickyInfo clickyInfo = real.Agents.Clickys.FirstOrDefault((ClickyInfo c) => c.Slug == approval.ClickySlug);
		if ((object)clickyInfo == null || real.Agents.IsClickyBusy(approval.ClickySlug))
		{
			return false;
		}
		ProactiveSuggestionCard card = approval.Card;
		string adjustmentText = approval.AdjustmentText;
		string agentBrief = ((adjustmentText != null) ? (card.AgentPrompt + "\n\nThe user adjusted this task before starting it. In their words: \"" + adjustmentText + "\"\nFollow the adjustment wherever it differs from the brief above; keep everything it doesn't touch.") : card.AgentPrompt);
		string text = "The user approved a proactively suggested background task on its card, so HeyClicky is launching this Codex agent with the prebuilt brief. The brief was written by HeyClicky from a signal it picked up about the user's world, not by the user, so it may rest on something HeyClicky never actually had (their channel or handle, which account, the specific project); the user's Yes approves the work, it does not fill in those blanks — if the brief hinges on one, ask for it in the chat instead of guessing.";
		if (approval.WasFounded)
		{
			text += " You were founded just now for exactly this. This is your first conversation. The chat ALREADY shows, in your voice, a hello from you and the pitch for this task, so do NOT introduce yourself again: open with one or two plain sentences about the work itself, specific to this brief, then get straight to it. Fill in your AGENTS.md persona from the brief as part of that work, quietly.";
		}
		ProactiveRoutine routine = card.Routine;
		if ((object)routine != null)
		{
			text = text + " This task is a ROUTINE the user just approved: HeyClicky will re-run it " + routine.CadenceSpokenLabel + " on this PC, and this turn is run #1. Do the task fully now; later runs will be told to report only what is new. Do not set up any repeat yourself (no <ROUTINE> block) — HeyClicky has already registered it.";
		}
		if (card.CheckReason == ProactiveCheckReason.Onboarding)
		{
			text += " This ask was written during onboarding to show what you can do, so it may reach for an app (Gmail, Notion, Google Calendar, Sheets, Linear, GitHub) the user has not connected. Assume NOTHING is connected unless your own tool list says otherwise. Never stall on that: do the strongest app-free version now with web research and files you make (the people in a CSV, the emails drafted right here in the chat, the plan as a doc), and then, plainly and eagerly, tell the user which one app to connect to finish the job and exactly what you would do the moment they do (\"Connect Gmail and I'll send these as drafts to your inbox\"). One sentence, at the end, no nagging. This is one of their very first jobs, picked during onboarding so it needs nothing from them: finish it start to finish without asking anything (this overrides any ask-first guidance above). Where something is open, make the sensible choice from the brief and the web and say in one line what you assumed; never stop to ask and never end on a question.";
		}
		string text2 = SuggestionApprovalMarker.Compose(card.Title, card.Description, agentBrief);
		if (approval.WasFounded)
		{
			text2 = ClickyFoundingMarker.Compose(clickyInfo.Name, "", text2);
		}
		int num = queue.FindIndex((QueuedProactiveTask e) => e.Id == approval.Id);
		if (num >= 0)
		{
			queue[num] = queue[num]with
			{
				IsLaunching = true
			};
			PersistQueue();
		}
		string result;
		try
		{
			result = real.Agents.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = text2,
				ClickySlug = clickyInfo.Slug,
				LaunchContext = text,
				LaunchSource = "suggestion",
				UsageContext = "companion",
				IsGeneratedTaskBrief = true,
				ProactiveSuggestionId = card.ServerSuggestionId
			}).GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			Log.Warn("Proactive", "approved suggestion did not dispatch: " + ex.Message);
			if (num >= 0)
			{
				int num2 = queue.FindIndex((QueuedProactiveTask e) => e.Id == approval.Id);
				if (num2 >= 0)
				{
					queue[num2] = queue[num2]with
					{
						IsLaunching = false
					};
					PersistQueue();
				}
			}
			return false;
		}
		launchedTasks[result] = approval.Id;
		ClickyRoster.Shared.BindTask(result, clickyInfo.Slug);
		queue.RemoveAll((QueuedProactiveTask e) => e.Id == approval.Id);
		PersistQueue();
		ProactiveRoutine routine2 = card.Routine;
		if ((object)routine2 != null)
		{
			real.Agents.Routines.RegisterApprovedSuggestion(clickyInfo.Slug, string.IsNullOrWhiteSpace(routine2.Title) ? card.Title : routine2.Title, card.AgentPrompt, routine2.ClampedIntervalMinutes, result);
		}
		Log.Info("Proactive", $"approved suggestion \"{card.Title}\" launched on {clickyInfo.Name} as {result}");
		return true;
	}

	public void CancelQueued(Guid id)
	{
		queue.RemoveAll((QueuedProactiveTask e) => e.Id == id);
		PersistQueue();
		DrainQueue();
		RaiseChanged();
	}

	public void ResumeQueued(Guid id)
	{
		QueuedProactiveTask queuedProactiveTask = queue.FirstOrDefault((QueuedProactiveTask e) => e.Id == id);
		if ((object)queuedProactiveTask == null)
		{
			return;
		}
		for (int i = 0; i < queue.Count; i++)
		{
			if (queue[i].ClickySlug == queuedProactiveTask.ClickySlug)
			{
				queue[i] = queue[i]with
				{
					IsPaused = false
				};
			}
		}
		PersistQueue();
		DrainQueue();
		RaiseChanged();
	}

	private void DrainQueue()
	{
		if (accountKey == null || queue.Count == 0)
		{
			return;
		}
		bool flag = false;
		foreach (QueuedProactiveTask entry in QueuedProactiveTask.NextCandidates(queue))
		{
			if (StatusFor(entry).Kind != 0)
			{
				continue;
			}
			flag = true;
			if (!Launch(entry))
			{
				int num = queue.FindIndex((QueuedProactiveTask e) => e.Id == entry.Id);
				if (num >= 0)
				{
					queue[num] = queue[num]with
					{
						IsPaused = true,
						IsLaunching = false
					};
					PersistQueue();
				}
			}
		}
		if (flag)
		{
			RaiseChanged();
		}
	}

	private void OnAgentTaskEvent(AgentTaskEvent e)
	{
		AgentTaskEventKind kind = e.Kind;
		if ((uint)(kind - 8) > 2u)
		{
			return;
		}
		if (e.Kind != AgentTaskEventKind.Finished && launchedTasks.ContainsKey(e.TaskId))
		{
			string text = real.Agents.GetTaskDetail(e.TaskId)?.ClickySlug;
			for (int i = 0; i < queue.Count; i++)
			{
				if (queue[i].ClickySlug == text)
				{
					queue[i] = queue[i]with
					{
						IsPaused = true,
						IsLaunching = false
					};
				}
			}
			PersistQueue();
		}
		launchedTasks.Remove(e.TaskId);
		DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromMilliseconds(400L, 0L);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			DrainQueue();
		};
		dispatcherQueueTimer.Start();
		RaiseChanged();
	}

	public void BeginAdjusting(Guid cardId)
	{
		ProactiveSuggestionCard proactiveSuggestionCard = FindCard(cardId);
		if ((object)proactiveSuggestionCard != null && !proactiveSuggestionCard.IsExpired(Now) && host.AllowsChatSubmission())
		{
			ClickyAnalytics.TrackSuggestionInteraction("home_space_suggestion_action", AnalyticsCard(proactiveSuggestionCard), "adjust", hasFeedback: false, AnalyticsContext("button"));
			FinishAdjusting();
			AdjustingCardId = cardId;
			adjustmentAnswerCount = 0;
			UpdateLiveStateHook();
			RaiseChanged();
			real.Companion.AskWithAppFactAsync("[App] The user clicked Adjust on the suggestion currently in view. Ask one short, natural question about what they would like to change, then wait for their answer. Do not read the proposal back or start the task.", "suggestion_adjust");
		}
	}

	public bool FinishAdjusting()
	{
		if (!AdjustingCardId.HasValue)
		{
			return false;
		}
		AdjustingCardId = null;
		adjustmentAnswerCount = 0;
		UpdateLiveStateHook();
		if (real.Companion.State != 0)
		{
			real.Companion.Cancel();
		}
		RaiseChanged();
		return true;
	}

	public bool CancelAdjustingFromEscape()
	{
		Guid? adjustingCardId = AdjustingCardId;
		if (adjustingCardId.HasValue)
		{
			Guid valueOrDefault = adjustingCardId.GetValueOrDefault();
			TrackSuggestionAction(valueOrDefault, "cancel_adjustment", AnalyticsContext("keyboard", "escape_key"));
			return FinishAdjusting();
		}
		return false;
	}

	public ApprovalResult? ApproveWithTypedAdjustment(Guid cardId, string text)
	{
		return Approve(cardId, text, announce: true, "text");
	}

	private string? AdjustmentNote()
	{
		Guid? adjustingCardId = AdjustingCardId;
		if (adjustingCardId.HasValue)
		{
			Guid valueOrDefault = adjustingCardId.GetValueOrDefault();
			return SuggestionVoiceAction.ContextNote(FindCard(valueOrDefault), Now) + "\n" + SuggestionVoiceAction.AdjustmentBudget(adjustmentAnswerCount);
		}
		if (IsSuggestionsPageVisible)
		{
			adjustingCardId = CurrentCardId;
			if (adjustingCardId.HasValue)
			{
				Guid valueOrDefault2 = adjustingCardId.GetValueOrDefault();
				return SuggestionVoiceAction.ContextNote(FindCard(valueOrDefault2), Now);
			}
		}
		return null;
	}

	public void NoteAdjustmentTurnCompleted()
	{
		Guid? adjustingCardId = AdjustingCardId;
		if (adjustingCardId.HasValue)
		{
			Guid valueOrDefault = adjustingCardId.GetValueOrDefault();
			if (adjustmentAnswerCount > 0)
			{
				TrackSuggestionAction(valueOrDefault, "adjustment_answer", AnalyticsContext("voice"), hasFeedback: true);
			}
			adjustmentAnswerCount++;
			UpdateLiveStateHook();
		}
	}

	private void UpdateLiveStateHook()
	{
		liveNoteSnapshot = ((AdjustingCardId.HasValue || IsSuggestionsPageVisible) ? AdjustmentNote() : null);
		ProactiveHooks.LiveStateNote = ((liveNoteSnapshot == null) ? null : ((Func<string>)(() => liveNoteSnapshot)));
	}

	public string HandleVoiceAction(SuggestionVoiceAction.Request request)
	{
		switch (request.Kind)
		{
		case SuggestionVoiceAction.Kind.Approve:
		{
			ApprovalResult approvalResult = Approve(request.CardId, request.Detail, announce: false, "voice");
			if ((object)approvalResult != null)
			{
				return $"{{\"status\":\"approved\",\"clicky_name\":{JsonSerializer.Serialize(approvalResult.ClickyName)},\"message\":{JsonSerializer.Serialize("The suggested task was approved. Say exactly: \"" + approvalResult.Status.Acknowledgment + "\" Nothing else. Do not recap the task, introduce the Clicky, explain the routine, promise an update, or ask a follow-up question.")}}}";
			}
			return "{\"status\":\"error\",\"message\":\"That suggestion is no longer available. Say so in one short line.\"}";
		}
		case SuggestionVoiceAction.Kind.Skip:
		{
			if ((object)FindCard(request.CardId) == null)
			{
				return "{\"status\":\"error\",\"message\":\"That suggestion is no longer available.\"}";
			}
			Deny(request.CardId, request.Detail, "voice");
			ProactiveSuggestionCard proactiveSuggestionCard = VisibleCards.FirstOrDefault();
			if ((object)proactiveSuggestionCard != null)
			{
				return "{\"status\":\"skipped\",\"next\":" + JsonSerializer.Serialize(proactiveSuggestionCard.Title) + "}";
			}
			return "{\"status\":\"skipped\",\"next\":null,\"message\":\"Skipped. There are no suggestions left.\"}";
		}
		default:
			return "{\"status\":\"error\",\"message\":\"That suggestion reference is not valid.\"}";
		}
	}

	private void Speak(string line)
	{
		Task.Run(async delegate
		{
			try
			{
				if (await real.Companion.Mouth.EnsureOpenAsync().ConfigureAwait(continueOnCapturedContext: false))
				{
					real.Companion.Mouth.SpeakLine(line, -50);
				}
			}
			catch (Exception ex)
			{
				Log.Info("Proactive", "acknowledgment not spoken: " + ex.Message);
			}
		});
	}

	public void DebugFetchNow()
	{
		StartJob(ProactiveCheckReason.Morning);
	}

	public void OnDebugChord(ShortcutAction action)
	{
	}

	public void DebugReset()
	{
		CancelJob();
		DismissMorningNotice();
		cards = new List<ProactiveSuggestionCard>();
		string text = accountKey;
		if (text != null)
		{
			store.UpdateAccount(text, delegate(ProactiveAccountState a)
			{
				a.PendingJob = null;
				a.Cards = new List<ProactiveSuggestionCard>();
				a.Slots = new ProactiveSlotDayState(null, null, null);
				a.SuggestionsLastFetchedAt = null;
				a.SuggestionsRowLastSeenAt = null;
				a.IgnoredSuggestionsBreakUntil = null;
			});
		}
		store.Update(delegate(ProactiveStoreData d)
		{
			d.History = new List<ProactiveHistoryEntry>();
			d.MorningSettledAt = null;
			d.MorningDismissals = MorningDismissalHistory.Empty;
		});
		LastFetchedAt = null;
		Log.Info("Proactive", "debug reset: suggestion state cleared");
		RaiseChanged();
	}

	public void ForgetForAccountDeletion()
	{
		CancelJob();
		DismissMorningNotice();
		cards = new List<ProactiveSuggestionCard>();
		store.Update(delegate(ProactiveStoreData d)
		{
			d.Accounts.Clear();
			d.History = new List<ProactiveHistoryEntry>();
		});
		LastFetchedAt = null;
		RaiseChanged();
	}

	public void MarkRowSeen()
	{
		string text = accountKey;
		if (text != null)
		{
			store.UpdateAccount(text, delegate(ProactiveAccountState a)
			{
				a.SuggestionsRowLastSeenAt = Now;
			});
		}
	}

	public void MarkRowUnread()
	{
		string text = accountKey;
		if (text != null)
		{
			store.UpdateAccount(text, delegate(ProactiveAccountState a)
			{
				a.SuggestionsRowLastSeenAt = DateTimeOffset.MinValue;
			});
		}
		RaiseChanged();
	}

	public void OnSuggestionsPageOpened()
	{
		IsSuggestionsPageVisible = true;
		UpdateLiveStateHook();
		SettleMorningNoticeForToday();
		SyncStatusesFromServer(force: true);
		ExpireOutdatedCards();
		StartJob(ProactiveCheckReason.HomeSpace, resumeOnly: true);
		MarkRowSeen();
		if (catalog.Count == 0)
		{
			RefreshIntegrationsAsync();
		}
	}

	public void OnSuggestionsPageClosed()
	{
		IsSuggestionsPageVisible = false;
		FinishAdjusting();
		UpdateLiveStateHook();
	}

	public void RefreshFromSuggestions()
	{
		StartJob(ProactiveCheckReason.HomeSpace);
	}

	public string HeaderStatusLine()
	{
		int count = VisibleCards.Count;
		if (IsFetching)
		{
			if (count != 0)
			{
				return "Looking for fresh ideas. This can take 5–10 minutes.";
			}
			return "Looking through your connected apps.";
		}
		string fetchError = FetchError;
		if (fetchError != null)
		{
			return fetchError;
		}
		string text = count switch
		{
			0 => "No new suggestions right now.", 
			1 => "One idea, ready when you are.", 
			_ => $"{count} ideas, ready when you are.", 
		};
		DateTimeOffset? lastFetchedAt = LastFetchedAt;
		if (!lastFetchedAt.HasValue)
		{
			return text;
		}
		return text + " Last checked " + lastFetchedAt.GetValueOrDefault().LocalDateTime.ToString("h:mm tt", CultureInfo.GetCultureInfo("en-US")) + ".";
	}

	public string? ClickyName(string? slug)
	{
		if (slug != null)
		{
			return real.Agents.Clickys.FirstOrDefault((ClickyInfo c) => c.Slug == slug)?.Name;
		}
		return null;
	}

	public ProactiveSuggestionCard? AppendTourCard(ProactiveSuggestionCard card)
	{
		ProactiveSuggestionCard proactiveSuggestionCard = VisibleCards.FirstOrDefault((ProactiveSuggestionCard c) => c.Id == card.Id) ?? VisibleCards.FirstOrDefault((ProactiveSuggestionCard c) => string.Equals(c.Title, card.Title, StringComparison.OrdinalIgnoreCase));
		if ((object)proactiveSuggestionCard != null)
		{
			return proactiveSuggestionCard;
		}
		ProactiveSuggestionCard proactiveSuggestionCard2 = card with
		{
			CreatedAt = Now
		};
		cards.Insert(0, proactiveSuggestionCard2);
		PersistCards();
		Report(new global::_003C_003Ez__ReadOnlySingleElementList<ProactiveSuggestionCard>(proactiveSuggestionCard2), "presented");
		ClickyAnalytics.TrackProactiveAgentsRecommended(new global::_003C_003Ez__ReadOnlySingleElementList<string>(proactiveSuggestionCard2.Title), proactiveSuggestionCard2.CheckReason.Wire());
		Log.Info("Proactive", "tour card added to Suggestions: " + proactiveSuggestionCard2.Title);
		RaiseChanged();
		return proactiveSuggestionCard2;
	}

	public void RestoreCards(IReadOnlyList<ProactiveSuggestionCard> restored)
	{
		HashSet<string> live = VisibleCards.Select((ProactiveSuggestionCard c) => c.Title.ToLowerInvariant()).ToHashSet();
		List<ProactiveSuggestionCard> list = restored.Where((ProactiveSuggestionCard c) => !live.Contains(c.Title.ToLowerInvariant())).ToList();
		if (list.Count != 0)
		{
			cards.AddRange(list);
			PersistCards();
			ScheduleExpiry();
			RaiseChanged();
		}
	}

	public IReadOnlyList<ProactiveSuggestionCard> LiveCards(params ProactiveCheckReason[] reasons)
	{
		return cards.Where((ProactiveSuggestionCard c) => !c.IsExpired(Now) && reasons.Contains(c.CheckReason)).ToList();
	}

	private Task<IReadOnlyList<AgentIntegrationInfo>> RefreshIntegrationsAsync()
	{
		Task<IReadOnlyList<AgentIntegrationInfo>> task = catalogLoad;
		if (task != null && !task.IsCompleted)
		{
			return catalogLoad;
		}
		return catalogLoad = LoadAsync();
		async Task<IReadOnlyList<AgentIntegrationInfo>> LoadAsync()
		{
			try
			{
				IReadOnlyList<AgentIntegrationInfo> list = await backend.FetchIntegrationsAsync().ConfigureAwait(continueOnCapturedContext: false);
				dq.TryEnqueue(delegate
				{
					catalog = list;
					catalogFetchedAt = DateTimeOffset.Now;
					(string, DateTimeOffset)? tuple = pendingSuggestionConnect;
					if (tuple.HasValue)
					{
						(string Slug, DateTimeOffset At) pending = tuple.GetValueOrDefault();
						AgentIntegrationInfo agentIntegrationInfo = list.FirstOrDefault((AgentIntegrationInfo i) => i.PrimaryToolkitSlug.Equals(pending.Slug, StringComparison.OrdinalIgnoreCase));
						if ((object)agentIntegrationInfo != null && agentIntegrationInfo.ConnectedCount > 0)
						{
							pendingSuggestionConnect = null;
							ClickyAnalytics.TrackIntegrationConnected(agentIntegrationInfo.Id, agentIntegrationInfo.DisplayName, agentIntegrationInfo.ToolkitSlugs, "integration_suggestion", agentIntegrationInfo.Status, agentIntegrationInfo.ConnectedCount, null);
							goto IL_0106;
						}
					}
					tuple = pendingSuggestionConnect;
					if (tuple.HasValue)
					{
						(string, DateTimeOffset) valueOrDefault = tuple.GetValueOrDefault();
						if (DateTimeOffset.Now - valueOrDefault.Item2 > TimeSpan.FromMinutes(30L))
						{
							pendingSuggestionConnect = null;
						}
					}
					goto IL_0106;
					IL_0106:
					IEnumerable<string> availableToolkits = list.Where((AgentIntegrationInfo i) => !i.IsLocal && i.IsEligibleForConnectionSuggestion).SelectMany((AgentIntegrationInfo i) => i.ToolkitSlugs.Append(i.Id).Append(i.PrimaryToolkitSlug));
					integrationRules.SetAvailableToolkits(availableToolkits);
					NotchSurfaces.UseCasesFor = UseCasesFor;
					Log.Info("Integrations", $"catalog: {list.Count} app(s), {list.Count((AgentIntegrationInfo i) => i.IsActiveConnection)} connected, {integrationRules.AvailableToolkitCount} promptable");
				});
				return list;
			}
			catch (Exception ex)
			{
				Log.Info("Integrations", "catalog fetch failed: " + ex.Message);
				return catalog;
			}
		}
	}

	private IReadOnlyList<string> UseCasesFor(string toolkit)
	{
		AgentIntegrationInfo agentIntegrationInfo = catalog.FirstOrDefault((AgentIntegrationInfo i) => i.ToolkitSlugs.Contains<string>(toolkit, StringComparer.OrdinalIgnoreCase) || i.Id.Equals(toolkit, StringComparison.OrdinalIgnoreCase));
		if ((object)agentIntegrationInfo != null)
		{
			IReadOnlyList<string> useCases = agentIntegrationInfo.UseCases;
			if (useCases != null && useCases.Count > 0)
			{
				return agentIntegrationInfo.UseCases;
			}
		}
		return new global::_003C_003Ez__ReadOnlyArray<string>(new string[4] { "Answer questions with your data", "Automate busywork", "Draft and summarize", "Keep things in sync" });
	}

	private void UpdateIntegrationMonitor()
	{
		if (accountKey == null)
		{
			StopIntegrationMonitor();
			return;
		}
		if ((object)integrationTimer == null)
		{
			integrationTimer = dq.CreateTimer();
			integrationTimer.Interval = IntegrationSuggestionRules.PollInterval;
			integrationTimer.Tick += delegate
			{
				SampleForegroundActivity();
			};
		}
		if (!integrationTimer.IsRunning)
		{
			integrationTimer.Start();
			Log.Info("Integrations", "suggestion monitor started");
		}
	}

	private void StopIntegrationMonitor()
	{
		integrationTimer?.Stop();
		integrationRules.ResetAll();
		if ((object)activeIntegrationSuggestion != null)
		{
			activeIntegrationSuggestion = null;
			host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.IntegrationSuggestion));
		}
		if (spotifyTipActive)
		{
			spotifyTipActive = false;
			host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.SpotifyVoiceTip));
		}
	}

	private string? IntegrationSuppressionReason()
	{
		bool isSignedIn = IsSignedIn;
		bool canAdvancePastPermissions = CanAdvancePastPermissions;
		bool hasCompletedOnboarding = HasCompletedOnboarding;
		string quietReason = AmbientQuietReason();
		NotchPresentation presentation = host.Services.Presenter.Presentation;
		bool notchExpanded = ((presentation is NotchPresentation.TextInput || presentation is NotchPresentation.TextResponse || presentation is NotchPresentation.OnboardingPinned || (presentation is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.AgentPeek)) ? true : false);
		return IntegrationSuggestionRules.SuppressionReason(new IntegrationSuggestionRules.SuppressionInputs(isSignedIn, canAdvancePastPermissions, hasCompletedOnboarding, quietReason, notchExpanded, spotifyTipActive, (object)activeIntegrationSuggestion != null, real.Companion.State != VoiceState.Idle, host.IsPreSignInIntroShowing || host.IsIntroSequenceActive, IsTutorialEngaged, IsWithinPostOnboardingHeadsUpQuietPeriod));
	}

	private static string? AmbientQuietReason()
	{
		if (!QuietContext.IgnoredForTests)
		{
			return IntegrationSuggestionRules.AmbientQuietReason(QuietContext.IsAnotherAppUsingMicrophone(), doNotDisturb: false);
		}
		return null;
	}

	private void SampleForegroundActivity()
	{
		if (integrationSampleInFlight)
		{
			return;
		}
		if (DateTimeOffset.Now - catalogFetchedAt > CatalogRefreshInterval)
		{
			RefreshIntegrationsAsync();
		}
		string text = IntegrationSuppressionReason();
		if (text != lastSuppression)
		{
			lastSuppression = text;
			if (text != null)
			{
				Log.Info("Integrations", "sampling suppressed: " + text + ((text == "quietContext") ? (" (" + AmbientQuietReason() + ")") : ""));
			}
		}
		if (text != null)
		{
			DateTimeOffset now = Now;
			integrationRules.Sample(null, now, suppressed: true, store.Read((ProactiveStoreData d) => d));
			return;
		}
		integrationSampleInFlight = true;
		Task.Run(async delegate
		{
			ForegroundActivity activity = null;
			try
			{
				ForegroundApp foregroundApp = await ForegroundAppInfo.CaptureAsync().ConfigureAwait(continueOnCapturedContext: false);
				if ((object)foregroundApp != null && foregroundApp.ProcessId != Environment.ProcessId)
				{
					bool isFirefox;
					bool isBrowser = ForegroundAppInfo.IsBrowserProcess(foregroundApp.ProcessName, out isFirefox);
					(string? Host, string? Path) tuple = ParseAddress(foregroundApp.BrowserUrl);
					string item = tuple.Host;
					string item2 = tuple.Path;
					activity = new ForegroundActivity(foregroundApp.ProcessName, foregroundApp.AppName, isBrowser, item, item2, foregroundApp.WindowTitle);
				}
			}
			catch (Exception ex)
			{
				Log.Info("Integrations", "foreground sample: " + ex.Message);
			}
			dq.TryEnqueue(delegate
			{
				integrationSampleInFlight = false;
				ProactiveStoreData state = store.Read((ProactiveStoreData d) => d);
				var (sampleOutcome, integrationCandidate) = integrationRules.Sample(activity, Now, IntegrationSuppressionReason() != null, state);
				switch (sampleOutcome)
				{
				case IntegrationSuggestionRules.SampleOutcome.SpotifyTipReady:
					PresentSpotifyTip();
					break;
				case IntegrationSuggestionRules.SampleOutcome.CandidateReady:
					if ((object)integrationCandidate != null)
					{
						PresentIntegrationSuggestion(integrationCandidate);
					}
					break;
				}
			});
		});
	}

	private static (string? Host, string? Path) ParseAddress(string? raw)
	{
		string text = raw?.Trim();
		if (string.IsNullOrEmpty(text) || text.Contains(' '))
		{
			return (Host: null, Path: null);
		}
		if (!text.Contains("://", StringComparison.Ordinal))
		{
			text = "https://" + text;
		}
		if (!Uri.TryCreate(text, UriKind.Absolute, out Uri result) || string.IsNullOrEmpty(result.Host))
		{
			return (Host: null, Path: null);
		}
		return (Host: result.Host.ToLowerInvariant().Trim('.'), Path: result.AbsolutePath);
	}

	private void PresentIntegrationSuggestion(IntegrationCandidate candidate)
	{
		string text = IntegrationSuppressionReason();
		if (text != null)
		{
			Log.Info("Integrations", "suggestion suppressed before presentation (" + text + ") toolkit=" + candidate.ToolkitSlug);
			return;
		}
		AgentIntegrationInfo agentIntegrationInfo = catalog.FirstOrDefault((AgentIntegrationInfo i) => i.ToolkitSlugs.Contains<string>(candidate.ToolkitSlug, StringComparer.OrdinalIgnoreCase) || i.Id.Equals(candidate.ToolkitSlug, StringComparison.OrdinalIgnoreCase));
		if ((object)agentIntegrationInfo == null || !agentIntegrationInfo.IsEligibleForConnectionSuggestion)
		{
			integrationRules.ResetDwell(candidate.ToolkitSlug);
			return;
		}
		activeIntegrationSuggestion = candidate with
		{
			ToolkitSlug = agentIntegrationInfo.PrimaryToolkitSlug,
			DisplayName = agentIntegrationInfo.DisplayName
		};
		Log.Info("Integrations", $"presenting integration suggestion toolkit={agentIntegrationInfo.PrimaryToolkitSlug} (from {candidate.SourceApplicationName}{((candidate.SourceHost == null) ? "" : (" " + candidate.SourceHost))})");
		host.Services.Presenter.ShowLiveEvent(new NotchLiveEvent.IntegrationSuggestion(agentIntegrationInfo.PrimaryToolkitSlug, agentIntegrationInfo.DisplayName), IntegrationSuggestionAutoHide);
		TrackIntegrationSuggestion("presented", activeIntegrationSuggestion);
		PlayEarcon("connectionQuestion", 0.85f);
		integrationRules.ResetDwell(candidate.ToolkitSlug);
		DateTimeOffset now = Now;
		store.Update(delegate(ProactiveStoreData d)
		{
			d.IntegrationPromptTimestamps = d.IntegrationPromptTimestamps.Where((DateTimeOffset t) => t >= now - IntegrationSuggestionRules.PromptsWindow).Append(now).ToList();
		});
		WatchForIntegrationCardGone();
	}

	private void WatchForIntegrationCardGone()
	{
		DispatcherQueueTimer t = dq.CreateTimer().Rooted();
		t.Interval = TimeSpan.FromSeconds(1L);
		bool seen = false;
		t.Tick += delegate
		{
			if (host.Services.Presenter.Presentation is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.IntegrationSuggestion)
			{
				seen = true;
			}
			else if (seen || (object)activeIntegrationSuggestion == null)
			{
				t.Stop();
				IntegrationCandidate integrationCandidate = activeIntegrationSuggestion;
				if ((object)integrationCandidate != null && seen)
				{
					integrationRules.ResetDwell(integrationCandidate.ToolkitSlug);
					activeIntegrationSuggestion = null;
				}
			}
		};
		t.Start();
	}

	public void AnswerIntegrationSuggestion(string? toolkit, IntegrationAnswer answer)
	{
		switch (answer)
		{
		case IntegrationAnswer.Yes:
			AcceptIntegrationSuggestion(toolkit);
			break;
		case IntegrationAnswer.NotNow:
			SnoozeIntegrationSuggestion(toolkit);
			break;
		default:
			DeclineIntegrationSuggestion(toolkit);
			break;
		}
	}

	private void TrackIntegrationSuggestion(string outcome, IntegrationCandidate? suggestion)
	{
		if ((object)suggestion != null)
		{
			AgentIntegrationInfo agentIntegrationInfo = catalog.FirstOrDefault((AgentIntegrationInfo i) => i.PrimaryToolkitSlug.Equals(suggestion.ToolkitSlug, StringComparison.OrdinalIgnoreCase));
			ClickyAnalytics.TrackIntegrationSuggestion(outcome, suggestion.ToolkitSlug, agentIntegrationInfo?.DisplayName ?? suggestion.DisplayName, agentIntegrationInfo?.UseCases ?? Array.Empty<string>(), agentIntegrationInfo?.LogoUrl, suggestion.SourceApplicationName, suggestion.SourceHost);
		}
	}

	private string? AnsweredSlug(string? toolkit)
	{
		object obj = activeIntegrationSuggestion?.ToolkitSlug;
		if (obj == null)
		{
			if (!string.IsNullOrWhiteSpace(toolkit))
			{
				return toolkit.ToLowerInvariant();
			}
			obj = null;
		}
		return (string?)obj;
	}

	public void AcceptIntegrationSuggestion(string? toolkit = null)
	{
		string slug = AnsweredSlug(toolkit);
		if (slug == null)
		{
			ClearIntegrationSuggestion();
			return;
		}
		TrackIntegrationSuggestion("accepted", activeIntegrationSuggestion);
		AgentIntegrationInfo agentIntegrationInfo = catalog.FirstOrDefault((AgentIntegrationInfo i) => i.PrimaryToolkitSlug.Equals(slug, StringComparison.OrdinalIgnoreCase));
		ClickyAnalytics.TrackIntegrationInitiated(agentIntegrationInfo?.Id ?? slug, agentIntegrationInfo?.DisplayName ?? activeIntegrationSuggestion?.DisplayName ?? slug, agentIntegrationInfo?.ToolkitSlugs ?? new global::_003C_003Ez__ReadOnlySingleElementList<string>(slug), "integration_suggestion", "not_connected", 0, 1);
		pendingSuggestionConnect = (slug, DateTimeOffset.Now);
		Snooze(slug);
		ClearIntegrationSuggestion();
		Log.Info("Integrations", "suggestion accepted: connecting " + slug);
		Task.Run(async delegate
		{
			try
			{
				Uri url = await backend.ConnectIntegrationAsync(slug).ConfigureAwait(continueOnCapturedContext: false);
				if ((object)url == null)
				{
					Log.Warn("Integrations", "connect returned no sign-in URL");
				}
				else
				{
					int baseline = catalog.FirstOrDefault((AgentIntegrationInfo i) => Matches(i, slug))?.ConnectedCount ?? 0;
					dq.TryEnqueue(delegate
					{
						pendingConnection = new PendingConnection(slug, baseline, DateTimeOffset.Now);
						PollPendingConnectionAsync();
						host.FoldNotchForOpenedLink();
						host.OpenUrl(url.ToString());
						real.Agents.InvalidateConnectedApps();
					});
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Integrations", "connect " + slug + " failed: " + ex.Message);
			}
		});
	}

	private static bool Matches(AgentIntegrationInfo i, string slug)
	{
		if (!i.Id.Equals(slug, StringComparison.OrdinalIgnoreCase))
		{
			return i.ToolkitSlugs.Contains<string>(slug, StringComparer.OrdinalIgnoreCase);
		}
		return true;
	}

	public bool OnIntegrationAuthCallback(string? toolkit, bool failed)
	{
		PendingConnection pending = pendingConnection;
		if ((object)pending == null)
		{
			return false;
		}
		if (toolkit != null && !toolkit.Equals(pending.Slug, StringComparison.OrdinalIgnoreCase) && !catalog.Any((AgentIntegrationInfo i) => Matches(i, pending.Slug) && Matches(i, toolkit)))
		{
			return false;
		}
		if (failed)
		{
			Log.Info("Integrations", "suggested connection failed or was cancelled: " + pending.Slug);
			pendingConnection = null;
			return true;
		}
		VerifyPendingConnectionAsync();
		return true;
	}

	private async Task PollPendingConnectionAsync()
	{
		while (true)
		{
			PendingConnection pending = pendingConnection;
			if ((object)pending == null)
			{
				break;
			}
			await Task.Delay(4000);
			if (pendingConnection != pending)
			{
				break;
			}
			if (DateTimeOffset.Now - pending.Started > PendingConnectionLifetime)
			{
				pendingConnection = null;
				break;
			}
			await VerifyPendingConnectionAsync();
		}
	}

	private async Task VerifyPendingConnectionAsync()
	{
		if ((object)pendingConnection == null)
		{
			return;
		}
		IReadOnlyList<AgentIntegrationInfo> source = await RefreshIntegrationsAsync();
		PendingConnection pending = pendingConnection;
		if ((object)pending != null)
		{
			AgentIntegrationInfo agentIntegrationInfo = source.FirstOrDefault((AgentIntegrationInfo i) => Matches(i, pending.Slug));
			if ((object)agentIntegrationInfo != null && agentIntegrationInfo.IsActiveConnection && agentIntegrationInfo.ConnectedCount > pending.Baseline)
			{
				pendingConnection = null;
				Log.Info("Integrations", $"connection verified: {agentIntegrationInfo.Id} ({agentIntegrationInfo.ConnectedCount} account(s))");
				real.Agents.InvalidateConnectedApps();
				PresentIntegrationConnected(agentIntegrationInfo);
			}
		}
	}

	private void PresentIntegrationConnected(AgentIntegrationInfo entry)
	{
		string text = IntegrationConnectedRefusal();
		if (text != null)
		{
			Log.Info("Integrations", "connected toast held back (" + text + "): " + entry.Id);
			return;
		}
		Log.Info("Integrations", "presenting the connected toast: " + entry.DisplayName);
		host.Services.Presenter.ShowLiveEvent(new NotchLiveEvent.IntegrationConnected(entry.PrimaryToolkitSlug, entry.DisplayName, entry.LogoUrl), null);
	}

	private string? IntegrationConnectedRefusal()
	{
		if (host.IsPreSignInIntroShowing || host.IsIntroSequenceActive || host.TourHoldsHome)
		{
			return "intro";
		}
		if (!CanAdvancePastPermissions)
		{
			return "permissionsIncomplete";
		}
		if (!HasCompletedOnboarding)
		{
			return "onboardingIncomplete";
		}
		if (real.Companion.State != 0)
		{
			return "voiceBusy";
		}
		NotchPresentation presentation = host.Services.Presenter.Presentation;
		if (!(presentation is NotchPresentation.TextInput) && !(presentation is NotchPresentation.TextResponse) && !(presentation is NotchPresentation.OnboardingPinned))
		{
			if (presentation is NotchPresentation.LiveEvent liveEvent)
			{
				NotchLiveEvent @event = liveEvent.Event;
				if (@event is NotchLiveEvent.ActivityListening || @event is NotchLiveEvent.ActivityThinking || @event is NotchLiveEvent.ActivitySpeaking || @event is NotchLiveEvent.ActivityCustomStatus || @event is NotchLiveEvent.Dictation || @event is NotchLiveEvent.MeetingCountdown || @event is NotchLiveEvent.OnboardingTutorialInvite || @event is NotchLiveEvent.OnboardingTutorialResume || @event is NotchLiveEvent.MorningSuggestions || @event is NotchLiveEvent.AgentPeek)
				{
					return "notchBusy";
				}
			}
			return null;
		}
		return "notchExpanded";
	}

	public void SnoozeIntegrationSuggestion(string? toolkit = null)
	{
		string text = AnsweredSlug(toolkit);
		if (text == null)
		{
			ClearIntegrationSuggestion();
			return;
		}
		TrackIntegrationSuggestion("snoozed", activeIntegrationSuggestion);
		Snooze(text);
		ClearIntegrationSuggestion();
		Log.Info("Integrations", "suggestion snoozed: " + text);
	}

	public void DeclineIntegrationSuggestion(string? toolkit = null)
	{
		string slug = AnsweredSlug(toolkit);
		if (slug == null)
		{
			ClearIntegrationSuggestion();
			return;
		}
		TrackIntegrationSuggestion("declined", activeIntegrationSuggestion);
		store.Update(delegate(ProactiveStoreData d)
		{
			if (!d.IntegrationNeverAsk.Contains(slug))
			{
				d.IntegrationNeverAsk.Add(slug);
			}
		});
		integrationRules.ResetDwell(slug);
		ClearIntegrationSuggestion();
		Log.Info("Integrations", "suggestion declined: never asking about " + slug + " again");
	}

	private void Snooze(string slug)
	{
		DateTimeOffset until = Now + IntegrationSuggestionRules.NotNowSnooze;
		store.Update(delegate(ProactiveStoreData d)
		{
			d.IntegrationSnoozedUntil[slug] = until;
		});
		integrationRules.ResetDwell(slug);
	}

	private void ClearIntegrationSuggestion()
	{
		activeIntegrationSuggestion = null;
		host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.IntegrationSuggestion));
	}

	private void PresentSpotifyTip()
	{
		if (!spotifyTipActive && IntegrationSuppressionReason() == null)
		{
			spotifyTipActive = true;
			store.Update(delegate(ProactiveStoreData d)
			{
				d.SpotifyVoiceTipShown = true;
			});
			integrationRules.ResetSpotifyDwell();
			Log.Info("Integrations", "presenting the Spotify voice tip");
			host.Services.Presenter.ShowLiveEvent(new NotchLiveEvent.SpotifyVoiceTip(), null);
			PlayEarcon("connectionQuestion", 0.85f);
		}
	}

	public void AcknowledgeSpotifyTip()
	{
		spotifyTipActive = false;
		store.Update(delegate(ProactiveStoreData d)
		{
			d.SpotifyVoiceTipShown = true;
		});
		host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.SpotifyVoiceTip));
	}

	public void ResetIntegrationSuggestionPreferences()
	{
		StopIntegrationMonitor();
		store.Update(delegate(ProactiveStoreData d)
		{
			d.IntegrationNeverAsk.Clear();
			d.IntegrationSnoozedUntil.Clear();
			d.IntegrationPromptTimestamps.Clear();
			d.SpotifyVoiceTipShown = false;
		});
		UpdateIntegrationMonitor();
	}

	private void UpdateMeetingsScheduler()
	{
		if (accountKey == null || !IsUpcomingMeetingsEnabled)
		{
			meetingsTimer?.Stop();
			if ((object)activeMeeting != null)
			{
				ClearActiveMeeting();
			}
			upcomingEvents = Array.Empty<UpcomingCalendarEvent>();
			return;
		}
		if ((object)meetingsTimer == null)
		{
			meetingsTimer = dq.CreateTimer();
			meetingsTimer.Interval = MeetingCountdownRules.TickInterval;
			meetingsTimer.Tick += delegate
			{
				TickMeetings();
			};
		}
		if (!meetingsTimer.IsRunning)
		{
			meetingsTimer.Start();
		}
		TickMeetings();
	}

	private void TickMeetings()
	{
		if (accountKey != null && IsUpcomingMeetingsEnabled)
		{
			RefreshMeetingsIfStale();
			UpdateActiveMeeting();
		}
	}

	public void RefreshMeetingsForNotchExpand()
	{
		RefreshMeetingsIfStale(triggeredByNotchExpand: true);
	}

	private void RefreshMeetingsIfStale(bool triggeredByNotchExpand = false)
	{
		if (accountKey == null || !IsUpcomingMeetingsEnabled || meetingsRefreshInFlight)
		{
			return;
		}
		if (meetingsClient.TestResponse == null && catalog.Count > 0 && !catalog.Any((AgentIntegrationInfo i) => i.IsActiveConnection && i.PrimaryToolkitSlug.Equals("googlecalendar", StringComparison.OrdinalIgnoreCase)))
		{
			meetingsBackendConnected = false;
			upcomingEvents = Array.Empty<UpcomingCalendarEvent>();
		}
		else
		{
			if (!MeetingCountdownRules.ShouldRefresh(DateTimeOffset.Now, meetingsRefreshedAt, meetingsFailedAt, meetingsBackendConnected, triggeredByNotchExpand))
			{
				return;
			}
			meetingsRefreshInFlight = true;
			int generation = meetingsAuthGeneration;
			Task.Run(async delegate
			{
				UpcomingCalendarFetch result = await meetingsClient.FetchAsync().ConfigureAwait(continueOnCapturedContext: false);
				dq.TryEnqueue(delegate
				{
					meetingsRefreshInFlight = false;
					if (generation == meetingsAuthGeneration)
					{
						if ((object)result == null)
						{
							meetingsFailedAt = DateTimeOffset.Now;
						}
						else
						{
							meetingsFailedAt = null;
							meetingsRefreshedAt = DateTimeOffset.Now;
							meetingsBackendConnected = result.IsCalendarConnected;
							upcomingEvents = result.Events;
							Log.Info("Meetings", "schedule: " + (result.IsCalendarConnected ? $"{result.Events.Count} event(s)" : "no calendar connected"));
							UpdateActiveMeeting();
							RaiseChanged();
						}
					}
				});
			});
		}
	}

	private void UpdateActiveMeeting()
	{
		bool canPresent = HasCompletedOnboarding && CanAdvancePastPermissions && !IsTutorialEngaged;
		UpcomingCalendarEvent upcomingCalendarEvent = activeMeeting;
		DateTimeOffset now = Now;
		(UpcomingCalendarEvent? Active, MeetingCountdownRules.Change Change) tuple = MeetingCountdownRules.Step(activeMeeting, upcomingEvents, announcedMeetings, now, canPresent);
		UpcomingCalendarEvent item = tuple.Active;
		MeetingCountdownRules.Change item2 = tuple.Change;
		bool flag = (object)upcomingCalendarEvent != null;
		if (flag)
		{
			bool flag2 = ((item2 == MeetingCountdownRules.Change.Cleared || item2 == MeetingCountdownRules.Change.Presented) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			ClickyAnalytics.TrackMeetingCountdownDismissed((now > upcomingCalendarEvent.StartsAt && !upcomingCalendarEvent.IsOngoing(now)) ? "meeting_ended" : "removed_from_schedule", ActionKindWire(upcomingCalendarEvent), SecondsUntilStart(upcomingCalendarEvent, now));
		}
		if (item2 == MeetingCountdownRules.Change.Presented)
		{
			ClickyAnalytics.TrackMeetingCountdownPresented(ActionKindWire(item), SecondsUntilStart(item, now));
		}
		switch (item2)
		{
		case MeetingCountdownRules.Change.Cleared:
			Log.Info("Meetings", "countdown cleared (meeting ended or left the schedule)");
			activeMeeting = null;
			host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MeetingCountdown));
			if ((object)item != null)
			{
				ShowMeeting(item, chime: true);
			}
			break;
		case MeetingCountdownRules.Change.Updated:
			ShowMeeting(item, chime: false);
			break;
		case MeetingCountdownRules.Change.Presented:
			Log.Info("Meetings", $"countdown: \"{item.Title}\" at {item.StartsAt.LocalDateTime:HH:mm} ({item.PrimaryActionKind?.ToString() ?? "no action"})");
			ShowMeeting(item, chime: true);
			break;
		}
	}

	private void ShowMeeting(UpcomingCalendarEvent meeting, bool chime)
	{
		activeMeeting = meeting;
		host.Services.Presenter.ShowLiveEvent(new NotchLiveEvent.MeetingCountdown(meeting.Title, meeting.StartsAt - clockOffset, meeting.PrimaryActionUrl, ActionKindWire(meeting)), null);
		if (chime)
		{
			PlayEarcon("connectionQuestion", 0.85f);
		}
	}

	private static string? ActionKindWire(UpcomingCalendarEvent meeting)
	{
		return meeting.PrimaryActionKind switch
		{
			MeetingActionKind.Join => "join", 
			MeetingActionKind.Open => "open", 
			MeetingActionKind.Directions => "directions", 
			_ => null, 
		};
	}

	private static int SecondsUntilStart(UpcomingCalendarEvent meeting, DateTimeOffset now)
	{
		return (int)(meeting.StartsAt - now).TotalSeconds;
	}

	public void JoinActiveMeeting()
	{
		UpcomingCalendarEvent upcomingCalendarEvent = activeMeeting;
		if ((object)upcomingCalendarEvent != null)
		{
			Uri primaryActionUrl = upcomingCalendarEvent.PrimaryActionUrl;
			if ((object)primaryActionUrl != null)
			{
				ClickyAnalytics.TrackMeetingCountdownActionClicked(ActionKindWire(upcomingCalendarEvent), SecondsUntilStart(upcomingCalendarEvent, Now));
				host.OpenUrl(primaryActionUrl.ToString());
				ClearActiveMeeting();
				return;
			}
		}
		ClearActiveMeeting();
	}

	public void DismissActiveMeeting()
	{
		UpcomingCalendarEvent upcomingCalendarEvent = activeMeeting;
		if ((object)upcomingCalendarEvent != null)
		{
			ClickyAnalytics.TrackMeetingCountdownDismissed("closed_by_user", ActionKindWire(upcomingCalendarEvent), SecondsUntilStart(upcomingCalendarEvent, Now));
		}
		ClearActiveMeeting();
	}

	private void ClearActiveMeeting()
	{
		activeMeeting = null;
		host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MeetingCountdown));
	}

	private void ResetMeetingsForAuthChange()
	{
		meetingsAuthGeneration++;
		if ((object)activeMeeting != null)
		{
			ClearActiveMeeting();
		}
		upcomingEvents = Array.Empty<UpcomingCalendarEvent>();
		meetingsBackendConnected = false;
		meetingsRefreshedAt = (meetingsFailedAt = null);
		announcedMeetings.Clear();
	}

	private void DisposeMeetings()
	{
		meetingsTimer?.Stop();
	}

	private bool InvitationHasSomewhereToGo(MorningSuggestionsNotice notice)
	{
		return notice.IsDebugPreview;
	}

	private void QueueMorningNotice()
	{
		DismissMorningNotice();
		store.Update(delegate(ProactiveStoreData d)
		{
			d.MorningSettledAt = null;
		});
		RebuildMorningInvitationIfNeeded("queued");
	}

	private void RebuildMorningInvitationIfNeeded(string reason)
	{
		MorningSuggestionsInvitationPolicy.RebuildDecision rebuildDecision = MorningSuggestionsInvitationPolicy.Decide((object)morningNotice != null, IsEnabled, store.Read((ProactiveStoreData d) => d.MorningSettledAt), IsSuggestionsPageVisible, cards, GreetingPresentedAtToday, morningSweepStartedAt, Now, IsMorningLaneResting);
		switch (rebuildDecision.Kind)
		{
		case MorningSuggestionsInvitationPolicy.RebuildKind.SettleForToday:
			store.Update(delegate(ProactiveStoreData d)
			{
				d.MorningSettledAt = Now;
			});
			break;
		case MorningSuggestionsInvitationPolicy.RebuildKind.Rebuild:
			morningNotice = new MorningSuggestionsNotice(Guid.NewGuid(), rebuildDecision.CardIds, rebuildDecision.CreatedAt, rebuildDecision.PresentNotBefore, null);
			heldBackReasons.Clear();
			Log.Info("Proactive", $"morning invitation {reason} for {rebuildDecision.CardIds.Count} card(s); presents no earlier than {rebuildDecision.PresentNotBefore.LocalDateTime:HH:mm:ss}");
			ScheduleReadiness(TimeSpan.Zero);
			break;
		}
	}

	private void ScheduleReadiness(TimeSpan delay)
	{
		MorningSuggestionsNotice morningSuggestionsNotice = morningNotice;
		if ((object)morningSuggestionsNotice == null || morningSuggestionsNotice.IsDebugPreview)
		{
			if ((object)readinessTimer == null)
			{
				readinessTimer = CreateReadinessTimer();
			}
			readinessTimer.Stop();
			readinessTimer.Interval = ((delay < TimeSpan.FromMilliseconds(50L, 0L)) ? TimeSpan.FromMilliseconds(50L, 0L) : delay);
			readinessTimer.Start();
		}
	}

	private DispatcherQueueTimer CreateReadinessTimer()
	{
		DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			CheckMorningReadiness();
		};
		return dispatcherQueueTimer;
	}

	private void CheckMorningReadiness()
	{
		MorningSuggestionsNotice notice = morningNotice;
		if ((object)notice == null || IsMorningShowing || !InvitationHasSomewhereToGo(notice))
		{
			return;
		}
		DateTimeOffset now = Now;
		if (notice.RemainingCardCount(cards, now) == 0 && !notice.IsDebugPreview)
		{
			DismissMorningNotice();
			return;
		}
		if (now < notice.PresentNotBefore)
		{
			ScheduleReadiness(notice.PresentNotBefore - now);
			return;
		}
		if (!notice.IsDebugPreview)
		{
			if (!MorningInvitationHasASurface)
			{
				return;
			}
			if (IdleSeconds() > ActiveRecency.TotalSeconds)
			{
				HeldBack("user_away");
				ScheduleReadiness(InvitationRecheck);
				return;
			}
			QuietReason quietReason = QuietContext.Current();
			if (quietReason != 0)
			{
				HeldBack(quietReason switch
				{
					QuietReason.DoNotDisturb => "do_not_disturb", 
					QuietReason.OnCall => "on_call", 
					_ => "quiet_context", 
				});
				ScheduleReadiness(InvitationRecheck);
				return;
			}
			string text = BlockingReason();
			if (text != null)
			{
				HeldBack(text);
				ScheduleReadiness(InvitationRecheck);
				return;
			}
			if (claimedNoticeId != notice.Id)
			{
				if (claimInFlight)
				{
					return;
				}
				claimInFlight = true;
				List<int> ids = (from c in cards
					where notice.CardIds.Contains(c.Id)
					select c.ServerSuggestionId into i
					where i.HasValue
					select i).Cast<int>().ToList();
				Task.Run(async delegate
				{
					bool granted = await client.ClaimMorningInvitationAsync(ids).ConfigureAwait(continueOnCapturedContext: false);
					dq.TryEnqueue(delegate
					{
						claimInFlight = false;
						if (!(morningNotice?.Id != notice.Id))
						{
							if (!granted)
							{
								Log.Info("Proactive", "morning invitation shown on another device; settling here");
								SettleMorningNoticeForToday();
							}
							else
							{
								claimedNoticeId = notice.Id;
								CheckMorningReadiness();
							}
						}
					});
				});
				return;
			}
		}
		PresentMorningNotice(notice);
		if (!IsMorningShowing)
		{
			HeldBack("notch_busy");
			ScheduleReadiness(InvitationRecheck);
		}
	}

	private void HeldBack(string reason)
	{
		if (heldBackReasons.Add(reason))
		{
			Log.Info("Proactive", "morning invitation held: " + reason);
			MorningSuggestionsNotice morningSuggestionsNotice = morningNotice;
			if ((object)morningSuggestionsNotice != null)
			{
				ClickyAnalytics.TrackProductEvent("morning_suggestions_invitation_held_back", null, null, new JsonObject((JsonNodeOptions?)null)
				{
					["reason"] = reason,
					["invitation_id"] = morningSuggestionsNotice.Id.ToString().ToUpperInvariant(),
					["waiting_minutes"] = WaitingMinutes(morningSuggestionsNotice),
					["suggestion_count"] = morningSuggestionsNotice.RemainingCardCount(cards, Now)
				});
			}
		}
	}

	private int WaitingMinutes(MorningSuggestionsNotice notice)
	{
		return Math.Max(0, (int)(Now - notice.PresentNotBefore).TotalMinutes);
	}

	private string? BlockingReason()
	{
		if (IsTutorialEngaged)
		{
			return "onboarding";
		}
		if (!HasCompletedOnboarding || !CanAdvancePastPermissions)
		{
			return "onboarding";
		}
		NotchPresentation presentation = host.Services.Presenter.Presentation;
		if ((presentation is NotchPresentation.TextInput || presentation is NotchPresentation.TextResponse || (presentation is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.AgentPeek)) ? true : false)
		{
			return "notch_expanded";
		}
		if (IsWithinPostOnboardingQuietPeriod)
		{
			return "post_onboarding_quiet_hour";
		}
		if (MorningSuggestionsInvitationPolicy.IsSettled(store.Read((ProactiveStoreData d) => d.MorningSettledAt), Now))
		{
			return "settled";
		}
		return null;
	}

	private void PresentMorningNotice(MorningSuggestionsNotice notice)
	{
		int num = notice.RemainingCardCount(cards, Now);
		if (num == 0)
		{
			return;
		}
		host.Services.Presenter.ShowLiveEvent(new NotchLiveEvent.MorningSuggestions(num), null);
		if (IsMorningShowing && chimedNoticeId != notice.Id)
		{
			chimedNoticeId = notice.Id;
			PlayEarcon("clickyQuestion", 0.28f);
			Log.Info("Proactive", $"morning invitation shown ({num} suggestion(s){((heldBackReasons.Count > 0) ? (", held by " + string.Join(", ", heldBackReasons)) : "")})");
			ClickyAnalytics.TrackProductEvent("morning_suggestions_invitation_action", null, null, new JsonObject((JsonNodeOptions?)null)
			{
				["action"] = "shown",
				["invitation_id"] = notice.Id.ToString().ToUpperInvariant(),
				["suggestion_count"] = num,
				["waiting_minutes"] = WaitingMinutes(notice),
				["held_back_reasons"] = new JsonArray(((IEnumerable<string>)heldBackReasons.Order<string>(StringComparer.Ordinal)).Select((Func<string, JsonNode>)((string r) => JsonValue.Create(r, (JsonNodeOptions?)null))).ToArray())
			});
		}
	}

	private void ReconcileMorningNotice()
	{
		MorningSuggestionsNotice morningSuggestionsNotice = morningNotice;
		if ((object)morningSuggestionsNotice != null && morningSuggestionsNotice.RemainingCardCount(cards, Now) == 0)
		{
			DismissMorningNotice();
			return;
		}
		MorningSuggestionsNotice morningSuggestionsNotice2 = morningNotice;
		if ((object)morningSuggestionsNotice2 != null && IsMorningShowing)
		{
			host.Services.Presenter.ShowLiveEvent(new NotchLiveEvent.MorningSuggestions(morningSuggestionsNotice2.RemainingCardCount(cards, Now)), null);
		}
	}

	private void DismissMorningNotice()
	{
		readinessTimer?.Stop();
		claimInFlight = false;
		morningNotice = null;
		host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MorningSuggestions));
	}

	private void SettleMorningNoticeForToday()
	{
		store.Update(delegate(ProactiveStoreData d)
		{
			d.MorningSettledAt = Now;
		});
		DismissMorningNotice();
	}

	public void OpenSuggestionsFromMorningNotch()
	{
		Log.Info("Proactive", "morning invitation: Show me");
		ClickyAnalytics.TrackProductEvent("morning_suggestions_invitation_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "open" });
		SettleMorningNoticeForToday();
		host.CorePresenter?.SuppressHoverUntilPointerReentry();
	}

	public void PlayMorningPaperFromNotch()
	{
		Log.Info("Proactive", "morning invitation: Read it");
		ClickyAnalytics.TrackProductEvent("morning_suggestions_invitation_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "read_paper" });
		SettleMorningNoticeForToday();
		host.CorePresenter?.SuppressHoverUntilPointerReentry();
		GoodMorningBrief.Shared?.Play();
	}

	public void DismissMorningNoticeFromExplicitClose()
	{
		MorningSuggestionsNotice morningSuggestionsNotice = morningNotice;
		if ((object)morningSuggestionsNotice != null && !morningSuggestionsNotice.IsDebugPreview)
		{
			MorningDismissalHistory updated = MorningSuggestionsInvitationPolicy.RecordingDismissal(store.Read((ProactiveStoreData d) => d.MorningDismissals), Now);
			store.Update(delegate(ProactiveStoreData d)
			{
				d.MorningDismissals = updated;
			});
			DateTimeOffset? restingUntil = updated.RestingUntil;
			if (restingUntil.HasValue)
			{
				DateTimeOffset valueOrDefault = restingUntil.GetValueOrDefault();
				Log.Info("Proactive", $"morning suggestions resting until {valueOrDefault.LocalDateTime:g} after repeated dismissals");
				ClickyAnalytics.TrackProductEvent("morning_suggestions_invitation_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "rest_started" });
			}
		}
		ClickyAnalytics.TrackProductEvent("morning_suggestions_invitation_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "dismiss" });
		Log.Info("Proactive", "morning invitation: dismissed");
		SettleMorningNoticeForToday();
	}

	private void NoteSuggestionApprovedForMorningStreak()
	{
		MorningDismissalHistory history = store.Read((ProactiveStoreData d) => d.MorningDismissals);
		if (history.DismissedDays.Count != 0 || history.RestingUntil.HasValue)
		{
			store.Update(delegate(ProactiveStoreData d)
			{
				d.MorningDismissals = MorningSuggestionsInvitationPolicy.RecordingApproval(history);
			});
		}
	}

	public void PreviewMorningNotch()
	{
		DismissMorningNotice();
		host.CollapseNotchForPreview();
		List<ProactiveSuggestionCard> list = cards.Where((ProactiveSuggestionCard c) => !c.IsExpired(Now)).ToList();
		morningNotice = new MorningSuggestionsNotice(Guid.NewGuid(), list.Select((ProactiveSuggestionCard c) => c.Id).ToHashSet(), Now, Now, (list.Count == 0) ? 3 : list.Count);
		PresentMorningNotice(morningNotice);
	}

	private void PlayEarcon(string name, float volume)
	{
		try
		{
			if (!(Environment.GetEnvironmentVariable("HEYCLICKY_MUTE_AUDIO") == "1"))
			{
				real.Audio.PlayEarcon(name, volume);
			}
		}
		catch (Exception ex)
		{
			Log.Info("Proactive", "earcon " + name + ": " + ex.Message);
		}
	}

	private void UpdateGreetingWatcher()
	{
		if (accountKey == null)
		{
			greetingTimer?.Stop();
			greetingArmTimer?.Stop();
		}
		else
		{
			ScheduleNextGreetingCheck(evaluatingNow: true);
		}
	}

	private void ScheduleNextGreetingCheck(bool evaluatingNow)
	{
		if (accountKey != null)
		{
			if (evaluatingNow)
			{
				EvaluateGreeting();
			}
			if ((object)greetingTimer == null)
			{
				greetingTimer = CreateGreetingTimer();
			}
			greetingTimer.Stop();
			TimeSpan timeSpan = MorningGreetingPolicy.UntilNextCheck(Now.LocalDateTime, HasGreetedToday);
			greetingTimer.Interval = ((timeSpan > TimeSpan.FromHours(6)) ? TimeSpan.FromHours(6) : timeSpan);
			greetingTimer.Start();
		}
	}

	private DispatcherQueueTimer CreateGreetingTimer()
	{
		DispatcherQueueTimer dispatcherQueueTimer = dq.CreateTimer().Rooted();
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			ScheduleNextGreetingCheck(evaluatingNow: true);
		};
		return dispatcherQueueTimer;
	}

	private void EvaluateGreeting()
	{
		DateTime localDateTime = Now.LocalDateTime;
		if (!HasCompletedOnboarding || !CanAdvancePastPermissions || IsTutorialEngaged)
		{
			return;
		}
		DispatcherQueueTimer? dispatcherQueueTimer = greetingArmTimer;
		if (((object)dispatcherQueueTimer != null && dispatcherQueueTimer.IsRunning) || greetingFiring || !MorningGreetingPolicy.IsWithinWindow(localDateTime) || HasGreetedToday)
		{
			return;
		}
		double num = IdleSeconds();
		if (num > MorningGreetingPolicy.ActiveRecency.TotalSeconds)
		{
			Log.Info("Proactive", $"greeting waits: no recent input ({num:0}s)");
			return;
		}
		if (real.Companion.State != 0)
		{
			Log.Info("Proactive", "greeting waits: voice busy");
			return;
		}
		EnsureMorningSweepStarted();
		string text = MorningGreetingPolicy.DayKey(localDateTime);
		if ((object)greetingArmTimer == null)
		{
			greetingArmTimer = dq.CreateTimer();
		}
		greetingArmTimer.Stop();
		greetingArmTimer.IsRepeating = false;
		greetingArmTimer.Interval = MorningGreetingPolicy.ArmDelay;
		greetingArmTimer.Tick -= OnGreetingArmed;
		greetingArmTimer.Tick += OnGreetingArmed;
		armedGreetingDay = text;
		greetingArmTimer.Start();
		Log.Info("Proactive", "greeting armed (10 s)");
	}

	private void OnGreetingArmed(DispatcherQueueTimer sender, object args)
	{
		FireGreetingAsync();
	}

	private async Task FireGreetingAsync()
	{
		greetingFiring = true;
		try
		{
			DateTime localDateTime = Now.LocalDateTime;
			if (!MorningGreetingPolicy.IsWithinWindow(localDateTime) || MorningGreetingPolicy.DayKey(localDateTime) != armedGreetingDay || HasGreetedToday)
			{
				return;
			}
			QuietReason quietReason = await Task.Run((Func<QuietReason>)QuietContext.Current);
			if (quietReason != 0)
			{
				Log.Info("Proactive", $"greeting held: {quietReason}");
				return;
			}
			if (real.Companion.State != 0)
			{
				Log.Info("Proactive", "greeting held: voice busy");
				return;
			}
			DateTimeOffset presentedAt = Now;
			store.Update(delegate(ProactiveStoreData d)
			{
				d.MorningGreetingLastDay = armedGreetingDay;
				d.MorningGreetingLastPresentedAt = presentedAt;
			});
			string text = MorningGreetingPolicy.Pick(random);
			Log.Info("Proactive", "greeting: \"" + text + "\"");
			await host.SayAtCursorAsync(text, streamed: true, MorningGreetingPolicy.PostStreamHold.TotalSeconds);
			if ((object)morningNotice != null && !morningNotice.IsDebugPreview)
			{
				morningNotice = morningNotice with
				{
					PresentNotBefore = presentedAt + MorningSuggestionsInvitationPolicy.DelayAfterGreeting
				};
				ScheduleReadiness(MorningSuggestionsInvitationPolicy.DelayAfterGreeting);
			}
		}
		finally
		{
			greetingFiring = false;
		}
	}

	private void UpdateNotificationPolling()
	{
		if (accountKey == null || !IsRemoteTasksEnabled)
		{
			StopNotifications(clear: true);
			return;
		}
		if ((object)notificationsTimer == null)
		{
			notificationsTimer = dq.CreateTimer();
			notificationsTimer.Interval = AgentBackendClient.PollInterval;
			notificationsTimer.Tick += delegate
			{
				PollNotificationsNow();
			};
		}
		if (!notificationsTimer.IsRunning)
		{
			notificationsTimer.Start();
			PollNotificationsNow();
		}
	}

	private void StopNotifications(bool clear)
	{
		notificationsTimer?.Stop();
		if (clear && notifications.Count > 0)
		{
			notifications = Array.Empty<AgentNotification>();
			this.NotificationsChanged?.Invoke();
		}
	}

	public void PollNotificationsNow()
	{
		if (accountKey == null || !IsRemoteTasksEnabled || notificationsPollInFlight)
		{
			return;
		}
		notificationsPollInFlight = true;
		Task.Run(async delegate
		{
			IReadOnlyList<AgentNotification> fetched = null;
			try
			{
				fetched = await backend.FetchUnreadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Info("Notifications", "poll failed: " + ex.Message);
			}
			dq.TryEnqueue(delegate
			{
				notificationsPollInFlight = false;
				if (fetched != null)
				{
					bool num = fetched.Count != notifications.Count || fetched.Zip(notifications).Any(((AgentNotification First, AgentNotification Second) p) => p.First.Id != p.Second.Id);
					notifications = fetched;
					if (num)
					{
						Log.Info("Notifications", $"{fetched.Count} unread notification(s)");
						this.NotificationsChanged?.Invoke();
					}
				}
			});
		});
	}

	public void MarkNotificationRead(AgentNotification n)
	{
		notifications = notifications.Where((AgentNotification x) => x.Id != n.Id).ToList();
		this.NotificationsChanged?.Invoke();
		Task.Run(async delegate
		{
			try
			{
				await backend.MarkReadAsync(n.Id).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Info("Notifications", "mark read failed: " + ex.Message);
				dq.TryEnqueue(PollNotificationsNow);
			}
		});
	}

	public void MarkAllNotificationsRead()
	{
		if (notifications.Count == 0)
		{
			return;
		}
		notifications = Array.Empty<AgentNotification>();
		this.NotificationsChanged?.Invoke();
		Task.Run(async delegate
		{
			try
			{
				await backend.MarkAllReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Info("Notifications", "read-all failed: " + ex.Message);
				dq.TryEnqueue(PollNotificationsNow);
			}
		});
	}

	private async Task<string> RemoteTaskContextAsync(AgentNotification n)
	{
		JsonObject jsonObject;
		if (n.SourceType == "cron")
		{
			string sourceId = n.SourceId;
			if (sourceId != null)
			{
				jsonObject = await backend.FetchCronDetailAsync(sourceId).ConfigureAwait(continueOnCapturedContext: false);
				goto IL_00b9;
			}
		}
		jsonObject = null;
		goto IL_00b9;
		IL_00b9:
		JsonObject detail = jsonObject;
		return AgentBackendClient.BuildContext(n, detail);
	}

	public void FixRemoteTask(AgentNotification n)
	{
		if (!IsRemoteTasksEnabled)
		{
			return;
		}
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				string prompt = AgentBackendClient.ComposePrompt(await RemoteTaskContextAsync(n).ConfigureAwait(continueOnCapturedContext: false), "Fix this remote task. Use the run error, logs, result, current source, and HeyClicky scheduled cron APIs to update the task so the next run succeeds. Show the proposed source/scope changes before deploying if a destructive or permission-expanding change is needed.");
				string text = await real.Agents.LaunchAsync(new AgentLaunchRequest
				{
					Prompt = prompt,
					LaunchContext = "The user tapped Fix the Task with HeyClicky Agent on a failed Remote Task notification, so HeyClicky is launching a Codex agent with the task source, run logs, and failure result.",
					LaunchSource = "suggested_action",
					IsGeneratedTaskBrief = true
				}).ConfigureAwait(continueOnCapturedContext: false);
				Log.Info("Notifications", "fix launched for \"" + n.Title + "\" as " + text);
			}
			catch (Exception ex)
			{
				Log.Warn("Notifications", "fix launch failed: " + ex.Message);
			}
		});
	}

	public void StartRemoteTaskTextModification(AgentNotification n)
	{
		if (!IsRemoteTasksEnabled)
		{
			return;
		}
		Task.Run(async delegate
		{
			string context = await RemoteTaskContextAsync(n).ConfigureAwait(continueOnCapturedContext: false);
			dq.TryEnqueue(delegate
			{
				PendingRemoteTaskContext = context;
				host.OpenRemoteTaskComposer(n.Title);
			});
		});
	}

	public void ToggleRemoteTaskVoiceModification(AgentNotification n)
	{
		if (!IsRemoteTasksEnabled)
		{
			return;
		}
		if (remoteTaskVoice == null)
		{
			remoteTaskVoice = CreateRemoteTaskVoice();
		}
		if (remoteTaskVoice.IsActive)
		{
			remoteTaskVoice.Stop();
		}
		else
		{
			if (!remoteTaskVoice.Start(delegate(string words)
			{
				dq.TryEnqueue(delegate
				{
					SubmitRemoteTaskModification(words);
				});
			}))
			{
				return;
			}
			Log.Info("Notifications", "modify \"" + n.Title + "\" by voice: listening");
			PendingRemoteTaskContext = AgentBackendClient.BuildContext(n, null);
			Task.Run(async delegate
			{
				string context = await RemoteTaskContextAsync(n).ConfigureAwait(continueOnCapturedContext: false);
				dq.TryEnqueue(delegate
				{
					if (PendingRemoteTaskContext != null)
					{
						PendingRemoteTaskContext = context;
					}
				});
			});
		}
	}

	private TapVoiceCapture CreateRemoteTaskVoice()
	{
		TapVoiceCapture tapVoiceCapture = new TapVoiceCapture(real);
		tapVoiceCapture.ActiveChanged += delegate
		{
			dq.TryEnqueue(delegate
			{
				this.NotificationsChanged?.Invoke();
			});
		};
		return tapVoiceCapture;
	}

	public bool CancelRemoteTaskVoice()
	{
		TapVoiceCapture? tapVoiceCapture = remoteTaskVoice;
		if (tapVoiceCapture == null || !tapVoiceCapture.Cancel())
		{
			return false;
		}
		PendingRemoteTaskContext = null;
		return true;
	}

	public bool SubmitRemoteTaskModification(string words)
	{
		string pendingRemoteTaskContext = PendingRemoteTaskContext;
		if (pendingRemoteTaskContext == null || string.IsNullOrWhiteSpace(words))
		{
			return false;
		}
		PendingRemoteTaskContext = null;
		string prompt = AgentBackendClient.ComposePrompt(pendingRemoteTaskContext, words);
		real.Agents.LaunchAsync(new AgentLaunchRequest
		{
			Prompt = prompt,
			LaunchSource = "text",
			IsGeneratedTaskBrief = false
		}).ContinueWith(delegate(Task<string> t)
		{
			Log.Info("Notifications", t.IsFaulted ? ("modification launch failed: " + t.Exception?.GetBaseException().Message) : ("modification launched as " + t.Result));
		});
		return true;
	}

	public void CancelRemoteTaskModification()
	{
		PendingRemoteTaskContext = null;
	}
}
