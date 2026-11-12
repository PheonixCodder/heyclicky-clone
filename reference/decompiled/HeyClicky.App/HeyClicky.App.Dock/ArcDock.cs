using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Jobs;
using HeyClicky.Agents.Routines;
using HeyClicky.App.Browser;
using HeyClicky.App.Drive;
using HeyClicky.App.Ink;
using HeyClicky.App.Interop;
using HeyClicky.App.Notch.Window;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Ink;
using HeyClicky.Core.Telemetry;
using HeyClicky.Drawing.Audio;
using HeyClicky.Platform.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace HeyClicky.App.Dock;

public sealed class ArcDock
{
	private readonly AgentService agents;

	private readonly Func<OneBrainJobReports?> jobReports;

	private readonly DispatcherQueue dq;

	private readonly ArcCardContentSource source;

	private bool isOpen;

	private bool closesInstantly;

	private bool isTourShowcase;

	private bool isTabTucked;

	private bool isNotchExpanded;

	private string? hoveredKey;

	private int? deckFocusIndex;

	private ArcScreenMetrics? metrics;

	private readonly Dictionary<string, DateTimeOffset> closedFromDockAt = new Dictionary<string, DateTimeOffset>();

	private volatile string? voiceFollowUpKey;

	private (string Key, DateTimeOffset At, AgentFocusPlace Place)? oneBrainFocusedWindow;

	private (string Key, DateTimeOffset At, AgentFocusPlace Place)? oneBrainTypedAgentFocus;

	private (string Key, DateTimeOffset At)? windowDirectVoiceKey;

	private ArcRect railFrame;

	private ArcRect cardFrame;

	private readonly Dictionary<string, ArcRect> faceFrames = new Dictionary<string, ArcRect>();

	private readonly List<ArcRect> moreFrames = new List<ArcRect>();

	private volatile string? cardKeyUnderPointer;

	private bool isRunning;

	private DateTimeOffset? leftAt;

	private (double X, double Y)? railOpenedAtPointer;

	private string? deckFocusKeyToRestore;

	private bool isAutoPresentedWindowAwaitingVisit;

	private DateTimeOffset lastStatusTickAt = DateTimeOffset.Now;

	private DateTimeOffset lastSwitchSoundAt = DateTimeOffset.MinValue;

	private DateTimeOffset lastSoundPrepareAt = DateTimeOffset.MinValue;

	private readonly DispatcherQueueTimer refreshDebounce;

	private readonly PointerEvaluationDriver pointerEventDriver;

	private readonly ArcDockView view;

	private readonly DockMembershipWatch membershipWatch = new DockMembershipWatch();

	private (double W, double H)? lastAnnouncedFrontCardSize;

	private const string DockedClickysKey = "clicky.clickyApps.dockedUnpinnedSlugs.v1";

	public const int MaximumFaces = 30;

	private List<string>? dockedClickys;

	private const double NotchReadInterval = 0.1;

	private double lastNotchReadAt;

	private double notchMovingUntil;

	private bool pointerOnNotch;

	private ArcRect? pressedMoreFrame;

	private readonly DeckScrollGestures scrollGestures = new DeckScrollGestures();

	private DateTimeOffset lastScrollEventAt = DateTimeOffset.MinValue;

	private const double ScrollGestureGap = 0.22;

	private volatile int deckWindowCountForArrowKeys;

	private long? arcOpenedAt;

	private string arcOpenedReason = "";

	private int arcOpenedWindowCount;

	private readonly object soundOwner = new object();

	public AgentDriveConsent? DriveConsent { get; set; }

	public Func<string, Task>? SubmitFact { get; set; }

	public ArcDockHost Host { get; }

	public bool ReplacesAgentHud
	{
		get
		{
			if (isRunning)
			{
				return ClickyAppsPreferences.ShowsEdgeShelf;
			}
			return false;
		}
	}

	public bool IsRunning => isRunning;

	public bool IsTabTucked => isTabTucked;

	public bool IsOpen => isOpen;

	public (double W, double H)? TopArcFrontCardSize
	{
		get
		{
			if (isRunning)
			{
				ArcScreenMetrics? arcScreenMetrics = metrics;
				if (arcScreenMetrics.HasValue)
				{
					ArcScreenMetrics valueOrDefault = arcScreenMetrics.GetValueOrDefault();
					if (view.IsShowing)
					{
						if (faceFrames.Count == 0 && DeckKeys().Count == 0)
						{
							return null;
						}
						return TopArcLayout.FrontCardSize(valueOrDefault.PanelWidth, valueOrDefault.PanelHeight);
					}
				}
			}
			return null;
		}
	}

	public bool IsTourShowcase
	{
		get
		{
			return isTourShowcase;
		}
		set
		{
			isTourShowcase = value;
			SyncDeckPointer();
			view.Layout(LayoutReason.Open);
		}
	}

	private List<string> DockedClickys
	{
		get
		{
			List<string> list = dockedClickys;
			if (list == null)
			{
				List<string>? obj = AgentDefaults.Get<List<string>>("clicky.clickyApps.dockedUnpinnedSlugs.v1") ?? new List<string>();
				List<string> list2 = obj;
				dockedClickys = obj;
				list = list2;
			}
			return list;
		}
	}

	private bool ShowsDock
	{
		get
		{
			if (Host.HasCompletedOnboarding())
			{
				return !Host.OnboardingHidesDock();
			}
			return false;
		}
	}

	public nint DeckHwnd => view.DeckHwnd;

	internal ArcScreenMetrics? Metrics => metrics;

	private bool IsGreetingNotchOpen
	{
		get
		{
			if (isNotchExpanded)
			{
				return Host.NotchIsOpenOnHome();
			}
			return false;
		}
	}

	private double CloseDelay
	{
		get
		{
			if (hoveredKey != null)
			{
				return 0.45;
			}
			return 0.3;
		}
	}

	private bool IsWindowHeldOpen
	{
		get
		{
			if (voiceFollowUpKey != null)
			{
				return Host.IsListening();
			}
			return false;
		}
	}

	internal bool IsOpenNow => isOpen;

	public bool IsTabTuckedNow => isTabTucked;

	internal bool ClosesInstantly => closesInstantly;

	internal string? HoveredKey => hoveredKey;

	internal int? FocusIndex => deckFocusIndex;

	private string? FrontWindowKey
	{
		get
		{
			IReadOnlyList<string> readOnlyList = DeckKeys();
			if (readOnlyList.Count != 0)
			{
				return readOnlyList[DeckFocus.Clamped(deckFocusIndex, readOnlyList.Count)];
			}
			return null;
		}
	}

	private bool IsScrollGestureGoingOn => (DateTimeOffset.Now - lastScrollEventAt).TotalSeconds <= 0.22;

	public bool ArrowKeysTurnDeck
	{
		get
		{
			if (isOpen && isNotchExpanded && deckWindowCountForArrowKeys > 1)
			{
				return Host.NotchIsOpenOnHome();
			}
			return false;
		}
	}

	public bool IsLaunchingFromOpenWindow
	{
		get
		{
			if (voiceFollowUpKey == null && cardKeyUnderPointer == null && !HeyClickyWindow.Shared.IsInFront)
			{
				return HeyClickyWindow.Shared.AgentKeyUnderPointer() != null;
			}
			return true;
		}
	}

	public IReadOnlyDictionary<string, ArcCardView> DemoCards => view.Cards;

	public UIElement? DemoDeckRoot => view.DeckRoot;

	public nint DemoDeckHwnd => view.DeckHwnd;

	public int DemoFrontIndex => DeckFocus.Clamped(deckFocusIndex, DeckKeys().Count);

	public bool DemoHoldsOpen { get; set; }

	public (double X, double Y)? DemoPointer { get; set; }

	public bool DemoTakesPointer => view.TakesPointer;

	public IReadOnlyDictionary<string, ArcRect> DemoFaceFrames => faceFrames;

	public ArcRect? DemoNotchSurface => metrics?.NotchSurface;

	public event Action? FrontCardSizeChanged;

	public event Action? ContentChanged;

	public event Action<bool>? PointerOnNotchChanged;

	private void Track(ArcCardContent content, string action, JsonObject? extra = null)
	{
		JsonObject jsonObject = extra ?? new JsonObject((JsonNodeOptions?)null);
		jsonObject["action"] = action;
		if (content.JobId != null)
		{
			ClickyAnalytics.TrackProductEvent("job_action", null, content.ThreadId, jsonObject);
		}
		else
		{
			ClickyAnalytics.TrackProductEvent("clicky_dashboard_action", content.ClickySlug, null, jsonObject);
		}
	}

	public static void TrackWindowAction(string action, ArcCardContent content, JsonObject? extra = null)
	{
		JsonObject jsonObject = WindowProperties(content);
		jsonObject["action"] = action;
		if (extra != null)
		{
			foreach (KeyValuePair<string, JsonNode> item in extra)
			{
				item.Deconstruct(out var key, out var value);
				string propertyName = key;
				jsonObject[propertyName] = value?.DeepClone();
			}
		}
		ClickyAnalytics.TrackProductEvent("arc_dock_action", content.ClickySlug, content.ThreadId, jsonObject);
	}

	public static JsonObject WindowProperties(ArcCardContent content)
	{
		return ArcDockAnalytics.WindowProperties(content.State, content.IsFinalizing, content.JobId != null);
	}

	public void TrackRequestAnswered(string answer, string surface, string railKey, string? taskId)
	{
		TrackRequestAnswered(agents, answer, surface, railKey, taskId);
	}

	public static void TrackRequestAnswered(AgentService agents, string answer, string surface, string railKey, string? taskId)
	{
		string text = ClickyJob.JobIdFromRailKey(railKey);
		string text2 = ((text == null) ? null : agents.Jobs.Job(text)?.ThreadId);
		string text3 = ((taskId == null) ? null : agents.GetTaskDetail(taskId)?.Snapshot.ThreadId);
		ClickyAnalytics.TrackProductEvent("agent_request_answered", (text2 == null) ? railKey : null, text3 ?? text2, ArcDockAnalytics.RequestAnswered(answer, surface, text != null, taskId != null));
	}

	private void MarkWindowRead(string railKey)
	{
		Host.DismissNotchUpdate(railKey);
		string text = ClickyJob.JobIdFromRailKey(railKey);
		if (text != null)
		{
			agents.Jobs.MarkSeen(text);
		}
		else
		{
			Host.MarkClickyRead(railKey);
		}
	}

	private void MarkRead(ArcCardContent content)
	{
		string jobId = content.JobId;
		if (jobId != null)
		{
			agents.Jobs.MarkSeen(jobId);
			return;
		}
		string clickySlug = content.ClickySlug;
		if (clickySlug != null)
		{
			Host.MarkClickyRead(clickySlug);
		}
	}

	public void OpenInWindow(string key)
	{
		Host.OpenAgentInWindow(key);
	}

	public void MoveToEarlier(string key)
	{
		TakeOffDock(key);
	}

	private void TakeOffDock(string key)
	{
		if (hoveredKey == key)
		{
			hoveredKey = null;
		}
		string text = ClickyJob.JobIdFromRailKey(key);
		if (text != null)
		{
			agents.Jobs.MarkSeen(text);
			agents.DismissJob(text);
		}
		else
		{
			Host.MarkClickyRead(key);
			RemoveDockedClicky(key);
			closedFromDockAt[key] = DateTimeOffset.Now;
		}
		Host.DismissNotchUpdate(key);
		Refresh();
	}

	public void RemoveFromDock(string key)
	{
		string text = DockOrder.NextFrontKeyAfterRemoving(DeckKeys(), key);
		TakeOffDock(key);
		List<string> list = DeckKeys().ToList();
		if (text != null)
		{
			int num = list.IndexOf(text);
			if (num >= 0)
			{
				deckFocusIndex = num;
				view.Layout(LayoutReason.Items);
				Open(text, "the window beside a removed one");
				return;
			}
		}
		Close(immediately: false, "the dock's last window was removed");
	}

	public void StopWork(ArcCardContent content)
	{
		string jobId = content.JobId;
		if (jobId != null)
		{
			agents.StopJobAsync(jobId);
			return;
		}
		string taskId = content.TaskId;
		if (taskId != null)
		{
			ClickyAnalytics.TrackProductEvent("agent_task_control_clicked", content.ClickySlug, content.ThreadId, new JsonObject((JsonNodeOptions?)null) { ["action"] = "stop" });
			agents.StopAsync(taskId);
		}
	}

	public bool CanShowFullResponse(ArcCardContent content)
	{
		return Host.HasLatestReply(content.JobId, content.ClickySlug);
	}

	public void OpenFullResponse(ArcCardContent content)
	{
		string jobId = content.JobId;
		if (jobId != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(jobId);
			if ((object)clickyJob != null && clickyJob.IsWaitingToStart)
			{
				StartWaitingJob(clickyJob);
				return;
			}
		}
		MarkRead(content);
		if (Host.HasLatestReply(content.JobId, content.ClickySlug))
		{
			Host.ShowFullReply(content.JobId, content.ClickySlug);
		}
		else
		{
			OpenInChat(content);
		}
	}

	private void StartWaitingJob(ClickyJob job)
	{
		Sounds.Play("clicky-text-send", 0.35);
		if (!Host.StartWaitingJob(job))
		{
			agents.Jobs.Dismiss(job.Id);
			return;
		}
		ClickyJob clickyJob = agents.Jobs.Job(job.Id);
		if ((object)clickyJob != null && clickyJob.IsWaitingToStart)
		{
			agents.Jobs.Dismiss(job.Id);
		}
		ClickyAnalytics.TrackProductEvent("job_action", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "started",
			["from"] = "waiting_window"
		});
	}

	public void OpenInChat(ArcCardContent content)
	{
		MarkRead(content);
		Host.ShowChat(content.JobId, content.ClickySlug);
	}

	public void OpenCardFile(string entry, ArcCardContent content)
	{
		MarkRead(content);
		string clickySlug = content.ClickySlug;
		if (clickySlug != null)
		{
			ClickyAnalytics.TrackProductEvent("clicky_dashboard_action", clickySlug, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "latest_file_opened" });
		}
		string text = DockFiles.Resolve(entry, content.WorkspacePath);
		if (!Host.OpensInNotchDocumentViewer(text))
		{
			Host.DismissNotch();
		}
		if (!Host.OpenInNotchViewer(text, content.ClickySlug, content.ThreadId))
		{
			DockFiles.Open(entry, content.WorkspacePath);
		}
	}

	public void OpenResultCardTarget(string target, ArcCardContent content)
	{
		MarkRead(content);
		if (target.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			DockFiles.Open(target, null);
		}
		else
		{
			OpenCardFile(target, content);
		}
	}

	public void AnswerResultCardQuestion(ArcCardContent content, ClickyAnswer.Choice choice)
	{
		Sounds.Play("clicky-text-send", 0.2);
		string reply = choice.Reply;
		string text = "The user answered your question by tapping \"" + reply + "\" on the job's card.";
		string file = choice.File;
		if (file != null)
		{
			text = text + " The file they picked: " + file;
		}
		string jobId = content.JobId;
		if (jobId != null)
		{
			agents.Jobs.MarkSeen(jobId);
		}
		string clickySlug = content.ClickySlug;
		if (clickySlug != null)
		{
			agents.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = reply,
				ClickySlug = clickySlug,
				LaunchContext = text,
				LaunchSource = "text",
				IsHudFollowUp = true
			});
			ClickyAnalytics.TrackProductEvent("clicky_dashboard_action", clickySlug, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "result_card_answer" });
			return;
		}
		string threadId = content.ThreadId;
		if (threadId != null)
		{
			agents.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = reply,
				JobId = content.JobId,
				TargetThreadId = threadId,
				LaunchContext = text,
				LaunchSource = "text",
				IsHudFollowUp = true
			});
			ClickyAnalytics.TrackProductEvent("job_action", null, threadId, new JsonObject((JsonNodeOptions?)null) { ["action"] = "result_card_answer" });
		}
	}

	public void CopyDraft(ArcCardContent content, string text)
	{
		DataPackage dataPackage = new DataPackage();
		dataPackage.SetText(text);
		try
		{
			Clipboard.SetContent(dataPackage);
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "copy draft: " + ex.Message);
		}
		Sounds.Play("clicky-text-send", 0.2);
		string jobId = content.JobId;
		if (jobId != null)
		{
			agents.Jobs.MarkSeen(jobId);
		}
	}

	public string? ResultCardRoutineId(ArcCardContent content)
	{
		string clickySlug = content.ClickySlug;
		if (clickySlug == null)
		{
			return null;
		}
		return agents.Routines.ForClicky(clickySlug).FirstOrDefault((ClickyRoutine r) => !r.IsPaused)?.Id;
	}

	public void RunRoutineNow(string routineId)
	{
		agents.Routines.RunNowAsync(routineId);
	}

	public string? ResultCardWhenLine(ArcCardContent content, ClickyAnswer answer)
	{
		if (!answer.IsQuiet && answer.NumbersLook != ClickyAnswer.NumbersLookKind.Changes)
		{
			return null;
		}
		List<string> list = new List<string>();
		string jobId = content.JobId;
		DateTimeOffset? obj;
		if (jobId != null)
		{
			obj = agents.Jobs.Job(jobId)?.FinishedAt;
		}
		else
		{
			string clickySlug = content.ClickySlug;
			obj = ((clickySlug != null) ? agents.LiveStatusForClicky(clickySlug).FinishedAt : ((DateTimeOffset?)null));
		}
		DateTimeOffset? dateTimeOffset = obj;
		DateTimeOffset now = DateTimeOffset.Now;
		if (answer.IsQuiet && dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			list.Add("Checked " + ResultCardLayouts.Moment(valueOrDefault, now));
		}
		string clickySlug2 = content.ClickySlug;
		ClickyRoutine clickyRoutine = ((clickySlug2 != null) ? agents.Routines.ForClicky(clickySlug2).FirstOrDefault((ClickyRoutine r) => !r.IsPaused) : null);
		if (clickyRoutine != null)
		{
			ClickyRoutine clickyRoutine2 = clickyRoutine;
			if (clickyRoutine2.NextRunAt > now)
			{
				list.Add("next check " + ResultCardLayouts.Moment(clickyRoutine2.NextRunAt, now));
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		string text = string.Join(" · ", list);
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text[0]));
		string text2 = text;
		return string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
	}

	public void OpenReadyOutput(string railKey, CardReadyOutput output)
	{
		string text = ClickyJob.JobIdFromRailKey(railKey);
		string arg = ((text == null) ? railKey : null);
		string cardEntry = output.CardEntry;
		if (cardEntry == null)
		{
			MarkWindowRead(railKey);
			if (Host.HasLatestReply(text, arg))
			{
				Host.ShowFullReply(text, arg);
			}
			else
			{
				Host.ShowChat(text, arg);
			}
			return;
		}
		MarkWindowRead(railKey);
		string workspace = ((text == null) ? agents.WorkspacePath(railKey) : agents.Jobs.Job(text)?.FolderPath);
		string text2 = DockFiles.Resolve(cardEntry, workspace);
		if (!Host.OpensInNotchDocumentViewer(text2))
		{
			Host.DismissNotch();
		}
		if (!Host.OpenInNotchViewer(text2, arg, output.ThreadId))
		{
			DockFiles.Open(cardEntry, workspace);
		}
	}

	public void ShowJobThroughBrain(string railKey)
	{
		string text = ClickyJob.JobIdFromRailKey(railKey);
		if (text == null)
		{
			return;
		}
		ClickyJob clickyJob = agents.Jobs.Job(text);
		if ((object)clickyJob != null)
		{
			MarkWindowRead(railKey);
			Close(immediately: true, "Show me on a finished job");
			agents.Jobs.MarkSeen(text);
			string brief = JobResultBrief(clickyJob);
			ClickyAnalytics.TrackProductEvent("job_action", null, clickyJob.ThreadId, new JsonObject((JsonNodeOptions?)null) { ["action"] = "show_me" });
			DockTimer.After(0.35, delegate
			{
				SubmitFact?.Invoke(brief);
			});
		}
	}

	public static string JobResultBrief(ClickyJob job)
	{
		List<string> list = new List<string>
		{
			"[Job result] The user pressed Show me on the job \"" + job.Title + "\". Present what it found: the answer in your first line, then draw it at the size it deserves (one card for a number or a pick, a card with rows or a small chart for a comparison or a plan, a timeline for a schedule). Use only what's below and never add facts. A few beats, then end by asking what they want to do with it.",
			"Asked: " + (job.Runs?.LastOrDefault()?.Ask ?? job.Prompt)
		};
		string summary = job.Summary;
		if (summary != null && summary.Length > 0)
		{
			list.Add("Its answer in one line: " + summary);
		}
		string text = job.ReplyText?.Trim();
		if (text != null && text.Length > 0)
		{
			list.Add("Its reply:\n" + ((text.Length > 1200) ? text.Substring(0, 1200) : text));
		}
		try
		{
			string path = ArcLiveNotes.NotePath(job.FolderPath);
			if (File.Exists(path))
			{
				string text2 = File.ReadAllText(path);
				if (text2 != null && text2.Trim().Length > 0)
				{
					list.Add("Its note (the page the user sees):\n" + ((text2.Length > 4000) ? text2.Substring(0, 4000) : text2));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "show me note: " + ex.Message);
		}
		if (job.ArtifactEntries.Count > 0)
		{
			list.Add("What it made (open one with open_artifact when they ask): " + string.Join(", ", job.ArtifactEntries.Take(6)));
		}
		return string.Join("\n", list);
	}

	public void LetClickyDrive(string taskId)
	{
		AgentDriveConsent driveConsent = DriveConsent;
		if (driveConsent != null)
		{
			driveConsent.LetClickyDriveForAgent(taskId);
		}
		else
		{
			Log.Warn("dock", "Let Clicky drive: Clicky drive isn't attached");
		}
	}

	public void DeclineComputerUse(string taskId)
	{
		AgentDriveConsent driveConsent = DriveConsent;
		if (driveConsent != null)
		{
			driveConsent.DeclineComputerUse(taskId);
		}
		else
		{
			agents.SetPendingComputerUseRequest(taskId, null);
		}
	}

	public void Retry(ArcCardContent content)
	{
		string taskId = content.TaskId;
		if (taskId != null && (agents.CanRetryAfterFailure(taskId) || agents.CanRetryAfterInterruption(taskId)))
		{
			AgentTaskDetail taskDetail = agents.GetTaskDetail(taskId);
			ClickyAnalytics.TrackProductEvent("agent_task_control_clicked", taskDetail?.ClickySlug, taskDetail?.Snapshot.ThreadId, new JsonObject((JsonNodeOptions?)null) { ["action"] = "retry" });
			agents.RetryFailedTask(taskId);
		}
		else
		{
			RetryWithoutLiveTask(content.RailKey);
		}
	}

	private void RetryWithoutLiveTask(string railKey)
	{
		string text = ClickyJob.JobIdFromRailKey(railKey);
		if (text != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(text);
			if ((object)clickyJob != null)
			{
				string threadId = clickyJob.ThreadId;
				if (threadId != null)
				{
					agents.LaunchAsync(new AgentLaunchRequest
					{
						Prompt = "Retry the task you were working on and finish it.",
						JobId = text,
						TargetThreadId = threadId,
						LaunchContext = "The user tapped Retry on this task's window on the edge of their screen. The previous turn failed or was stopped partway through, so first check what this thread has already completed, keep that finished work, and continue from where it stopped instead of starting over.",
						LaunchSource = "retry",
						IsHudFollowUp = true
					});
					ClickyAnalytics.TrackProductEvent("agent_task_control_clicked", null, threadId, new JsonObject((JsonNodeOptions?)null)
					{
						["action"] = "retry",
						["from"] = "edge_dock_saved"
					});
				}
			}
		}
		else if ((object)agents.FindClicky(railKey) != null)
		{
			agents.LaunchAsync(new AgentLaunchRequest
			{
				Prompt = "Retry the task you were working on and finish it.",
				ClickySlug = railKey,
				LaunchContext = "The user tapped Retry on this task's window on the edge of their screen. The previous turn failed or was stopped partway through, so first check what this thread has already completed, keep that finished work, and continue from where it stopped instead of starting over.",
				LaunchSource = "retry",
				IsHudFollowUp = true
			});
			ClickyAnalytics.TrackProductEvent("agent_task_control_clicked", railKey, null, new JsonObject((JsonNodeOptions?)null)
			{
				["action"] = "retry",
				["from"] = "edge_dock_saved"
			});
		}
	}

	public void ChooseChrome(ChromeSetupFlow.Choice choice, ArcCardContent content)
	{
		ChromeSetupFlow.Shared.Choose(choice, new ChromeSetupFlow.Target(content.JobId, content.ThreadId));
	}

	public void ContinueAfterUserStep(string jobId)
	{
		agents.ContinueJobAfterUserStep(jobId);
	}

	public void ApproveExtraUsage(string taskId, bool always)
	{
		ApproveExtraUsageAsync(taskId, always);
	}

	private async Task ApproveExtraUsageAsync(string taskId, bool always)
	{
		try
		{
			AgentService agentService = agents;
			bool remembersAlways = always;
			await agentService.ApproveExtraUsageAsync(taskId, asManyAsNeeded: true, default(CancellationToken), remembersAlways);
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "extra usage approval: " + ex.Message);
		}
	}

	public ArcDock(AgentService agents, ArcDockHost host, Func<OneBrainJobReports?> jobReports, bool excludeFromCapture)
	{
		this.agents = agents;
		Host = host;
		this.jobReports = jobReports;
		dq = DispatcherQueue.GetForCurrentThread();
		source = new ArcCardContentSource(agents, jobReports, host);
		view = new ArcDockView(this, excludeFromCapture);
		pointerEventDriver = new PointerEvaluationDriver(60.0, 1.0, delegate
		{
			Tick();
			ScheduleNextTimedTick();
		});
		refreshDebounce = dq.CreateTimer();
		refreshDebounce.Interval = TimeSpan.FromMilliseconds(250L, 0L);
		refreshDebounce.IsRepeating = false;
		refreshDebounce.Tick += delegate
		{
			Refresh();
		};
		FileCardSound.PlaysSounds = () => Host.PlaysSounds();
	}

	public void Start()
	{
		if (isRunning)
		{
			return;
		}
		isRunning = true;
		agents.TasksChanged += delegate
		{
			RefreshSoon();
		};
		agents.TaskEvent += delegate(object? _, AgentTaskEvent e)
		{
			AgentTaskEventKind kind = e.Kind;
			if (((uint)(kind - 8) <= 3u || kind == AgentTaskEventKind.PlanUpdated) ? true : false)
			{
				RefreshSoon();
			}
		};
		agents.Jobs.Changed += delegate
		{
			RefreshSoon();
		};
		agents.LiveSteps.Changed += delegate
		{
			RefreshSoon();
		};
		agents.ResultCardShown += delegate
		{
			RefreshSoon();
		};
		agents.ClickyApps.Changed += delegate
		{
			RefreshSoon();
		};
		OneBrainJobReports oneBrainJobReports = jobReports();
		if (oneBrainJobReports != null)
		{
			oneBrainJobReports.PresentingChanged += RefreshSoon;
		}
		ClickyInkNoteStore.Shared.RevisionChanged += RefreshSoon;
		membershipWatch.Note(ShowsDock, Host.LiveSuggestionCardIds());
		Refresh();
		Log.Info("dock", "the agent dock is running (it stands in for the HUD)");
	}

	private void RefreshSoon()
	{
		dq.TryEnqueue(delegate
		{
			refreshDebounce.Stop();
			refreshDebounce.Start();
		});
	}

	public void NoteMembershipInputsChanged()
	{
		dq.TryEnqueue(delegate
		{
			if (isRunning && membershipWatch.Note(ShowsDock, Host.LiveSuggestionCardIds()))
			{
				RefreshSoon();
			}
		});
	}

	public void NotchChanged()
	{
		dq.TryEnqueue(delegate
		{
			notchMovingUntil = PointerEvaluationDriver.Now + 0.6;
			UpdateNotchState();
			Tick();
			ScheduleNextTimedTick();
		});
	}

	public void NotchTabChanged(bool isHome, string? tab = null)
	{
		if (!isHome && isOpen)
		{
			deckFocusKeyToRestore = FrontWindowKey;
			Close(immediately: true, (tab == null) ? "notch tab changed" : ("notch tab → " + tab));
		}
		if (isHome)
		{
			UpdateNotchState();
		}
		Tick();
	}

	public void CloseTopArc(string reason = "the notch's gear")
	{
		if (isOpen)
		{
			Close(immediately: true, reason);
		}
	}

	private void NotifyIfFrontCardSizeChanged()
	{
		(double, double)? topArcFrontCardSize = TopArcFrontCardSize;
		(double, double)? tuple = topArcFrontCardSize;
		(double, double)? tuple2 = lastAnnouncedFrontCardSize;
		bool hasValue = tuple.HasValue;
		if (hasValue == tuple2.HasValue)
		{
			if (!hasValue)
			{
				return;
			}
			(double, double) valueOrDefault = tuple.GetValueOrDefault();
			(double, double) valueOrDefault2 = tuple2.GetValueOrDefault();
			if (valueOrDefault.Item1 == valueOrDefault2.Item1 && valueOrDefault.Item2 == valueOrDefault2.Item2)
			{
				return;
			}
		}
		lastAnnouncedFrontCardSize = topArcFrontCardSize;
		this.FrontCardSizeChanged?.Invoke();
	}

	private void AddDockedClicky(string slug)
	{
		if (!DockedClickys.Contains(slug))
		{
			DockedClickys.Add(slug);
			AgentDefaults.Set("clicky.clickyApps.dockedUnpinnedSlugs.v1", DockedClickys);
		}
	}

	public void DockLegacyClickys(IEnumerable<string> slugs)
	{
		foreach (string slug in slugs)
		{
			AddDockedClicky(slug);
		}
		Refresh();
	}

	private void RemoveDockedClicky(string slug)
	{
		if (DockedClickys.Remove(slug))
		{
			AgentDefaults.Set("clicky.clickyApps.dockedUnpinnedSlugs.v1", DockedClickys);
		}
	}

	public void RemoveAllForSignOut()
	{
		dockedClickys = new List<string>();
		AgentDefaults.Remove("clicky.clickyApps.dockedUnpinnedSlugs.v1");
		closedFromDockAt.Clear();
		Refresh();
	}

	public IReadOnlyList<string> ShelfClickys()
	{
		if (!ShowsDock)
		{
			return Array.Empty<string>();
		}
		HashSet<string> hashSet = new HashSet<string>();
		List<string> list = new List<string>();
		foreach (AgentTaskSnapshot task in agents.Tasks)
		{
			AgentTaskDetail taskDetail = agents.GetTaskDetail(task.TaskId);
			if ((object)taskDetail == null || taskDetail.IsDemo)
			{
				continue;
			}
			string clickySlug = taskDetail.ClickySlug;
			if (clickySlug != null && taskDetail.JobId == null && !hashSet.Contains(clickySlug))
			{
				bool flag = agents.PendingComputerUseRequest(task.TaskId) != null || taskDetail.RequiresExtraUsageApproval;
				AgentTaskStatus status = task.Status;
				bool flag2 = (uint)status <= 1u;
				bool flag3 = flag2 || flag;
				if ((!closedFromDockAt.TryGetValue(clickySlug, out var value) || flag || !(task.StartedAt < value)) && flag3 && (object)agents.FindClicky(clickySlug) != null)
				{
					hashSet.Add(clickySlug);
					list.Add(clickySlug);
				}
			}
		}
		foreach (string item in Host.UnreadClickySlugs())
		{
			if (!hashSet.Contains(item) && (object)agents.FindClicky(item) != null)
			{
				hashSet.Add(item);
				list.Add(item);
			}
		}
		foreach (string item2 in list)
		{
			AddDockedClicky(item2);
		}
		foreach (string item3 in DockedClickys.ToList())
		{
			if (!hashSet.Contains(item3) && (object)agents.FindClicky(item3) != null)
			{
				hashSet.Add(item3);
				list.Add(item3);
			}
		}
		return list.Take(30).ToList();
	}

	public IReadOnlyList<ClickyJob> ShelfJobs()
	{
		if (!ClickyJobsPreferences.IsEnabled || !ShowsDock)
		{
			return Array.Empty<ClickyJob>();
		}
		ISet<Guid> liveCards = Host.LiveSuggestionCardIds();
		return agents.Jobs.RailJobs.Where(delegate(ClickyJob job)
		{
			if (job.IsWaitingToStart)
			{
				Guid? suggestionCardId = job.SuggestionCardId;
				if (suggestionCardId.HasValue)
				{
					Guid valueOrDefault = suggestionCardId.GetValueOrDefault();
					return liveCards?.Contains(valueOrDefault) ?? true;
				}
			}
			return true;
		}).ToList();
	}

	public IReadOnlyList<DockWindowKey> DeckWindows()
	{
		return DockOrder.DeckOrder(ShelfClickys(), ShelfJobs(), ArcRecency);
	}

	private IReadOnlyList<string> DeckKeys()
	{
		return (from w in DeckWindows()
			select w.Key).ToList();
	}

	public IReadOnlyList<string> DockKeys()
	{
		return DeckKeys();
	}

	public DateTimeOffset LatestActivity(string key)
	{
		return ArcRecency(key).Date;
	}

	private DockRecency ArcRecency(string key)
	{
		DateTimeOffset date = EdgeDockRecency(key).Date;
		string text = ClickyJob.JobIdFromRailKey(key);
		string arg = ((text != null) ? ("job:" + text) : ("clicky:" + key));
		DateTimeOffset dateTimeOffset = Host.LatestChatEventAt(arg) ?? DateTimeOffset.MinValue;
		return new DockRecency(IsActive: false, (date > dateTimeOffset) ? date : dateTimeOffset);
	}

	private DockRecency EdgeDockRecency(string key)
	{
		string text = ClickyJob.JobIdFromRailKey(key);
		if (text != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(text);
			if ((object)clickyJob == null)
			{
				return new DockRecency(IsActive: false, DateTimeOffset.MinValue);
			}
			string text2 = agents.LiveTaskId(clickyJob.Id);
			AgentTaskDetail agentTaskDetail = ((text2 != null) ? agents.GetTaskDetail(text2) : null);
			ClickyJobRailPhase clickyJobRailPhase = agents.RailPhase(clickyJob);
			if ((uint)clickyJobRailPhase <= 2u)
			{
				return new DockRecency(IsActive: true, agentTaskDetail?.Snapshot.StartedAt ?? clickyJob.CreatedAt);
			}
			return new DockRecency(IsActive: false, clickyJob.FinishedAt ?? clickyJob.CreatedAt);
		}
		string text3 = source.ClickyTaskId(key);
		int num;
		DateTimeOffset date;
		if (text3 != null)
		{
			AgentTaskDetail taskDetail = agents.GetTaskDetail(text3);
			if ((object)taskDetail != null)
			{
				AgentTaskStatus status = taskDetail.Snapshot.Status;
				bool flag = (uint)status <= 1u;
				if (!flag && agents.PendingComputerUseRequest(text3) == null)
				{
					num = (taskDetail.RequiresExtraUsageApproval ? 1 : 0);
					if (num == 0)
					{
						date = agents.LiveStatusForClicky(key).FinishedAt ?? taskDetail.Snapshot.StartedAt;
						goto IL_0156;
					}
				}
				else
				{
					num = 1;
				}
				date = taskDetail.Snapshot.StartedAt;
				goto IL_0156;
			}
		}
		return new DockRecency(IsActive: false, (from w in agents.ClickyApps.Works(key)
			orderby w.FinishedAt descending
			select w).FirstOrDefault()?.FinishedAt ?? DateTimeOffset.MinValue);
		IL_0156:
		return new DockRecency((byte)num != 0, date);
	}

	private (int Working, int NeedsYou, bool Failed) Counts()
	{
		int num = 0;
		int num2 = 0;
		bool item = false;
		OneBrainJobReports oneBrainJobReports = jobReports();
		foreach (string item2 in ShelfClickys())
		{
			switch (agents.LiveStatusForClicky(item2).Phase)
			{
			case ClickyAppLiveStatus.Phases.Working:
				num++;
				break;
			case ClickyAppLiveStatus.Phases.Finished:
				if (oneBrainJobReports != null && oneBrainJobReports.IsFinalizingResult(item2))
				{
					num++;
				}
				break;
			case ClickyAppLiveStatus.Phases.NeedsYou:
				num2++;
				break;
			case ClickyAppLiveStatus.Phases.Failed:
				item = true;
				break;
			}
		}
		foreach (ClickyJob item3 in ShelfJobs())
		{
			switch (agents.RailPhase(item3))
			{
			case ClickyJobRailPhase.Queued:
			case ClickyJobRailPhase.Working:
				num++;
				break;
			case ClickyJobRailPhase.Done:
				if (oneBrainJobReports != null && oneBrainJobReports.IsFinalizingResult(item3))
				{
					num++;
				}
				break;
			case ClickyJobRailPhase.NeedsYou:
				num2++;
				break;
			case ClickyJobRailPhase.Failed:
				item = true;
				break;
			}
		}
		return (Working: num, NeedsYou: num2, Failed: item);
	}

	public string StatusLine()
	{
		var (workingCount, needsYouCount, _) = Counts();
		return TopArcGreetingLayout.StatusLine(workingCount, needsYouCount, Host.VoiceHoldDescription());
	}

	public (int WorkingCount, string AlertPhase) RestingMark()
	{
		var (item, num, flag) = Counts();
		return (WorkingCount: item, AlertPhase: (num > 0) ? "needsYou" : (flag ? "failed" : "idle"));
	}

	public ArcCardContent Content(DockWindowKey window)
	{
		ClickyJob job = window.Job;
		if ((object)job == null)
		{
			return source.ForClicky(window.ClickySlug);
		}
		return source.ForJob(agents.Jobs.Job(job.Id) ?? job);
	}

	public ArcCardContent? ContentForKey(string railKey)
	{
		string text = ClickyJob.JobIdFromRailKey(railKey);
		if (text != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(text);
			if ((object)clickyJob == null)
			{
				return null;
			}
			return source.ForJob(clickyJob);
		}
		if ((object)agents.FindClicky(railKey) != null)
		{
			return source.ForClicky(railKey);
		}
		return null;
	}

	public void KeepAboveNotch()
	{
		view.KeepZOrder();
	}

	public void RebuildAfterLayoutCycle()
	{
		view.RebuildCards();
		Refresh();
	}

	public void Refresh()
	{
		if (!isRunning)
		{
			return;
		}
		this.ContentChanged?.Invoke();
		bool showsEdgeShelf = ClickyAppsPreferences.ShowsEdgeShelf;
		bool showsDock = ShowsDock;
		bool flag = Host.SuppressesVisualSurfaces();
		if (!showsEdgeShelf || !showsDock || flag)
		{
			if (view.IsShowing)
			{
				HidePanel((!showsEdgeShelf) ? "dock switched off" : ((!showsDock) ? "dock not shown (setup not done or signed out)" : "HeyClicky's visuals suppressed"));
			}
			return;
		}
		ShowPanel();
		IReadOnlyList<DockWindowKey> readOnlyList = DeckWindows();
		deckWindowCountForArrowKeys = readOnlyList.Count;
		view.Sync(readOnlyList);
		NotifyIfFrontCardSizeChanged();
		pointerEventDriver.EvaluateSoon();
	}

	private void ShowPanel()
	{
		UpdateNotchState();
		view.Show();
		pointerEventDriver.Start();
	}

	private void HidePanel(string reason)
	{
		if (isOpen)
		{
			LogArc("close", "panel hidden: " + reason);
		}
		Host.SetNotchHeldOpenByArc(obj: false);
		pointerEventDriver.Stop();
		isOpen = false;
		hoveredKey = null;
		view.Hide();
		NotifyIfFrontCardSizeChanged();
	}

	internal Displays.Monitor HostMonitor()
	{
		(int, int, int, int)? tuple = Host.NotchSurfacePx();
		if (tuple.HasValue)
		{
			(int, int, int, int) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item3 > 0)
			{
				return Displays.At(valueOrDefault.Item1 + valueOrDefault.Item3 / 2, valueOrDefault.Item2 + Math.Max(1, valueOrDefault.Item4 / 2));
			}
		}
		return Displays.Primary();
	}

	private bool UpdateNotchState()
	{
		Displays.Monitor on = HostMonitor();
		double scale = on.Scale;
		double panelWidth = (double)on.Bounds.Width / scale;
		double panelHeight = (double)on.Bounds.Height / scale;
		double a = Math.Max(0.0, (double)(on.Work.Top - on.Bounds.Top) / scale);
		(int, int, int, int)? tuple = Host.NotchSurfacePx();
		ArcRect? obj;
		if (tuple.HasValue)
		{
			(int, int, int, int) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item3 > 0 && valueOrDefault.Item4 > 0)
			{
				obj = new ArcRect((double)(valueOrDefault.Item1 - on.Bounds.Left) / scale, (double)(valueOrDefault.Item2 - on.Bounds.Top) / scale, (double)valueOrDefault.Item3 / scale, (double)valueOrDefault.Item4 / scale);
				goto IL_00f3;
			}
		}
		obj = null;
		goto IL_00f3;
		IL_00f3:
		ArcRect? notchSurface = obj;
		ArcScreenMetrics value = new ArcScreenMetrics(panelWidth, panelHeight, Math.Ceiling(a), null, notchSurface);
		isNotchExpanded = Host.NotchIsExpanded();
		bool flag = Host.IsFullscreenOnDockDisplay();
		if (flag != isTabTucked)
		{
			isTabTucked = flag;
		}
		if (metrics == value)
		{
			return false;
		}
		ArcScreenMetrics? arcScreenMetrics = metrics;
		bool flag2 = arcScreenMetrics.HasValue && view.Monitor.Handle != on.Handle;
		metrics = value;
		view.Place(on);
		view.Layout(LayoutReason.Metrics, !flag2);
		NotifyIfFrontCardSizeChanged();
		return true;
	}

	private void SyncGreetingNotchHold()
	{
		Host.SetNotchHeldOpenByArc(isOpen);
	}

	private (double X, double Y) PointerInPanel()
	{
		(double, double)? demoPointer = DemoPointer;
		if (demoPointer.HasValue)
		{
			return demoPointer.GetValueOrDefault();
		}
		Native.GetCursorPos(out var p);
		Displays.Monitor monitor = view.Monitor;
		return (X: (double)(p.X - monitor.Bounds.Left) / monitor.Scale, Y: (double)(p.Y - monitor.Bounds.Top) / monitor.Scale);
	}

	private void ScheduleNextTimedTick()
	{
		if (!pointerEventDriver.IsRunning)
		{
			return;
		}
		double now = PointerEvaluationDriver.Now;
		double? deadline = null;
		if (isOpen)
		{
			DateTimeOffset? dateTimeOffset = leftAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				double num = now + (valueOrDefault - DateTimeOffset.Now).TotalSeconds + CloseDelay + 0.001;
				if (num > now)
				{
					deadline = num;
				}
			}
		}
		if (isOpen && IsScrollGestureGoingOn)
		{
			double num2 = now + (lastScrollEventAt - DateTimeOffset.Now).TotalSeconds + 0.22 + 0.01;
			double value;
			if (deadline.HasValue)
			{
				double valueOrDefault2 = deadline.GetValueOrDefault();
				value = Math.Min(valueOrDefault2, num2);
			}
			else
			{
				value = num2;
			}
			deadline = value;
		}
		if (notchMovingUntil > now)
		{
			double num3 = Math.Max(now, lastNotchReadAt + 0.1);
			double value2;
			if (deadline.HasValue)
			{
				double valueOrDefault3 = deadline.GetValueOrDefault();
				value2 = Math.Min(valueOrDefault3, num3);
			}
			else
			{
				value2 = num3;
			}
			deadline = value2;
		}
		pointerEventDriver.EvaluateAt(deadline);
	}

	private void Tick()
	{
		Evaluate();
		SyncDeckPointer();
	}

	private void SyncDeckPointer()
	{
		if (!view.IsShowing)
		{
			SetPointerOnNotch(on: false);
			return;
		}
		(double, double) p = PointerInPanel();
		SetPointerOnNotch((metrics?.NotchSurface)?.Contains(p.Item1, p.Item2) ?? false);
		double slack = (view.TakesPointer ? 6 : 0);
		bool flag = isOpen && !isTourShowcase && !isAutoPresentedWindowAwaitingVisit && IsPointerOverInteractiveArea(p, slack);
		if ((flag || !isOpen || !view.TakesPointer || (Native.GetAsyncKeyState(1) & 0x8000) == 0) && (flag || !isOpen || !view.TakesPointer || !IsScrollGestureGoingOn) && flag != view.TakesPointer)
		{
			view.SetTakesPointer(flag);
		}
	}

	private void SetPointerOnNotch(bool on)
	{
		if (on != pointerOnNotch)
		{
			pointerOnNotch = on;
			this.PointerOnNotchChanged?.Invoke(on);
		}
	}

	private void Evaluate()
	{
		if (!view.IsShowing)
		{
			return;
		}
		ArcScreenMetrics? arcScreenMetrics = metrics;
		if (!arcScreenMetrics.HasValue)
		{
			return;
		}
		ArcScreenMetrics valueOrDefault = arcScreenMetrics.GetValueOrDefault();
		DateTimeOffset now = DateTimeOffset.Now;
		double now2 = PointerEvaluationDriver.Now;
		if (now2 - lastNotchReadAt >= 0.1)
		{
			lastNotchReadAt = now2;
			if (UpdateNotchState())
			{
				notchMovingUntil = Math.Max(notchMovingUntil, now2 + 0.6);
			}
			if (isOpen)
			{
				view.KeepZOrder();
			}
		}
		SyncGreetingNotchHold();
		if ((now - lastStatusTickAt).TotalSeconds > 20.0)
		{
			lastStatusTickAt = now;
			Refresh();
		}
		if (DemoHoldsOpen)
		{
			leftAt = null;
			return;
		}
		if (deckFocusKeyToRestore != null && !IsGreetingNotchOpen)
		{
			deckFocusKeyToRestore = null;
		}
		(double X, double Y) pointer = PointerInPanel();
		cardKeyUnderPointer = ((hoveredKey != null && cardFrame.Inset(-2.0, -2.0).Contains(pointer.X, pointer.Y)) ? hoveredKey : null);
		if (isTourShowcase)
		{
			if (!isOpen && IsGreetingNotchOpen && faceFrames.Count > 0)
			{
				OpenRail(pointer, "the welcome tour showing the notch");
			}
			leftAt = null;
			return;
		}
		if (IsWindowHeldOpen && hoveredKey != null)
		{
			leftAt = null;
			return;
		}
		if (!isOpen && (now - lastSoundPrepareAt).TotalSeconds > 5.0 && Host.PlaysSounds() && valueOrDefault.NotchTriggerFrame.Inset(-60.0, -30.0).Contains(pointer.X, pointer.Y))
		{
			lastSoundPrepareAt = now;
			try
			{
				ScreenDrawingSounds.Prepare();
			}
			catch (Exception ex)
			{
				Log.Warn("dock", "sound prepare: " + ex.Message);
			}
		}
		if (isAutoPresentedWindowAwaitingVisit)
		{
			if (hoveredKey != null && !IsPointerOverInteractiveArea(pointer))
			{
				leftAt = null;
				return;
			}
			isAutoPresentedWindowAwaitingVisit = false;
		}
		if (isOpen && !Host.NotchTabIsHome())
		{
			Close(immediately: true, "notch not on Home");
			return;
		}
		if (!isOpen && IsGreetingNotchOpen && faceFrames.Count > 0)
		{
			leftAt = null;
			OpenRail(pointer, "notch open on Home");
		}
		(double, double)? tuple = railOpenedAtPointer;
		if (tuple.HasValue)
		{
			(double, double) valueOrDefault2 = tuple.GetValueOrDefault();
			if (Math.Sqrt(Math.Pow(pointer.X - valueOrDefault2.Item1, 2.0) + Math.Pow(pointer.Y - valueOrDefault2.Item2, 2.0)) > 8.0)
			{
				railOpenedAtPointer = null;
			}
		}
		ArcRect? arcRect = pressedMoreFrame;
		if (arcRect.HasValue && !arcRect.GetValueOrDefault().Inset(-4.0, -4.0).Contains(pointer.X, pointer.Y))
		{
			pressedMoreFrame = null;
		}
		object obj;
		if (isOpen)
		{
			tuple = railOpenedAtPointer;
			if (!tuple.HasValue)
			{
				obj = faceFrames.FirstOrDefault<KeyValuePair<string, ArcRect>>((KeyValuePair<string, ArcRect> f) => f.Value.Inset(-4.0, -6.0).Contains(pointer.X, pointer.Y)).Key;
				goto IL_0413;
			}
		}
		obj = null;
		goto IL_0413;
		IL_0413:
		string text = (string)obj;
		if (isOpen && hoveredKey != null && cardFrame.Contains(pointer.X, pointer.Y))
		{
			leftAt = null;
		}
		else if (text != null)
		{
			leftAt = null;
			Open(text, "pointer on a window");
		}
		else if (hoveredKey != null && cardFrame.Inset(-18.0, -18.0).Contains(pointer.X, pointer.Y))
		{
			leftAt = null;
		}
		else if (isOpen && (railFrame.Inset(-10.0, -10.0).Contains(pointer.X, pointer.Y) || OpenDeckEdgeStrip(valueOrDefault).Contains(pointer.X, pointer.Y) || IsPointerOnMoreButton(pointer) || IsPointerOnTopArcNotch(valueOrDefault, pointer)))
		{
			leftAt = null;
		}
		else if (isOpen && IsGreetingNotchOpen && Host.NotchHoldsOpenWithoutArc())
		{
			if (hoveredKey == null)
			{
				leftAt = null;
				return;
			}
			DateTimeOffset? dateTimeOffset = leftAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault3 = dateTimeOffset.GetValueOrDefault();
				if ((now - valueOrDefault3).TotalSeconds > CloseDelay)
				{
					leftAt = null;
					FinishClose(keepingRailOpen: true);
				}
			}
			else
			{
				leftAt = now;
			}
		}
		else
		{
			if (!isOpen)
			{
				return;
			}
			DateTimeOffset? dateTimeOffset = leftAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault4 = dateTimeOffset.GetValueOrDefault();
				if ((now - valueOrDefault4).TotalSeconds > CloseDelay)
				{
					Close(immediately: false, "pointer left the arc and the notch");
				}
			}
			else
			{
				leftAt = now;
			}
		}
	}

	private static ArcRect OpenDeckEdgeStrip(ArcScreenMetrics arc)
	{
		return new ArcRect(0.0, 0.0, arc.PanelWidth, TopArcLayout.FrontTop(arc));
	}

	private bool IsPointerOnMoreButton((double X, double Y) p)
	{
		if (!moreFrames.Any((ArcRect f) => f.Inset(-4.0, -4.0).Contains(p.X, p.Y)))
		{
			return pressedMoreFrame?.Inset(-4.0, -4.0).Contains(p.X, p.Y) ?? false;
		}
		return true;
	}

	private static bool IsPointerOnTopArcNotch(ArcScreenMetrics arc, (double X, double Y) p)
	{
		return (arc.NotchSurface ?? arc.NotchTriggerFrame).Inset(-6.0, -6.0).Contains(p.X, p.Y);
	}

	private bool IsPointerOverInteractiveArea((double X, double Y) p, double slack = 0.0)
	{
		if (hoveredKey != null && cardFrame.Inset(-2.0 - slack, -2.0 - slack).Contains(p.X, p.Y))
		{
			return true;
		}
		ArcRect? arcRect = metrics?.NotchSurface;
		if (arcRect.HasValue && arcRect.GetValueOrDefault().Contains(p.X, p.Y) && !faceFrames.Values.Any((ArcRect f) => f.Inset(0.0 - slack, 0.0 - slack).Contains(p.X, p.Y)) && !DeckBand().Inset(0.0 - slack, 0.0 - slack).Contains(p.X, p.Y))
		{
			return false;
		}
		if (!isOpen)
		{
			return false;
		}
		if (!railFrame.Inset(-4.0 - slack, -4.0 - slack).Contains(p.X, p.Y))
		{
			return IsPointerOnMoreButton(p);
		}
		return true;
	}

	private ArcRect DeckBand()
	{
		if (faceFrames.Count == 0 || railFrame.IsEmpty)
		{
			return default(ArcRect);
		}
		double num = faceFrames.Values.Min((ArcRect f) => f.Y);
		string frontWindowKey = FrontWindowKey;
		ArcRect value;
		double num2 = ((frontWindowKey != null && faceFrames.TryGetValue(frontWindowKey, out value)) ? value.MaxY : faceFrames.Values.Max((ArcRect f) => f.MaxY));
		return new ArcRect(railFrame.X, num, railFrame.W, Math.Max(0.0, num2 - num));
	}

	internal void FramesChanged(ArcRect rail, IReadOnlyDictionary<string, ArcRect> faces, ArcRect? card, IReadOnlyList<ArcRect> more)
	{
		railFrame = rail;
		faceFrames.Clear();
		foreach (var (key, value) in faces)
		{
			faceFrames[key] = value;
		}
		if (card.HasValue)
		{
			ArcRect valueOrDefault = card.GetValueOrDefault();
			cardFrame = valueOrDefault;
		}
		moreFrames.Clear();
		moreFrames.AddRange(more);
		NotifyIfFrontCardSizeChanged();
		pointerEventDriver.EvaluateSoon();
	}

	private void Open(string key, string reason)
	{
		if (hoveredKey == key && isOpen)
		{
			return;
		}
		if (!isOpen)
		{
			LogArc("open", reason, key);
		}
		FileCardSound.NoteFocusChanged();
		PlaySwitchSound(hoveredKey, key);
		string text = hoveredKey;
		if (text != null && text != key)
		{
			NoteCardClosed(text);
		}
		isOpen = true;
		hoveredKey = key;
		view.Layout(LayoutReason.Hover);
		if (ClickyJob.JobIdFromRailKey(key) != null)
		{
			return;
		}
		string clickySlug = DeckWindows().FirstOrDefault((DockWindowKey w) => w.Key == key).ClickySlug;
		if (clickySlug != null)
		{
			IReadOnlyList<DockWindowKey> readOnlyList = DeckWindows();
			if (readOnlyList.Count > 0 && readOnlyList[DeckFocus.Clamped(deckFocusIndex, readOnlyList.Count)].Key == key)
			{
				ClickyAnalytics.TrackProductEvent("clicky_dashboard_action", clickySlug, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "edge_window_opened" });
			}
		}
	}

	private void OpenRail((double X, double Y) pointer, string reason)
	{
		if (!isOpen)
		{
			LogArc("open", reason);
		}
		railOpenedAtPointer = pointer;
		if (!isOpen)
		{
			IReadOnlyList<string> readOnlyList = DeckKeys();
			string text = deckFocusKeyToRestore;
			if (text != null)
			{
				int num = readOnlyList.ToList().IndexOf(text);
				if (num >= 0)
				{
					deckFocusIndex = num;
					goto IL_0075;
				}
			}
			if (readOnlyList.Count > 0)
			{
				deckFocusIndex = readOnlyList.Count - 1;
			}
			goto IL_0075;
		}
		goto IL_007c;
		IL_0075:
		deckFocusKeyToRestore = null;
		goto IL_007c;
		IL_007c:
		PlaySwitchSound(null, "rail");
		isOpen = true;
		closesInstantly = false;
		view.Layout(LayoutReason.Open);
	}

	private void Close(bool immediately = false, string reason = "")
	{
		leftAt = null;
		pressedMoreFrame = null;
		FinishClose(keepingRailOpen: false, !immediately, reason);
	}

	private void FinishClose(bool keepingRailOpen = false, bool isAnimated = true, string reason = "a window shrank back")
	{
		if (!keepingRailOpen && isOpen)
		{
			LogArc("close", reason);
		}
		isAutoPresentedWindowAwaitingVisit = false;
		if (!keepingRailOpen)
		{
			railOpenedAtPointer = null;
		}
		string text = hoveredKey;
		if (text != null)
		{
			NoteCardClosed(text);
		}
		if (!keepingRailOpen)
		{
			closesInstantly = !isAnimated;
			isOpen = false;
		}
		hoveredKey = null;
		view.Layout(keepingRailOpen ? LayoutReason.Hover : LayoutReason.Open);
		SyncGreetingNotchHold();
		SyncDeckPointer();
	}

	private void NoteCardClosed(string railKey)
	{
		string text = ClickyJob.JobIdFromRailKey(railKey);
		if (text != null)
		{
			if (voiceFollowUpKey == railKey)
			{
				voiceFollowUpKey = null;
			}
			if (!(railKey != FrontWindowKey))
			{
				agents.NoteJobCardClosed(text);
			}
		}
	}

	public void PresentWindow(string railKey)
	{
		if (isRunning && !isTabTucked)
		{
			Refresh();
			int num = DeckKeys().ToList().IndexOf(railKey);
			if (num >= 0 && (hoveredKey == null || !(hoveredKey != railKey) || !IsPointerOverInteractiveArea(PointerInPanel())))
			{
				deckFocusIndex = num;
				view.Layout(LayoutReason.Focus);
				Open(railKey, "a window came forward by itself");
				isAutoPresentedWindowAwaitingVisit = true;
			}
		}
	}

	internal bool HandleDeckScroll(int delta, bool horizontal, ulong timestamp = 0uL)
	{
		return HandleDeckScroll(delta, horizontal, EventTime(timestamp), PrecisionTouchpad.Snapshot);
	}

	internal bool HandleDeckScroll(int delta, bool horizontal, double time, TouchpadSnapshot pad)
	{
		if (!isOpen || IsWindowHeldOpen)
		{
			return false;
		}
		FileCardSound.NoteFocusChanged();
		lastScrollEventAt = DateTimeOffset.Now;
		ScheduleNextTimedTick();
		int num = scrollGestures.Feed(time, horizontal ? (-delta) : delta, pad);
		if (num == 0)
		{
			return true;
		}
		int count = DeckKeys().Count;
		int num2 = DeckFocus.Clamped(deckFocusIndex, count);
		int num3 = Math.Min(Math.Max(num2 + num, 0), Math.Max(count - 1, 0));
		if (num3 != num2)
		{
			TurnDeck(num3, PointerInPanel(), "scroll");
		}
		return true;
	}

	private static double EventTime(ulong timestamp)
	{
		double now = PointerEvaluationDriver.Now;
		if (timestamp == 0L)
		{
			return now;
		}
		double num = (double)timestamp / 1000000.0;
		double num2 = now - num;
		if (!(num2 >= -0.05) || !(num2 <= 2.0))
		{
			return now;
		}
		return Math.Min(num, now);
	}

	public void TurnDeckWithArrowKey(int step)
	{
		if (!isOpen || !IsGreetingNotchOpen)
		{
			return;
		}
		int count = DeckKeys().Count;
		if (count >= 2)
		{
			int num = DeckFocus.Clamped(deckFocusIndex, count);
			int num2 = Math.Min(Math.Max(num + step, 0), count - 1);
			if (num2 != num)
			{
				TurnDeck(num2, PointerInPanel(), "arrow_key");
			}
		}
	}

	private void TurnDeck(int next, (double X, double Y) pointer, string via)
	{
		IReadOnlyList<string> readOnlyList = DeckKeys();
		int num = DeckFocus.Clamped(deckFocusIndex, readOnlyList.Count);
		if (hoveredKey != null)
		{
			FinishClose(keepingRailOpen: true);
		}
		deckFocusIndex = next;
		if (next != num && next >= 0 && next < readOnlyList.Count)
		{
			TrackDeckTurned(readOnlyList[next], num, next, readOnlyList.Count, via);
		}
		view.Layout(LayoutReason.Focus);
		PlaySwitchSound("deck", $"deck-{next}");
		IReadOnlyList<string> readOnlyList2 = DeckKeys();
		if (readOnlyList2.Count > 0)
		{
			Open(readOnlyList2[DeckFocus.Clamped(next, readOnlyList2.Count)], "the deck turned to it");
		}
		railOpenedAtPointer = pointer;
	}

	internal void ShowDeckWindow(int index, string via)
	{
		if (!isOpen)
		{
			return;
		}
		int count = DeckKeys().Count;
		int num = Math.Min(Math.Max(index, 0), Math.Max(count - 1, 0));
		if (num == DeckFocus.Clamped(deckFocusIndex, count) && hoveredKey == null)
		{
			return;
		}
		(double X, double Y) pointer = PointerInPanel();
		if (via == "more_pill")
		{
			ArcRect value = moreFrames.FirstOrDefault((ArcRect f) => f.Inset(-4.0, -4.0).Contains(pointer.X, pointer.Y));
			pressedMoreFrame = ((value.W > 0.0) ? new ArcRect?(value) : ((ArcRect?)null));
		}
		TurnDeck(num, pointer, via);
	}

	private void TrackArcOpenedOrClosed(bool isOpening, string reason)
	{
		long tickCount = Environment.TickCount64;
		if (isOpening)
		{
			arcOpenedAt = tickCount;
			arcOpenedReason = reason;
			arcOpenedWindowCount = DeckKeys().Count;
			return;
		}
		long? num = arcOpenedAt;
		if (num.HasValue)
		{
			long valueOrDefault = num.GetValueOrDefault();
			arcOpenedAt = null;
			JsonObject jsonObject = ArcDockAnalytics.Closed(reason, arcOpenedReason, arcOpenedWindowCount, (double)Math.Max(0L, tickCount - valueOrDefault) / 1000.0);
			if (jsonObject != null)
			{
				ClickyAnalytics.TrackProductEvent("arc_dock_action", null, null, jsonObject);
			}
		}
	}

	private void TrackDeckTurned(string key, int previous, int next, int windowCount, string via)
	{
		string text = ClickyJob.JobIdFromRailKey(key);
		ClickyJob clickyJob = ((text == null) ? null : agents.Jobs.Job(text));
		string windowKind = ((text == null) ? "clicky" : (((object)clickyJob != null && clickyJob.IsWaitingToStart) ? "suggested" : "job"));
		ClickyAnalytics.TrackProductEvent("arc_dock_action", (text == null) ? key : null, clickyJob?.ThreadId, ArcDockAnalytics.DeckTurned(via, previous, next, windowCount, windowKind));
	}

	private void PlaySwitchSound(string? previous, string next)
	{
		if (!Host.PlaysSounds() || previous == next)
		{
			return;
		}
		DateTimeOffset now = DateTimeOffset.Now;
		if ((now - lastSwitchSoundAt).TotalSeconds <= 0.12)
		{
			return;
		}
		lastSwitchSoundAt = now;
		try
		{
			ScreenDrawingSounds.Play("gel-pop", 0.2f, 0.03, soundOwner);
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "gel pop: " + ex.Message);
		}
	}

	private void LogArc(string arcEvent, string reason, string? key = null)
	{
		TrackArcOpenedOrClosed(arcEvent == "open", reason);
	}

	public bool NotePushToTalkDown()
	{
		voiceFollowUpKey = null;
		oneBrainFocusedWindow = null;
		windowDirectVoiceKey = null;
		if (Host.OnboardingHidesDock())
		{
			return false;
		}
		string text = cardKeyUnderPointer;
		if (text == null)
		{
			return false;
		}
		voiceFollowUpKey = text;
		oneBrainFocusedWindow = (text, DateTimeOffset.Now, AgentFocusPlace.Dock);
		string text2 = ClickyJob.JobIdFromRailKey(text);
		if (text2 != null)
		{
			string text3 = agents.Jobs.Job(text2)?.ThreadId;
			if (text3 != null)
			{
				ClickyAnalytics.TrackProductEvent("job_action", null, text3, new JsonObject((JsonNodeOptions?)null)
				{
					["action"] = "voice_follow_up",
					["via"] = "one_brain"
				});
				goto IL_0118;
			}
		}
		ClickyAnalytics.TrackProductEvent("clicky_dashboard_action", text, null, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "footer_voice_follow_up",
			["via"] = "one_brain"
		});
		goto IL_0118;
		IL_0118:
		return true;
	}

	public void NoteFullReplyVoiceFollowUp(string key)
	{
		oneBrainFocusedWindow = (key, DateTimeOffset.Now, AgentFocusPlace.NotchFullReply);
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "full_reply_voice_follow_up" });
	}

	public void ClearVoiceFollowUp()
	{
		voiceFollowUpKey = null;
	}

	public void NoteVoiceStateChanged()
	{
		dq.TryEnqueue(pointerEventDriver.EvaluateSoon);
	}

	public void NoteTypedAgentMessage(string key, bool isNotchFullReply)
	{
		oneBrainTypedAgentFocus = (key, DateTimeOffset.Now, (!isNotchFullReply) ? AgentFocusPlace.Notch : AgentFocusPlace.NotchFullReply);
	}

	public void NoteTypedAgentMessageInWindow(string key)
	{
		oneBrainTypedAgentFocus = (key, DateTimeOffset.Now, AgentFocusPlace.Window);
	}

	public void NoteWindowVoiceFollowUp(string key)
	{
		oneBrainFocusedWindow = (key, DateTimeOffset.Now, AgentFocusPlace.Window);
		windowDirectVoiceKey = (key, DateTimeOffset.Now);
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "window_voice_follow_up" });
	}

	public string? TakeWindowDirectVoiceKey()
	{
		(string, DateTimeOffset)? tuple = windowDirectVoiceKey;
		if (tuple.HasValue)
		{
			(string, DateTimeOffset) valueOrDefault = tuple.GetValueOrDefault();
			windowDirectVoiceKey = null;
			if (!((DateTimeOffset.Now - valueOrDefault.Item2).TotalSeconds < 180.0))
			{
				return null;
			}
			return valueOrDefault.Item1;
		}
		return null;
	}

	public void DropOneBrainFocusedWindow()
	{
		oneBrainFocusedWindow = null;
	}

	public string? FocusNoteForOneBrainTurn(bool typed = false)
	{
		(string Key, DateTimeOffset At, AgentFocusPlace Place) focus;
		if (typed)
		{
			(string, DateTimeOffset, AgentFocusPlace)? tuple = oneBrainTypedAgentFocus;
			if (!tuple.HasValue)
			{
				return null;
			}
			(string, DateTimeOffset, AgentFocusPlace) valueOrDefault = tuple.GetValueOrDefault();
			oneBrainTypedAgentFocus = null;
			if ((DateTimeOffset.Now - valueOrDefault.Item2).TotalSeconds >= 30.0)
			{
				return null;
			}
			focus = valueOrDefault;
		}
		else
		{
			(string, DateTimeOffset, AgentFocusPlace)? tuple = oneBrainFocusedWindow;
			if (!tuple.HasValue)
			{
				return null;
			}
			(string, DateTimeOffset, AgentFocusPlace) valueOrDefault2 = tuple.GetValueOrDefault();
			oneBrainFocusedWindow = null;
			if ((DateTimeOffset.Now - valueOrDefault2.Item2).TotalSeconds >= 180.0)
			{
				return null;
			}
			focus = valueOrDefault2;
		}
		string text = ClickyJob.JobIdFromRailKey(focus.Key);
		if (text != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(text);
			if ((object)clickyJob != null)
			{
				string text2 = clickyJob.Title.Replace("\"", "'");
				if (text2.Length > 80)
				{
					text2 = text2.Substring(0, 80);
				}
				if (clickyJob.ThreadId == null)
				{
					return Origin("the job \"" + text2 + "\"") + " It hasn't started yet; their words are most likely about it.";
				}
				return Origin($"the job \"{text2}\" (job_id: {clickyJob.Id})") + " " + Guidance("job_id " + clickyJob.Id);
			}
		}
		ClickyInfo clickyInfo = agents.FindClicky(focus.Key);
		if ((object)clickyInfo == null)
		{
			return null;
		}
		string value = clickyInfo.Name.Replace("\"", "'");
		string threadId = clickyInfo.ThreadId;
		if (threadId != null)
		{
			string text3 = agents.OneBrainJobId(threadId);
			return Origin($"\"{value}\" (job_id: {text3})") + " " + Guidance("job_id " + text3);
		}
		return Origin($"\"{value}\" (clicky_slug: {clickyInfo.Slug})") + " " + Guidance("clicky_slug " + clickyInfo.Slug);
		static string Guidance(string handle)
		{
			return "Their words are most likely for or about it. Use judgment: an instruction, a change, or an answer to what it asked goes to it with sessions_send (" + handle + "); HeyClicky passes on their exact words, so don't reword them, and you say in a few words that you passed it on. A question about its progress or result you answer yourself (clicky_memory); if they clearly mean something else, do that instead.";
		}
		string Origin(string who)
		{
			AgentFocusPlace item = focus.Place;
			if (typed)
			{
				return item switch
				{
					AgentFocusPlace.NotchFullReply => "[Notch] The user typed this in the box under " + who + "'s reply, open in full in the notch.", 
					AgentFocusPlace.Window => "[HeyClicky window] The user typed this in " + who + "'s conversation in the HeyClicky window.", 
					_ => "[Notch] The user typed this to " + who + " in the notch.", 
				};
			}
			return item switch
			{
				AgentFocusPlace.NotchFullReply => "[Notch] The user held the voice keys while " + who + "'s reply was open in full in the notch.", 
				AgentFocusPlace.Window => "[HeyClicky window] The user held the voice keys with the pointer on " + who + "'s conversation in the HeyClicky window.", 
				_ => "[Edge dock] The user held the voice keys with the pointer on " + who + "'s window on the dock.", 
			};
		}
	}

	public PointD? LaunchLandingPointPx()
	{
		if (!ReplacesAgentHud)
		{
			return null;
		}
		Displays.Monitor monitor = HostMonitor();
		return new PointD((double)monitor.Bounds.Left + (double)monitor.Bounds.Width / 2.0, (double)monitor.Work.Top + 8.0 * monitor.Scale);
	}

	public void DemoOpen(string? key = null)
	{
		if (!view.IsShowing)
		{
			Refresh();
		}
		OpenRail(PointerInPanel(), "demo");
		railOpenedAtPointer = null;
		if (key != null)
		{
			int num = DeckKeys().ToList().IndexOf(key);
			if (num >= 0)
			{
				deckFocusIndex = num;
				view.Layout(LayoutReason.Focus);
			}
			Open(key, "demo");
		}
	}

	public void DemoClose()
	{
		Close(immediately: false, "demo");
	}

	public void DemoPeek(string key)
	{
		Open(key, "demo peek");
	}

	public void DemoEvaluate()
	{
		Tick();
	}

	internal ArcLiveNote? LiveNote(ArcCardContent content)
	{
		string notePath;
		ClickyInkCardSubject subject;
		ClickyInkNoteTarget target;
		if (ArcLiveNotes.IsEnabled)
		{
			notePath = content.NotePath;
			if (notePath != null)
			{
				string jobId = content.JobId;
				if (jobId != null)
				{
					ClickyJob clickyJob = agents.Jobs.Job(jobId);
					if ((object)clickyJob != null)
					{
						subject = ClickyInkNoteBridge.SubjectForJob(ClickyInkLiveNoteTurns.InkJob(agents, clickyJob));
						target = new ClickyInkNoteTarget.Job(clickyJob.Id);
						goto IL_0085;
					}
				}
				string clickySlug = content.ClickySlug;
				if (clickySlug != null && (object)agents.FindClicky(clickySlug) != null)
				{
					subject = ClickyInkNoteBridge.SubjectForClicky(clickySlug);
					target = new ClickyInkNoteTarget.Clicky(clickySlug);
					goto IL_0085;
				}
				return null;
			}
		}
		return null;
		IL_0085:
		ClickyHomeInkNote clickyHomeInkNote = ClickyInkNoteStore.Shared.InkNote(notePath);
		if ((object)clickyHomeInkNote == null)
		{
			return null;
		}
		return new ArcLiveNote(notePath, subject, target, clickyHomeInkNote);
	}

	internal static ClickyInkTopOfNote TopOfNote(ArcLiveNote note, bool isWorking, bool animates)
	{
		return new ClickyInkTopOfNote(note.Ink, ClickyInkNoteBridge.Paper(note.Subject), ClickyInkNoteBridge.Actions(note.NotePath, note.Target, note.Ink, note.Subject), isWorking, animates, isGlancePreview: true);
	}
}
