using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Core.Drive;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;
using HeyClicky.Platform.Capture;
using HeyClicky.Platform.Input;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Dispatching;
using Windows.UI;

namespace HeyClicky.App.Drive;

public sealed class ClickyTakeOverController : IClickyDriveTurns
{
	public enum PhaseKind
	{
		Idle,
		Offered,
		Driving
	}

	public readonly record struct Phase(PhaseKind Kind, string Goal = "", int Step = 0);

	public static readonly IReadOnlySet<string> ToolNames = new HashSet<string> { "offer_screen_takeover", "start_screen_takeover", "end_screen_takeover" };

	private readonly CompanionSession session;

	private readonly ScreenCaptureService capture;

	private readonly DispatcherQueue ui;

	private readonly object gate = new object();

	private readonly TakeOverOverlay overlay = new TakeOverOverlay();

	private readonly TakeOverConsentCard consentCard = new TakeOverConsentCard();

	private Phase phase = new Phase(PhaseKind.Idle);

	private CancellationTokenSource? loopCancellation;

	private ScreenCaptureFrame? stepCapture;

	private IReadOnlyList<DisplayInfo> stepDisplays = Array.Empty<DisplayInfo>();

	private string? targetProcessName;

	private string overlayStatus = "Getting started";

	private string? endSummary;

	private DateTimeOffset actuatingUntil = DateTimeOffset.MinValue;

	private DateTimeOffset movesIgnoredUntil = DateTimeOffset.MinValue;

	private static readonly TimeSpan BeginMoveGrace = TimeSpan.FromSeconds(1L);

	private (string Goal, DateTimeOffset At)? offerAnsweredBySpeaking;

	private static readonly TimeSpan SpokenAnswerWindow = TimeSpan.FromSeconds(30L);

	private readonly ClickyDriveUserMouse userMouse = new ClickyDriveUserMouse();

	private bool hasClickyActedThisDrive;

	private bool isShowingUserMouseHint;

	private int userMousePauseCount;

	private CancellationTokenSource? userMouseHintLinger;

	private ClickyTakeBackWatch? userInputWatch;

	private DateTimeOffset lastTakeOverEndedAt = DateTimeOffset.MinValue;

	private bool currentTurnProgramSpoke;

	private string? currentTurnToken;

	private bool spokeForSilentTurn;

	private List<string> askingTurnTokens = new List<string>();

	private ClickyDriveBilling? pendingBilling;

	private ClickyDriveBilling driveBilling = ClickyDriveBilling.UserDrive;

	private string driveIdentifier = Guid.NewGuid().ToString().ToUpperInvariant();

	private List<string> driveAskingTurnTokens = new List<string>();

	private DateTimeOffset driveStartedAt = DateTimeOffset.MinValue;

	private string? driveLeaseId;

	private bool driveBillingSent;

	private Action<AgentDriveOutcome>? agentDriveEnded;

	private AgentDriveEnding driveEnding;

	private string? agentDriveClickyName;

	private string? offeredThen;

	private string? driveThen;

	private readonly List<string> actionLog = new List<string>();

	private readonly List<JsonObject> lastCallResults = new List<JsonObject>();

	private string? pendingAnalyticsOrigin;

	private string driveAnalyticsOrigin = "offer_card";

	private string? pendingAnalyticsThreadId;

	private string? driveAnalyticsThreadId;

	private string? driveTakenBackBy;

	public static ClickyTakeOverController? Current { get; private set; }

	public Func<Color> Accent { get; set; } = () => Color.FromArgb(byte.MaxValue, 59, 130, 246);

	public Action<string, double> PlayChime { get; set; } = Sounds.Play;

	public Func<bool> IsAvailable { get; set; } = () => true;

	public Func<bool> ChromeSetupIsRunning { get; set; } = () => false;

	public Func<string, AgentDriveBinding?> MacStepBindingForBrainDrive { get; set; } = (string _) => (AgentDriveBinding?)null;

	public Func<string, Task>? RefundEarlyStop { get; set; }

	public Func<Task>? RefreshBilling { get; set; }

	public Phase CurrentPhase
	{
		get
		{
			lock (gate)
			{
				return phase;
			}
		}
	}

	public bool IsDriving
	{
		get
		{
			lock (gate)
			{
				return phase.Kind == PhaseKind.Driving;
			}
		}
	}

	public DateTimeOffset LastEndedAt
	{
		get
		{
			lock (gate)
			{
				return lastTakeOverEndedAt;
			}
		}
	}

	public IReadOnlyList<string> ActionLog
	{
		get
		{
			lock (gate)
			{
				return actionLog.ToList();
			}
		}
	}

	public IReadOnlyList<JsonObject> LastCallResults
	{
		get
		{
			lock (gate)
			{
				return lastCallResults.ToList();
			}
		}
	}

	public bool OffersDrive => IsAvailable();

	public bool ClaimsEscape => CurrentPhase.Kind != PhaseKind.Idle;

	private static void TrackDrive(string action, string? threadId = null, JsonObject? properties = null)
	{
		ClickyDriveAnalytics.Track(action, threadId, properties);
	}

	private static void TrackOfferAnswered(string answer)
	{
		TrackDrive("offer_answered", null, new JsonObject((JsonNodeOptions?)null) { ["answer"] = answer });
	}

	public ClickyTakeOverController(CompanionSession session, ScreenCaptureService capture, DispatcherQueue ui)
	{
		this.session = session;
		this.capture = capture;
		this.ui = ui;
		Current = this;
	}

	public bool IsDriveToolName(string name)
	{
		return ToolNames.Contains(name);
	}

	public JsonObject TakeDriveBillingRequestFields()
	{
		lock (gate)
		{
			if (phase.Kind != PhaseKind.Driving || driveBillingSent)
			{
				return new JsonObject((JsonNodeOptions?)null);
			}
			driveBillingSent = true;
			return ClickyDriveBilling.RequestFields(driveBilling, driveIdentifier, driveAskingTurnTokens);
		}
	}

	public void NoteDriveLeaseId(string leaseId)
	{
		lock (gate)
		{
			if (phase.Kind == PhaseKind.Driving && driveBilling is ClickyDriveBilling.User)
			{
				driveLeaseId = leaseId;
			}
		}
	}

	public void NoteTurnBeforeTools(bool programSpoke, string? turnToken)
	{
		lock (gate)
		{
			currentTurnProgramSpoke = programSpoke;
			currentTurnToken = turnToken;
		}
	}

	public void StopForOutOfAgentMessages()
	{
		OnUi(delegate
		{
			if (IsDriving)
			{
				Log.Info("drive", "out of agent messages: handing back");
				lock (gate)
				{
					driveEnding = AgentDriveEnding.OutOfAgentMessages;
				}
				Finish("You're out of agent messages for now, so I stopped here.", "agent-close");
			}
		});
	}

	public void NoteDriveRequestFailed()
	{
		lock (gate)
		{
			if (phase.Kind == PhaseKind.Driving && driveEnding == AgentDriveEnding.Finished)
			{
				driveEnding = AgentDriveEnding.Failed;
			}
		}
	}

	public void PreviewForDebug()
	{
		OnUi(delegate
		{
			if (CurrentPhase.Kind == PhaseKind.Idle)
			{
				Color accent = Accent();
				consentCard.Present("add a bass line to the second track", "Want me to add the bass line while you watch?", accent, async delegate
				{
					PlayChime("agent-launch", 0.5);
					overlay.Show(Displays.UnderCursor(), accent, "Getting started");
					string[] array = new string[3] { "I'll open the Instruments list, where the synths live.", "Basic Analog Bass is a warm start, so I'll pick it.", "Now I drag it onto your second track." };
					foreach (string status in array)
					{
						await Task.Delay(1400);
						overlay.SetStatus(status);
						await Task.Delay(900);
						overlay.SetActing(acting: true);
						await Task.Delay(1300);
						overlay.SetActing(acting: false);
					}
					await Task.Delay(900);
					PlayChime("agent-done", 0.55);
					overlay.Hide();
				}, delegate
				{
				}, PlayChime);
			}
		});
	}

	internal void AcceptConsentForTest()
	{
		OnUi(consentCard.AcceptForTest);
	}

	public Task<string> HandleToolAsync(string name, string argumentsJson)
	{
		return InvokeOnUi(() => HandleTool(name, argumentsJson));
	}

	private string HandleTool(string name, string argumentsJson)
	{
		JsonObject jsonObject;
		try
		{
			jsonObject = (JsonNode.Parse(argumentsJson, null) as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		}
		catch (JsonException)
		{
			jsonObject = new JsonObject((JsonNodeOptions?)null);
		}
		string text = ((string?)jsonObject["goal"])?.Trim();
		switch (name)
		{
		case "offer_screen_takeover":
		{
			if (IsDriving)
			{
				TrackDrive("offer_refused", null, new JsonObject((JsonNodeOptions?)null) { ["reason"] = "already_driving" });
				return Result("error", "You are already driving.");
			}
			if (ChromeSetupIsRunning())
			{
				TrackDrive("offer_refused", null, new JsonObject((JsonNodeOptions?)null) { ["reason"] = "chrome_setup_running" });
				return Result("error", "HeyClicky is setting up Chrome on their screen right now. Wait for it.");
			}
			if (CurrentPhase.Kind == PhaseKind.Offered)
			{
				TrackOfferAnswered("replaced");
			}
			string offeredGoal = (string.IsNullOrEmpty(text) ? "do these steps for you" : text);
			bool flag;
			lock (gate)
			{
				this.phase = new Phase(PhaseKind.Offered, offeredGoal);
				offerAnsweredBySpeaking = null;
				string text2 = currentTurnToken;
				List<string> list;
				if (text2 == null)
				{
					list = new List<string>();
				}
				else
				{
					int num = 1;
					list = new List<string>(num);
					CollectionsMarshal.SetCount(list, num);
					Span<string> span = CollectionsMarshal.AsSpan(list);
					int index = 0;
					span[index] = text2;
				}
				askingTurnTokens = list;
				string text3 = ((string?)jsonObject["then"])?.Trim();
				offeredThen = ((text3 != null && text3.Length > 0) ? text3 : null);
				flag = currentTurnProgramSpoke;
			}
			string text4 = ((string?)jsonObject["say"])?.Trim();
			if (!flag && !string.IsNullOrEmpty(text4))
			{
				session.SpeakVerbatimLineAsync((text4.Length > 200) ? text4.Substring(0, 200) : text4);
				lock (gate)
				{
					spokeForSilentTurn = true;
				}
			}
			string line = (string.IsNullOrEmpty(text4) ? null : ((text4.Length > 140) ? text4.Substring(0, 140) : text4));
			consentCard.Present(offeredGoal, line, Accent(), delegate
			{
				TrackOfferAnswered("yes_card");
				lock (gate)
				{
					pendingAnalyticsOrigin = "offer_card";
				}
				Begin(offeredGoal);
			}, delegate
			{
				Declined("no_card");
			}, PlayChime, delegate
			{
				Declined("timed_out");
			});
			bool flag2;
			lock (gate)
			{
				flag2 = offeredThen != null;
			}
			TrackDrive("offered", null, new JsonObject((JsonNodeOptions?)null) { ["has_then"] = flag2 });
			return Result("offered", "A card is asking for their yes. Stop here; don't do any steps until they agree.");
		}
		case "start_screen_takeover":
		{
			Phase phase;
			DateTimeOffset dateTimeOffset;
			(string, DateTimeOffset)? tuple;
			lock (gate)
			{
				string text5 = currentTurnToken;
				if (text5 != null && !askingTurnTokens.Contains(text5))
				{
					askingTurnTokens.Add(text5);
				}
				phase = this.phase;
				dateTimeOffset = lastTakeOverEndedAt;
				tuple = offerAnsweredBySpeaking;
			}
			if (phase.Kind == PhaseKind.Offered)
			{
				TrackOfferAnswered("yes_spoken");
				lock (gate)
				{
					pendingAnalyticsOrigin = "offer_spoken";
				}
				Begin(string.IsNullOrEmpty(text) ? phase.Goal : text);
				return Result("started");
			}
			if (phase.Kind != PhaseKind.Driving && tuple.HasValue)
			{
				(string, DateTimeOffset) valueOrDefault = tuple.GetValueOrDefault();
				if (DateTimeOffset.Now - valueOrDefault.Item2 < SpokenAnswerWindow)
				{
					lock (gate)
					{
						offerAnsweredBySpeaking = null;
						pendingAnalyticsOrigin = "offer_spoken";
					}
					TrackOfferAnswered("yes_spoken");
					string goal;
					if (string.IsNullOrEmpty(text))
					{
						(goal, _) = valueOrDefault;
					}
					else
					{
						goal = text;
					}
					Begin(goal);
					return Result("started");
				}
			}
			if (phase.Kind != PhaseKind.Driving && DateTimeOffset.Now - dateTimeOffset < TimeSpan.FromSeconds(600L) && !string.IsNullOrEmpty(text))
			{
				TrackDrive("consent_skipped", null, new JsonObject((JsonNodeOptions?)null) { ["reason"] = "recent_yes" });
				lock (gate)
				{
					pendingAnalyticsOrigin = "recent_yes";
				}
				Begin(text);
				return Result("started");
			}
			if (phase.Kind != PhaseKind.Driving && !string.IsNullOrEmpty(text))
			{
				AgentDriveBinding agentDriveBinding = MacStepBindingForBrainDrive(text);
				if ((object)agentDriveBinding != null)
				{
					lock (gate)
					{
						agentDriveEnded = agentDriveBinding.OnEnded;
						agentDriveClickyName = agentDriveBinding.ClickyName;
						pendingBilling = new ClickyDriveBilling.Job(agentDriveBinding.JobLeaseId);
						pendingAnalyticsOrigin = "job_step_spoken";
						pendingAnalyticsThreadId = agentDriveBinding.ThreadId;
					}
					TrackDrive("consent_skipped", agentDriveBinding.ThreadId, new JsonObject((JsonNodeOptions?)null) { ["reason"] = "job_card_was_the_ask" });
					Begin(text);
					return Result("started");
				}
			}
			if (phase.Kind == PhaseKind.Idle && !ChromeSetupIsRunning())
			{
				string offeredGoal2 = (string.IsNullOrEmpty(text) ? "do these steps for you" : text);
				lock (gate)
				{
					this.phase = new Phase(PhaseKind.Offered, offeredGoal2);
					offerAnsweredBySpeaking = null;
					offeredThen = null;
				}
				consentCard.Present(offeredGoal2, null, Accent(), delegate
				{
					TrackOfferAnswered("yes_card");
					lock (gate)
					{
						pendingAnalyticsOrigin = "offer_card";
					}
					Begin(offeredGoal2);
				}, delegate
				{
					Declined("no_card");
				}, PlayChime, delegate
				{
					Declined("timed_out");
				});
				TrackDrive("offered", null, new JsonObject((JsonNodeOptions?)null)
				{
					["has_then"] = false,
					["origin"] = "start_without_offer"
				});
				return Result("offered", "There was no yes to take over yet, so a card is asking for it now. Stop here; don't do any steps until they agree.");
			}
			lock (gate)
			{
				askingTurnTokens = new List<string>();
			}
			TrackDrive("start_refused", null, new JsonObject((JsonNodeOptions?)null)
			{
				["reason"] = ((phase.Kind == PhaseKind.Driving) ? "already_driving" : "no_offer"),
				["origin"] = "brain"
			});
			return Result("error", "Offer first with offer_screen_takeover; take over only after their yes.");
		}
		case "end_screen_takeover":
			lock (gate)
			{
				endSummary = ((string?)jsonObject["summary"]) ?? "done";
			}
			return Result("ended");
		default:
			return Result("error", "Unknown take-over tool.");
		}
	}

	private static string Result(string status, string? message = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["status"] = status };
		if (message != null)
		{
			jsonObject["message"] = message;
		}
		return jsonObject.ToJsonString();
	}

	public bool DriveForAgent(string goal, string clickyName, string? jobLeaseId, Action<AgentDriveOutcome> onEnded, string? threadId = null)
	{
		if (CurrentPhase.Kind != 0 || ChromeSetupIsRunning())
		{
			TrackDrive("start_refused", threadId, new JsonObject((JsonNodeOptions?)null)
			{
				["reason"] = "busy",
				["origin"] = "job_step_approved"
			});
			return false;
		}
		lock (gate)
		{
			agentDriveEnded = onEnded;
			agentDriveClickyName = clickyName;
			askingTurnTokens = new List<string>();
			pendingBilling = new ClickyDriveBilling.Job(jobLeaseId);
			pendingAnalyticsOrigin = "job_step_approved";
			pendingAnalyticsThreadId = threadId;
		}
		Begin(goal);
		if (IsDriving)
		{
			return true;
		}
		lock (gate)
		{
			agentDriveEnded = null;
			agentDriveClickyName = null;
			pendingBilling = null;
			pendingAnalyticsOrigin = null;
			pendingAnalyticsThreadId = null;
		}
		TrackDrive("start_refused", threadId, new JsonObject((JsonNodeOptions?)null)
		{
			["reason"] = "busy",
			["origin"] = "job_step_approved"
		});
		return false;
	}

	public bool DriveForApp(string goal, string? targetProcessName, Action<AgentDriveOutcome> onEnded)
	{
		if (CurrentPhase.Kind != 0)
		{
			TrackDrive("start_refused", null, new JsonObject((JsonNodeOptions?)null)
			{
				["reason"] = "busy",
				["origin"] = "app_setup"
			});
			return false;
		}
		lock (gate)
		{
			agentDriveEnded = onEnded;
			this.targetProcessName = targetProcessName;
			askingTurnTokens = new List<string>();
			pendingBilling = ClickyDriveBilling.SetupDrive;
			pendingAnalyticsOrigin = "app_setup";
		}
		Begin(goal);
		if (IsDriving)
		{
			return true;
		}
		lock (gate)
		{
			agentDriveEnded = null;
			this.targetProcessName = null;
			pendingBilling = null;
			pendingAnalyticsOrigin = null;
		}
		TrackDrive("start_refused", null, new JsonObject((JsonNodeOptions?)null)
		{
			["reason"] = "busy",
			["origin"] = "app_setup"
		});
		return false;
	}

	private DisplayInfo? WorkDisplay()
	{
		string text;
		lock (gate)
		{
			text = targetProcessName;
		}
		IReadOnlyList<DisplayInfo> displays = capture.GetDisplays();
		if (text != null)
		{
			RectD? rectD = AppWindowLocator.FrontWindowFrame(text);
			if (rectD.HasValue)
			{
				RectD valueOrDefault = rectD.GetValueOrDefault();
				return AppWindowLocator.Display(valueOrDefault, displays);
			}
		}
		return AppWindowLocator.FrontmostAppDisplay(displays);
	}

	private ScreenCaptureFrame? CaptureForWork(IReadOnlyList<ScreenCaptureFrame> captures, ScreenCaptureFrame? previous)
	{
		DisplayInfo display = WorkDisplay();
		PointD pointer = ClickyScreenActuator.CurrentPointer;
		ScreenCaptureFrame screenCaptureFrame = (((object)display == null) ? null : captures.FirstOrDefault((ScreenCaptureFrame c) => c.Display.BoundsPx == display.BoundsPx)) ?? (((object)previous == null) ? null : captures.FirstOrDefault((ScreenCaptureFrame c) => c.Display.BoundsPx == previous.Display.BoundsPx)) ?? captures.FirstOrDefault((ScreenCaptureFrame c) => c.Display.BoundsPx.Contains(pointer)) ?? captures.FirstOrDefault((ScreenCaptureFrame c) => c.ContainsCursor) ?? captures.FirstOrDefault();
		if ((object)previous != null && (object)screenCaptureFrame != null && screenCaptureFrame.Display.BoundsPx != previous.Display.BoundsPx && (object)display != null)
		{
			Log.Info("drive", "the work moved to another display");
			string status;
			lock (gate)
			{
				status = overlayStatus;
			}
			Displays.Monitor monitor = MonitorFor(display);
			OnUi(delegate
			{
				overlay.Show(monitor, Accent(), status);
			});
		}
		return screenCaptureFrame;
	}

	private static Displays.Monitor MonitorFor(DisplayInfo display)
	{
		return Displays.At((int)(display.BoundsPx.X + display.BoundsPx.Width / 2.0), (int)(display.BoundsPx.Y + display.BoundsPx.Height / 2.0));
	}

	public void VoiceSilentToolTurn(string name, string argumentsJson)
	{
		JsonObject jsonObject;
		try
		{
			jsonObject = (JsonNode.Parse(argumentsJson, null) as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		}
		catch (JsonException)
		{
			jsonObject = new JsonObject((JsonNodeOptions?)null);
		}
		if (!(name == "offer_screen_takeover"))
		{
			if (name == "end_screen_takeover")
			{
				string text = (string?)jsonObject["summary"];
				if (text != null && text.Length > 0)
				{
					session.SpeakVerbatimLineAsync(ClickyDriveLines.SummaryFirstSentence(text));
				}
			}
			return;
		}
		lock (gate)
		{
			if (spokeForSilentTurn)
			{
				spokeForSilentTurn = false;
				return;
			}
		}
		session.SpeakVerbatimLineAsync("Want me to just do it? I'll drive, you watch.");
	}

	private void Declined(string answer)
	{
		lock (gate)
		{
			if (phase.Kind != PhaseKind.Offered)
			{
				return;
			}
			phase = new Phase(PhaseKind.Idle);
			offeredThen = null;
			askingTurnTokens = new List<string>();
		}
		TrackOfferAnswered(answer);
		Log.Info("drive", "declined");
		session.SubmitFactAsync("[App] The user chose to do it themselves (they declined the take-over). Keep guiding them.");
	}

	public void Begin(string goal)
	{
		if (!ui.HasThreadAccess)
		{
			ui.TryEnqueue(delegate
			{
				Begin(goal);
			});
		}
		else
		{
			if (IsDriving)
			{
				return;
			}
			bool flag;
			lock (gate)
			{
				flag = agentDriveEnded == null;
			}
			if (flag)
			{
				AgentDriveBinding agentDriveBinding = MacStepBindingForBrainDrive(goal);
				if ((object)agentDriveBinding != null)
				{
					lock (gate)
					{
						agentDriveEnded = agentDriveBinding.OnEnded;
						agentDriveClickyName = agentDriveBinding.ClickyName;
						pendingBilling = new ClickyDriveBilling.Job(agentDriveBinding.JobLeaseId);
						pendingAnalyticsThreadId = agentDriveBinding.ThreadId;
					}
				}
			}
			CancellationTokenSource cancellation;
			string threadId;
			JsonObject properties;
			lock (gate)
			{
				driveBilling = pendingBilling ?? ClickyDriveBilling.UserDrive;
				pendingBilling = null;
				driveIdentifier = Guid.NewGuid().ToString().ToUpperInvariant();
				driveAskingTurnTokens = ((driveBilling is ClickyDriveBilling.User) ? askingTurnTokens : new List<string>());
				askingTurnTokens = new List<string>();
				driveStartedAt = DateTimeOffset.Now;
				driveLeaseId = null;
				driveBillingSent = false;
				driveThen = ((agentDriveEnded == null) ? offeredThen : null);
				offeredThen = null;
				phase = new Phase(PhaseKind.Driving, goal);
				endSummary = null;
				actionLog.Clear();
				overlayStatus = "Getting started";
				loopCancellation?.Cancel();
				cancellation = (loopCancellation = new CancellationTokenSource());
				driveAnalyticsOrigin = pendingAnalyticsOrigin ?? "offer_card";
				driveAnalyticsThreadId = pendingAnalyticsThreadId;
				pendingAnalyticsOrigin = null;
				pendingAnalyticsThreadId = null;
				driveTakenBackBy = null;
				hasClickyActedThisDrive = false;
				userMousePauseCount = 0;
				threadId = driveAnalyticsThreadId;
				properties = ClickyDriveAnalytics.StartedProperties(driveIdentifier, driveAnalyticsOrigin, driveBilling, agentDriveEnded != null && !(driveBilling is ClickyDriveBilling.Setup), driveThen != null);
			}
			TrackDrive("started", threadId, properties);
			consentCard.Dismiss(animated: false);
			ClickyDriveState.IsDrivingNow = true;
			Log.Info("drive", "begin: " + goal);
			session.ClearDrawingForDrive();
			DisplayInfo displayInfo = WorkDisplay();
			overlay.Show(((object)displayInfo == null) ? Displays.UnderCursor() : MonitorFor(displayInfo), Accent(), "Getting started");
			PlayChime("agent-launch", 0.5);
			lock (gate)
			{
				movesIgnoredUntil = DateTimeOffset.Now + BeginMoveGrace;
			}
			StartWatchingUserInput();
			Task.Run(async delegate
			{
				_ = 1;
				try
				{
					await Task.Delay(300, cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
					await DriveAsync(goal).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex2)
				{
					Log.Error("drive", "drive failed", ex2);
				}
			});
		}
	}

	private async Task DriveAsync(string goal)
	{
		if (!IsDriving)
		{
			return;
		}
		IReadOnlyList<ScreenCaptureFrame> captures;
		try
		{
			captures = await capture.CaptureAllAtDisplayPointSizeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("drive", "capture failed: " + ex.Message);
			captures = Array.Empty<ScreenCaptureFrame>();
		}
		ScreenCaptureFrame screenCaptureFrame = CaptureForWork(captures, null);
		if ((object)screenCaptureFrame == null)
		{
			await InvokeOnUi(delegate
			{
				lock (gate)
				{
					driveEnding = AgentDriveEnding.CouldNotSee;
				}
				Finish("I can't see the screen right now.");
				return true;
			}).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		string clickyName;
		lock (gate)
		{
			stepCapture = screenCaptureFrame;
			stepDisplays = capture.GetDisplays();
			clickyName = agentDriveClickyName;
		}
		await session.RunDriveTurnAsync(ClickyDriveLines.DriveFact(goal, clickyName), screenCaptureFrame, stepDisplays).ConfigureAwait(continueOnCapturedContext: false);
		await InvokeOnUi(delegate
		{
			if (!IsDriving)
			{
				return false;
			}
			AgentDriveEnding agentDriveEnding;
			lock (gate)
			{
				agentDriveEnding = driveEnding;
			}
			if (agentDriveEnding == AgentDriveEnding.Failed)
			{
				Finish("I lost the connection partway, so I stopped there.", "agent-close");
			}
			else
			{
				Finish(null);
			}
			return true;
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public void NoteSpokenStep(string sentence)
	{
		if (IsDriving)
		{
			lock (gate)
			{
				overlayStatus = sentence;
			}
			OnUi(delegate
			{
				overlay.SetStatus(sentence);
			});
		}
	}

	public async Task<ScreenCaptureFrame?> PerformComputerActionsAsync(IReadOnlyList<JsonObject> actions, int audioPlaybackBeganBefore)
	{
		ScreenCaptureFrame frame;
		lock (gate)
		{
			lastCallResults.Clear();
			frame = stepCapture;
		}
		if (!IsDriving || (object)frame == null)
		{
			return null;
		}
		if (ClickyDriveActions.IsObservationOnly(actions))
		{
			foreach (JsonObject action in actions)
			{
				Log.Info("drive", ClickyDriveActions.Describe(action));
				await PerformAsync(action, frame).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (!IsDriving)
			{
				return null;
			}
			ScreenCaptureFrame screenCaptureFrame = CaptureForWork(await CaptureAllAsync().ConfigureAwait(continueOnCapturedContext: false), frame);
			lock (gate)
			{
				stepCapture = screenCaptureFrame;
			}
			return IsDriving ? screenCaptureFrame : null;
		}
		lock (gate)
		{
			if (phase.Kind == PhaseKind.Driving)
			{
				phase = phase with
				{
					Step = phase.Step + 1
				};
			}
		}
		DateTimeOffset lineDeadline = DateTimeOffset.Now.AddSeconds(1.6);
		while (session.AudioPlaybackBeganCount <= audioPlaybackBeganBefore && DateTimeOffset.Now < lineDeadline)
		{
			await Task.Delay(50).ConfigureAwait(continueOnCapturedContext: false);
		}
		await WaitForUserMouseToRestAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (!IsDriving)
		{
			return null;
		}
		lock (gate)
		{
			actuatingUntil = DateTimeOffset.Now.AddSeconds(30.0);
			hasClickyActedThisDrive = true;
		}
		OnUi(delegate
		{
			overlay.SetActing(acting: true);
		});
		try
		{
			foreach (JsonObject action2 in actions)
			{
				if (!IsDriving)
				{
					return null;
				}
				Log.Info("drive", ClickyDriveActions.Describe(action2));
				await PerformAsync(action2, frame).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			lock (gate)
			{
				actuatingUntil = DateTimeOffset.Now.AddSeconds(0.3);
				userMouse.ForgetRun();
			}
			OnUi(delegate
			{
				overlay.SetActing(acting: false);
			});
		}
		if (!IsDriving)
		{
			return null;
		}
		RectD settleDisplay = frame.Display.BoundsPx;
		try
		{
			await ScreenSettle.WaitForScreenToSettleAsync(async delegate
			{
				IReadOnlyList<ScreenCaptureFrame> source = await capture.CaptureAllAsync(256, 60).ConfigureAwait(continueOnCapturedContext: false);
				return (source.FirstOrDefault((ScreenCaptureFrame s) => s.Display.BoundsPx == settleDisplay) ?? source.FirstOrDefault())?.Jpeg;
			}, TimeSpan.FromMilliseconds(600L, 0L), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("drive", "settle: " + ex.Message);
		}
		if (!IsDriving)
		{
			return null;
		}
		ScreenCaptureFrame result = CaptureForWork(await CaptureAllAsync().ConfigureAwait(continueOnCapturedContext: false), frame);
		lock (gate)
		{
			stepCapture = result;
		}
		return result;
	}

	private async Task<IReadOnlyList<ScreenCaptureFrame>> CaptureAllAsync()
	{
		try
		{
			return await capture.CaptureAllAtDisplayPointSizeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("drive", "capture failed: " + ex.Message);
			return Array.Empty<ScreenCaptureFrame>();
		}
	}

	private void NoteCallResult(JsonObject action, JsonObject fields)
	{
		double? num = ClickyDriveActions.Number(action, "claude_call_index");
		if (!num.HasValue)
		{
			return;
		}
		double valueOrDefault = num.GetValueOrDefault();
		int callIndex = (int)valueOrDefault;
		lock (gate)
		{
			if (!lastCallResults.Any((JsonObject r) => (int?)r["call_index"] == callIndex))
			{
				fields["call_index"] = callIndex;
				lastCallResults.Add(fields);
			}
		}
	}

	private void NoteRefused(JsonObject action)
	{
		Log.Warn("drive", ClickyDriveActions.TypeOf(action) + ": the window in front refused HeyClicky's input, so it stopped");
		NoteCallResult(action, new JsonObject((JsonNodeOptions?)null)
		{
			["text"] = "The window in front refuses HeyClicky's input (an app running as administrator), so this stopped. No key was left held down.",
			["error"] = true
		});
	}

	private void AddToActionLog(string line)
	{
		lock (gate)
		{
			actionLog.Add(line);
		}
	}

	private async Task PerformAsync(JsonObject action, ScreenCaptureFrame frame)
	{
		string type = ClickyDriveActions.TypeOf(action);
		ClickyModifierKeys modifiers = ClickyModifierKeys.None;
		ClickyModifierKeys valueOrDefault = default(ClickyModifierKeys);
		foreach (string item in ClickyDriveActions.Keys(action))
		{
			ClickyModifierKeys? clickyModifierKeys = ClickyKeyNames.Modifier(item);
			int num;
			if (clickyModifierKeys.HasValue)
			{
				valueOrDefault = clickyModifierKeys.GetValueOrDefault();
				num = 1;
			}
			else
			{
				num = 0;
			}
			bool flag = (byte)num != 0;
			if (flag)
			{
				bool flag2;
				switch (item)
				{
				case "cmd":
				case "ctrl":
				case "opt":
				case "shift":
				case "win":
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				modifiers |= valueOrDefault;
			}
		}
		switch (type)
		{
		case "double_click":
		case "triple_click":
		case "move":
		case "click":
		{
			PointD? pointD = Point(ClickyDriveActions.Number(action, "x"), ClickyDriveActions.Number(action, "y")) ?? ((type == "move") ? ((PointD?)null) : new PointD?(ClickyScreenActuator.CurrentPointer));
			if (!pointD.HasValue)
			{
				return;
			}
			PointD point = pointD.GetValueOrDefault();
			await ClickyScreenActuator.GlideAsync(point).ConfigureAwait(continueOnCapturedContext: false);
			if (IsDriving && !(type == "move"))
			{
				string text = (((string?)action["button"]) ?? "left").ToLowerInvariant();
				int count = ((type == "triple_click") ? 3 : ((!(type == "double_click")) ? 1 : 2));
				ClickyMouseButton button = ((text == "right") ? ClickyMouseButton.Right : ((text == "middle") ? ClickyMouseButton.Middle : ClickyMouseButton.Left));
				if (await ClickyScreenActuator.ClickAsync(point, button, count, modifiers).ConfigureAwait(continueOnCapturedContext: false) == ClickyActuation.Refused)
				{
					NoteRefused(action);
				}
				else
				{
					AddToActionLog(ClickyDriveActions.ClickLogLine(type, action));
				}
			}
			break;
		}
		case "drag":
		{
			List<PointD> path = (from step in (action["path"] as JsonArray)?.OfType<JsonObject>()
				select Point(ClickyDriveActions.Number(step, "x"), ClickyDriveActions.Number(step, "y")) into p
				where p.HasValue
				select p.Value).ToList() ?? new List<PointD>();
			if (path.Count < 2)
			{
				return;
			}
			await ClickyScreenActuator.GlideAsync(path[0]).ConfigureAwait(continueOnCapturedContext: false);
			if (!IsDriving)
			{
				return;
			}
			if (await ClickyScreenActuator.DragAsync(path).ConfigureAwait(continueOnCapturedContext: false) == ClickyActuation.Refused)
			{
				NoteRefused(action);
			}
			else
			{
				AddToActionLog("drag");
			}
			break;
		}
		case "scroll":
		{
			PointD? pointD2 = Point(ClickyDriveActions.Number(action, "x"), ClickyDriveActions.Number(action, "y"));
			if (pointD2.HasValue)
			{
				PointD valueOrDefault3 = pointD2.GetValueOrDefault();
				await ClickyScreenActuator.GlideAsync(valueOrDefault3, 0.3).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (await ClickyScreenActuator.ScrollAsync(ClickyDriveActions.ScrollLines(ClickyDriveActions.Number(action, "scroll_y").GetValueOrDefault())).ConfigureAwait(continueOnCapturedContext: false) == ClickyActuation.Refused)
			{
				NoteRefused(action);
			}
			else
			{
				AddToActionLog("scroll");
			}
			break;
		}
		case "type":
		{
			string text2 = (string?)action["text"];
			if (text2 != null && text2.Length > 0)
			{
				CancellationToken ct = loopCancellation?.Token ?? new CancellationToken(canceled: true);
				if (await ClickyScreenActuator.TypeAsync(text2, ct).ConfigureAwait(continueOnCapturedContext: false) == ClickyActuation.Refused)
				{
					NoteRefused(action);
				}
				else
				{
					AddToActionLog(ClickyDriveActions.TypedLogLine(text2));
				}
			}
			break;
		}
		case "keypress":
		{
			List<string> list2 = ClickyDriveActions.Keys(action);
			string chord = string.Join("+", list2);
			switch ((list2.Count <= 0) ? ClickyActuation.UnknownKey : (await ClickyScreenActuator.PressAsync(chord).ConfigureAwait(continueOnCapturedContext: false)))
			{
			case ClickyActuation.Done:
				AddToActionLog("pressed " + chord);
				break;
			case ClickyActuation.Refused:
				NoteRefused(action);
				break;
			default:
				NoteCallResult(action, new JsonObject((JsonNodeOptions?)null)
				{
					["text"] = "Unknown key: " + chord + ". Nothing was pressed.",
					["error"] = true
				});
				break;
			}
			break;
		}
		case "wait":
			await Task.Delay(1000).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case "mouse_up":
		case "mouse_down":
			if (ClickyScreenActuator.SetLeftButton(type == "mouse_down") == ClickyActuation.Refused)
			{
				NoteRefused(action);
			}
			else
			{
				AddToActionLog((type == "mouse_down") ? "mouse down" : "mouse up");
			}
			break;
		case "hold_key":
		{
			List<string> list = ClickyDriveActions.Keys(action);
			string chord = string.Join("+", list);
			switch (await ClickyScreenActuator.HoldAsync(list, ClickyDriveActions.Number(action, "seconds") ?? 1.0).ConfigureAwait(continueOnCapturedContext: false))
			{
			case ClickyActuation.Done:
				AddToActionLog("held " + chord);
				break;
			case ClickyActuation.Refused:
				NoteRefused(action);
				break;
			default:
				NoteCallResult(action, new JsonObject((JsonNodeOptions?)null)
				{
					["text"] = "Unknown key: " + chord + ". Nothing was held.",
					["error"] = true
				});
				break;
			}
			break;
		}
		case "cursor_position":
			NoteCallResult(action, new JsonObject((JsonNodeOptions?)null) { ["text"] = ClickyDriveActions.CursorPositionText(ClickyScreenActuator.CurrentPointer, frame) });
			break;
		case "zoom":
		{
			RectD? rectD = ClickyDriveActions.ZoomRegionPx(action, frame);
			if (rectD.HasValue)
			{
				RectD valueOrDefault2 = rectD.GetValueOrDefault();
				try
				{
					var (inArray, value, value2) = await capture.CaptureRegionAsync(frame.Display, valueOrDefault2).ConfigureAwait(continueOnCapturedContext: false);
					NoteCallResult(action, new JsonObject((JsonNodeOptions?)null) { ["image_url"] = "data:image/jpeg;base64," + Convert.ToBase64String(inArray) });
					Log.Info("drive", $"zoomed → {value}×{value2}");
				}
				catch (Exception ex)
				{
					NoteCallResult(action, new JsonObject((JsonNodeOptions?)null)
					{
						["text"] = "Zoom failed: " + ex.Message,
						["error"] = true
					});
				}
			}
			else
			{
				NoteCallResult(action, new JsonObject((JsonNodeOptions?)null)
				{
					["text"] = "Zoom needs [x1, y1, x2, y2].",
					["error"] = true
				});
			}
			break;
		}
		}
		await Task.Delay(120).ConfigureAwait(continueOnCapturedContext: false);
		PointD? Point(double? x, double? y)
		{
			if (x.HasValue)
			{
				double valueOrDefault4 = x.GetValueOrDefault();
				if (y.HasValue)
				{
					double valueOrDefault5 = y.GetValueOrDefault();
					return ClickyDriveActions.ScreenPoint(valueOrDefault4, valueOrDefault5, frame);
				}
			}
			return null;
		}
	}

	public void StopForSafetyCheck(IReadOnlyList<JsonObject> checks)
	{
		OnUi(delegate
		{
			if (IsDriving)
			{
				string text = ((string?)checks.FirstOrDefault()?["message"]) ?? "something on screen needs your decision";
				Log.Info("drive", "safety check: " + text);
				lock (gate)
				{
					driveEnding = AgentDriveEnding.SafetyStop;
				}
				Finish(ClickyDriveLines.SafetyStop(text), "agent-close");
			}
		});
	}

	public bool HandleEscape()
	{
		if (CurrentPhase.Kind == PhaseKind.Idle)
		{
			return false;
		}
		OnUi(delegate
		{
			switch (CurrentPhase.Kind)
			{
			case PhaseKind.Driving:
				StopForUser("escape");
				break;
			case PhaseKind.Offered:
				consentCard.Dismiss();
				Declined("no_escape");
				break;
			}
		});
		return true;
	}

	private void StartWatchingUserInput()
	{
		userInputWatch?.Dispose();
		userInputWatch = new ClickyTakeBackWatch(delegate(ClickyTakeBackWatch.Kind kind, PointD pointer)
		{
			if (IsDriving)
			{
				bool nudge = false;
				bool flag = false;
				lock (gate)
				{
					DateTimeOffset now = DateTimeOffset.Now;
					if (now < actuatingUntil)
					{
						return;
					}
					ClickyTakeBackWatch.Kind kind2 = kind;
					bool flag2 = (uint)kind2 <= 1u;
					if (flag2 && now < movesIgnoredUntil)
					{
						return;
					}
					if (kind == ClickyTakeBackWatch.Kind.Move)
					{
						double num = Math.Max(0.5, Displays.At((int)pointer.X, (int)pointer.Y).Scale);
						ClickyDriveUserMouse.Verdict verdict = userMouse.NoteMove(pointer.X / num, pointer.Y / num, now, hasClickyActedThisDrive);
						if (!verdict.TakeOver)
						{
							nudge = verdict.Nudge;
							flag = isShowingUserMouseHint;
							if (!nudge && !flag)
							{
								return;
							}
						}
					}
				}
				if (nudge || flag)
				{
					OnUi(delegate
					{
						ShowUserMouseHint(nudge);
					});
				}
				else
				{
					OnUi(delegate
					{
						StopForUser(TakeBackReason(kind));
					});
				}
			}
		});
	}

	private static string TakeBackReason(ClickyTakeBackWatch.Kind kind)
	{
		return kind switch
		{
			ClickyTakeBackWatch.Kind.Move => "mouse_move", 
			ClickyTakeBackWatch.Kind.Press => "click", 
			ClickyTakeBackWatch.Kind.Key => "key", 
			ClickyTakeBackWatch.Kind.Wheel => "scroll", 
			_ => "other", 
		};
	}

	private void ShowUserMouseHint(bool nudge)
	{
		if (!IsDriving)
		{
			return;
		}
		if (nudge)
		{
			bool flag;
			lock (gate)
			{
				flag = !isShowingUserMouseHint;
				isShowingUserMouseHint = true;
				if (flag)
				{
					userMousePauseCount++;
				}
			}
			if (flag)
			{
				Log.Info("drive", "the user moved the mouse: waiting for it to rest");
			}
			overlay.SetWaitingForUserMouse(waiting: true, nudge: true);
		}
		userMouseHintLinger?.Cancel();
		CancellationTokenSource linger = (userMouseHintLinger = new CancellationTokenSource());
		Task.Delay(ClickyDriveUserMouse.HintLinger, linger.Token).ContinueWith(delegate(Task task)
		{
			if (!task.IsCanceled)
			{
				OnUi(delegate
				{
					if (!linger.IsCancellationRequested)
					{
						lock (gate)
						{
							if (!isShowingUserMouseHint)
							{
								return;
							}
							isShowingUserMouseHint = false;
						}
						overlay.SetWaitingForUserMouse(waiting: false, nudge: false);
					}
				});
			}
		}, TaskScheduler.Default);
	}

	private async Task WaitForUserMouseToRestAsync()
	{
		DateTimeOffset waitStart = DateTimeOffset.Now;
		while (IsDriving)
		{
			bool flag;
			lock (gate)
			{
				flag = userMouse.IsResting(DateTimeOffset.Now);
			}
			if (flag)
			{
				break;
			}
			if (DateTimeOffset.Now - waitStart >= ClickyDriveUserMouse.WaitCap)
			{
				await InvokeOnUi(delegate
				{
					StopForUser("mouse_move");
					return true;
				}).ConfigureAwait(continueOnCapturedContext: false);
				break;
			}
			await Task.Delay(100).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void StopWatchingUserInput()
	{
		userInputWatch?.Dispose();
		userInputWatch = null;
		userMouseHintLinger?.Cancel();
		userMouseHintLinger = null;
		lock (gate)
		{
			userMouse.Reset();
			isShowingUserMouseHint = false;
		}
	}

	public void StopBecauseUserSpoke()
	{
		if (CurrentPhase.Kind == PhaseKind.Idle)
		{
			return;
		}
		OnUi(delegate
		{
			switch (CurrentPhase.Kind)
			{
			case PhaseKind.Offered:
				TrackOfferAnswered("dismissed_by_voice");
				consentCard.Dismiss();
				lock (gate)
				{
					offerAnsweredBySpeaking = (phase.Goal, DateTimeOffset.Now);
					phase = new Phase(PhaseKind.Idle);
					break;
				}
			case PhaseKind.Driving:
				Log.Info("drive", "the user started talking: handing back");
				lock (gate)
				{
					driveEnding = AgentDriveEnding.UserTookOver;
					driveTakenBackBy = "voice";
				}
				session.InterruptForDrive();
				Teardown();
				break;
			}
		});
	}

	private void StopForUser(string reason)
	{
		if (IsDriving)
		{
			Log.Info("drive", "the user took back control (" + reason + ")");
			lock (gate)
			{
				driveEnding = AgentDriveEnding.UserTookOver;
				driveTakenBackBy = reason;
			}
			session.InterruptForDrive();
			PlayChime("agent-close", 0.5);
			Teardown();
			session.SpeakVerbatimLineAsync("Okay, it's all yours.");
		}
	}

	private void Finish(string? line, string chime = "agent-done")
	{
		if (IsDriving)
		{
			Log.Info("drive", "finished" + ((line == null) ? "" : (": " + line)));
			PlayChime(chime, 0.55);
			Teardown();
			if (line != null)
			{
				session.SpeakVerbatimLineAsync(line);
			}
		}
	}

	private void Teardown()
	{
		ClickyScreenActuator.ReleaseHeldButton();
		ClickyScreenActuator.ReleaseStrandedKeysAfterDrive("the drive ended");
		int stepCount;
		string driveId;
		string origin;
		string threadId;
		string takenBackBy;
		int mousePauses;
		AgentDriveEnding agentDriveEnding;
		ClickyDriveBilling clickyDriveBilling;
		string text;
		DateTimeOffset dateTimeOffset;
		Action<AgentDriveOutcome> action;
		string text2;
		string text3;
		List<string> list;
		lock (gate)
		{
			stepCount = ((phase.Kind == PhaseKind.Driving) ? phase.Step : 0);
			driveId = driveIdentifier;
			origin = driveAnalyticsOrigin;
			threadId = driveAnalyticsThreadId;
			takenBackBy = driveTakenBackBy;
			mousePauses = userMousePauseCount;
			driveAnalyticsThreadId = null;
			driveTakenBackBy = null;
			lastTakeOverEndedAt = DateTimeOffset.Now;
			loopCancellation?.Cancel();
			loopCancellation = null;
			phase = new Phase(PhaseKind.Idle);
			stepCapture = null;
			targetProcessName = null;
			agentDriveEnding = driveEnding;
			driveEnding = AgentDriveEnding.Finished;
			clickyDriveBilling = driveBilling;
			text = driveLeaseId;
			dateTimeOffset = driveStartedAt;
			driveLeaseId = null;
			driveAskingTurnTokens = new List<string>();
			driveBilling = ClickyDriveBilling.UserDrive;
			agentDriveClickyName = null;
			action = agentDriveEnded;
			agentDriveEnded = null;
			text2 = driveThen;
			driveThen = null;
			text3 = endSummary;
			list = actionLog.ToList();
		}
		ClickyDriveState.IsDrivingNow = false;
		StopWatchingUserInput();
		overlay.Hide();
		session.SettleAfterDrive();
		TimeSpan timeSpan = DateTimeOffset.Now - dateTimeOffset;
		bool flag = agentDriveEnding == AgentDriveEnding.UserTookOver && text != null && timeSpan <= TimeSpan.FromSeconds(10L);
		if (flag)
		{
			RefundAndRefreshAsync(text);
		}
		else if (!(clickyDriveBilling is ClickyDriveBilling.Setup))
		{
			RefreshBilling?.Invoke();
		}
		TrackDrive("ended", threadId, ClickyDriveAnalytics.EndedProperties(driveId, origin, clickyDriveBilling, agentDriveEnding, timeSpan.TotalSeconds, list.Count, stepCount, text3 != null, action != null && !(clickyDriveBilling is ClickyDriveBilling.Setup), flag, takenBackBy, mousePauses));
		if (action != null)
		{
			try
			{
				action(new AgentDriveOutcome(agentDriveEnding, text3, list));
			}
			catch (Exception ex)
			{
				Log.Warn("drive", "drive outcome handler failed: " + ex.Message);
			}
		}
		if (agentDriveEnding == AgentDriveEnding.Finished && text2 != null)
		{
			session.SubmitFactAsync(ClickyDriveLines.ThenFact(text3, text2));
		}
	}

	private async Task RefundAndRefreshAsync(string leaseId)
	{
		try
		{
			Func<string, Task> refundEarlyStop = RefundEarlyStop;
			if (refundEarlyStop != null)
			{
				await refundEarlyStop(leaseId).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("drive", "early-stop refund failed: " + ex.Message);
			ClickyAnalytics.ReportInternalError("clicky_drive", "early_stop_refund", ex, null, CrashReportLevel.Warning);
		}
		Func<Task> refreshBilling = RefreshBilling;
		if (refreshBilling != null)
		{
			await refreshBilling().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void OnUi(Action action)
	{
		if (ui.HasThreadAccess)
		{
			action();
			return;
		}
		ui.TryEnqueue(delegate
		{
			action();
		});
	}

	private Task<T> InvokeOnUi<T>(Func<T> action)
	{
		if (ui.HasThreadAccess)
		{
			return Task.FromResult(action());
		}
		TaskCompletionSource<T> done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!ui.TryEnqueue(delegate
		{
			try
			{
				done.SetResult(action());
			}
			catch (Exception exception)
			{
				done.SetException(exception);
			}
		}))
		{
			done.SetCanceled();
		}
		return done.Task;
	}
}
