using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Brain;
using HeyClicky.Core.Drive;
using HeyClicky.Core.Identity;
using HeyClicky.Core.Net;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Platform;
using HeyClicky.Core.Proactive;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Tools;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Voice;

public sealed class CompanionSession : ICompanionSession, IAsyncDisposable
{
	private sealed class SilentAudioOutput(int sampleRate) : IAudioOutput, IDisposable
	{
		public int SampleRate => sampleRate;

		public TimeSpan Buffered => TimeSpan.Zero;

		public double PlayedSeconds => 0.0;

		public float Level => 0f;

		public void Enqueue(ReadOnlySpan<byte> pcm16Mono)
		{
		}

		public void Flush()
		{
		}

		public void PlayEarcon(string name)
		{
		}

		public void Dispose()
		{
		}
	}

	internal sealed record TurnCapture(IReadOnlyList<ScreenCaptureFrame> Frames, IReadOnlyList<DisplayInfo> Displays, string? HighlightGesture, int CaptureMs, string? HighlightText = null)
	{
		public IReadOnlyList<ScreenCaptureFrame>? FramesWithoutInk { get; init; }

		public ScreenCaptureFrame WithoutUserInk(ScreenCaptureFrame frame)
		{
			IReadOnlyList<ScreenCaptureFrame> framesWithoutInk = FramesWithoutInk;
			if (framesWithoutInk == null)
			{
				return frame;
			}
			for (int i = 0; i < Frames.Count && i < framesWithoutInk.Count; i++)
			{
				if ((object)Frames[i] == frame)
				{
					return framesWithoutInk[i];
				}
			}
			return frame;
		}
	}

	internal sealed record TurnRequest(string? Words, string? Fact, bool Typed, bool AnswersInText, TurnCapture? Capture, (string? DisplayName, string? ProcessName) FrontApp, double? AudioSeconds, string Trigger, long StartedTicks, int TranscriptMs, int? WalkthroughStep = null)
	{
		public IReadOnlyList<string> AttachedFilePaths { get; init; } = Array.Empty<string>();

		public bool IsDrive { get; init; }

		public bool Screenless { get; init; }

		public bool PointsOnly { get; init; }

		public JsonObject? FlowBrief { get; init; }

		public string? Flow
		{
			get
			{
				bool flag = WalkthroughStep.HasValue;
				if (!flag)
				{
					string trigger = Trigger;
					bool flag2 = ((trigger == "clicked_target" || trigger == "did_step") ? true : false);
					flag = flag2;
				}
				if (!flag)
				{
					return null;
				}
				return "walkthrough";
			}
		}
	}

	internal sealed class NarrationState
	{
		public readonly object Gate = new object();

		public List<string> Lines = new List<string>();

		public List<DrawingAction> Actions = new List<DrawingAction>();

		public string ProgramSoFar = "";

		public bool Finished;

		public string? Failure;

		public string? StatusLabel;

		public string? RunningToolName;

		public bool IsSearchingTheWeb;

		public bool IsReadingConnectedApps;

		public List<string> HeardLines = new List<string>();

		public int? FirstSayLineMs;

		public int FirstLine;

		public bool IsContinuationDecided = true;

		public Func<Task>? StartOver;

		public int DisplayCount = 1;

		public bool IsDisplayDecided = true;

		public Func<int, Task>? MoveToDisplay;

		public bool DrewOnOtherDisplay;

		public string? EarlyFirstLine;

		public int Marks;

		public bool WriteOnly;

		public bool ReadConnectedApps;

		public CompanionToolContext? ToolContext;

		public bool TargetArmed;

		public int TextDeliveries;

		public long? FirstLineHeardTicks;

		public string? Model;

		public bool HeldForAdoption;

		public string? LineText(int index)
		{
			lock (Gate)
			{
				if (Lines.Count > index)
				{
					return Lines[index];
				}
				return (index == FirstLine) ? EarlyFirstLine : null;
			}
		}
	}

	internal sealed record TurnBodyParts(List<JsonNode> Thread, List<JsonNode> TurnItems, string SessionId, string? TurnToken, string? UserLayer, string? UserLayerSignature, bool SendEnvironment, string? Words, string? Fact, string TimeLabel, string? FrontApp, string? FrontAppProcess, string? HighlightGesture, bool Typed, string LiveState, string ScreenshotBase64, ScreenCaptureFrame Screenshot, List<ScreenCaptureFrame> OtherScreens, string? StableNote, double? AudioSeconds, string Trigger, string? DrawingCode, string? DrawingOnScreen = null, int? WalkthroughStep = null, string? Flow = null, string? BrainModel = null, bool AppReads = false)
	{
		public bool OffersDrive { get; init; }

		public bool IsDrive { get; init; }

		public JsonObject? DriveBillingFields { get; init; }

		public PageContext? Page { get; init; }

		public bool PageAccessNoted { get; init; } = true;

		public IReadOnlyList<byte[]> AttachedImages { get; init; } = Array.Empty<byte[]>();

		public BrainUserContext UserContext { get; init; } = BrainUserContext.Empty;

		public string? HighlightText { get; init; }

		public JsonObject? FlowBrief { get; init; }

		public bool Screenless { get; init; }

		public BrainRequestFlags Flags { get; init; } = BrainRequestFlags.None;
	}

	internal sealed class CanvasFeeder(CompanionSession owner, NarrationState narration, Func<bool> stillCurrent) : IProgramFeeder
	{
		private readonly object feedGate = new object();

		private string? pendingCode;

		private bool pendingDone;

		private Task running = Task.CompletedTask;

		public void Push(string code, bool done)
		{
			lock (feedGate)
			{
				pendingCode = code;
				pendingDone |= done;
				if (running.IsCompleted)
				{
					running = Task.Run((Func<Task?>)LoopAsync);
				}
			}
		}

		public async Task DrainAsync()
		{
			Task task;
			lock (feedGate)
			{
				task = running;
			}
			await task.ConfigureAwait(continueOnCapturedContext: false);
			lock (feedGate)
			{
				task = running;
			}
			await task.ConfigureAwait(continueOnCapturedContext: false);
		}

		private async Task LoopAsync()
		{
			while (true)
			{
				string code;
				bool done;
				lock (feedGate)
				{
					if (pendingCode == null)
					{
						break;
					}
					code = pendingCode;
					done = pendingDone;
					pendingCode = null;
				}
				if (!stillCurrent())
				{
					continue;
				}
				if (!narration.IsDisplayDecided)
				{
					int num = DrawingDisplayChoices.Decide(code, done, narration.DisplayCount);
					if (num < 0)
					{
						continue;
					}
					narration.IsDisplayDecided = true;
					if (num > 0)
					{
						Func<int, Task> moveToDisplay = narration.MoveToDisplay;
						if (moveToDisplay != null)
						{
							try
							{
								await moveToDisplay(num).ConfigureAwait(continueOnCapturedContext: false);
							}
							catch (OperationCanceledException)
							{
								break;
							}
						}
					}
				}
				if (!narration.IsContinuationDecided)
				{
					DrawingContinuationDecision drawingContinuationDecision = DrawingContinuationDecisions.Decide(code, done);
					if (drawingContinuationDecision == DrawingContinuationDecision.Undecided)
					{
						continue;
					}
					narration.IsContinuationDecided = true;
					if (drawingContinuationDecision == DrawingContinuationDecision.StartOver)
					{
						Func<Task> startOver = narration.StartOver;
						if (startOver != null)
						{
							try
							{
								await startOver().ConfigureAwait(continueOnCapturedContext: false);
							}
							catch (OperationCanceledException)
							{
								break;
							}
						}
					}
				}
				lock (narration.Gate)
				{
					narration.ProgramSoFar = code;
				}
				if (narration.WriteOnly)
				{
					continue;
				}
				int? firstSayLineMs = narration.FirstSayLineMs;
				if (!firstSayLineMs.HasValue)
				{
					string text = ProgramText.FirstSayLiteral(code);
					if (text != null)
					{
						lock (narration.Gate)
						{
							if (narration.Lines.Count <= narration.FirstLine)
							{
								narration.EarlyFirstLine = text;
								narration.FirstSayLineMs = (int)(Environment.TickCount64 - owner.turnStartedTicks);
							}
						}
						owner.SignalNarration();
					}
				}
				try
				{
					DrawingFeedResult drawingFeedResult = await owner.ui.InvokeAsync(() => owner.canvas.Feed(code, done)).ConfigureAwait(continueOnCapturedContext: false);
					lock (narration.Gate)
					{
						if (drawingFeedResult.Lines.Count >= narration.Lines.Count)
						{
							narration.Lines = drawingFeedResult.Lines.ToList();
						}
						if (drawingFeedResult.Actions.Count >= narration.Actions.Count)
						{
							narration.Actions = drawingFeedResult.Actions.ToList();
						}
						narration.Marks = Math.Max(narration.Marks, drawingFeedResult.MarkCount);
						firstSayLineMs = narration.FirstSayLineMs;
						if (!firstSayLineMs.HasValue && narration.Lines.Count > narration.FirstLine)
						{
							narration.FirstSayLineMs = (int)(Environment.TickCount64 - owner.turnStartedTicks);
						}
					}
				}
				catch (Exception ex3)
				{
					Log.Error("Companion", "canvas feed failed", ex3);
				}
				owner.SignalNarration();
			}
		}
	}

	private sealed class TurnTimer(long startedTicks)
	{
		public int? BrainFirstByteMs;

		public int? FirstSayLineMs;

		public int? FirstSpokenMs;

		public string? Model;

		public int? RequestSentMs;

		public int? FirstTextMs;

		public int? FirstCodeMs;

		public int? FirstLineToMouthMs;

		private int Elapsed => (int)(Environment.TickCount64 - startedTicks);

		public void MarkBrainFirstByte()
		{
			int valueOrDefault = BrainFirstByteMs.GetValueOrDefault();
			if (!BrainFirstByteMs.HasValue)
			{
				valueOrDefault = Elapsed;
				BrainFirstByteMs = valueOrDefault;
			}
		}

		public void MarkFirstSpoken()
		{
			int valueOrDefault = FirstSpokenMs.GetValueOrDefault();
			if (!FirstSpokenMs.HasValue)
			{
				valueOrDefault = Elapsed;
				FirstSpokenMs = valueOrDefault;
			}
		}

		public void MarkRequestSent()
		{
			int valueOrDefault = RequestSentMs.GetValueOrDefault();
			if (!RequestSentMs.HasValue)
			{
				valueOrDefault = Elapsed;
				RequestSentMs = valueOrDefault;
			}
		}

		public void MarkFirstText()
		{
			int valueOrDefault = FirstTextMs.GetValueOrDefault();
			if (!FirstTextMs.HasValue)
			{
				valueOrDefault = Elapsed;
				FirstTextMs = valueOrDefault;
			}
		}

		public void MarkFirstCode()
		{
			int valueOrDefault = FirstCodeMs.GetValueOrDefault();
			if (!FirstCodeMs.HasValue)
			{
				valueOrDefault = Elapsed;
				FirstCodeMs = valueOrDefault;
			}
		}

		public void MarkFirstLineToMouth()
		{
			int valueOrDefault = FirstLineToMouthMs.GetValueOrDefault();
			if (!FirstLineToMouthMs.HasValue)
			{
				valueOrDefault = Elapsed;
				FirstLineToMouthMs = valueOrDefault;
			}
		}

		public CompanionTurnMetrics ToMetrics(bool typed, int transcriptMs, int capture, int? firstAudio)
		{
			return new CompanionTurnMetrics(typed, transcriptMs, capture, BrainFirstByteMs, FirstSayLineMs, firstAudio, FirstSpokenMs, (int)(Environment.TickCount64 - startedTicks), Model);
		}

		public string Describe(int? firstAudio)
		{
			return $"request sent {RequestSentMs?.ToString() ?? "-"} ms, brain first byte {BrainFirstByteMs?.ToString() ?? "-"} ms, first text {FirstTextMs?.ToString() ?? "-"} ms, first code {FirstCodeMs?.ToString() ?? "-"} ms, first say() {FirstSayLineMs?.ToString() ?? "-"} ms, line 1 to mouth {FirstLineToMouthMs?.ToString() ?? "-"} ms, first audio {firstAudio?.ToString() ?? "-"} ms, first spoken {FirstSpokenMs?.ToString() ?? "-"} ms, total {Environment.TickCount64 - startedTicks} ms ({Model ?? "?"})";
		}
	}

	private sealed record HeldActions(string SessionId, List<BrainToolCall> Calls, TurnRequest Request, CompanionToolContext Context);

	private sealed class LessonRecording
	{
		public readonly string Question = question;

		public readonly ScreenCaptureFrame Screen = screen;

		public readonly string Color = color;

		public readonly bool SavesDrawing = savesDrawing || keepsInChat;

		public readonly bool KeepsInChat = keepsInChat;

		public readonly long StartedTicks = Environment.TickCount64;

		public readonly Dictionary<int, double> Starts = new Dictionary<int, double>();

		public readonly Dictionary<int, double> Lengths = new Dictionary<int, double>();

		public readonly Dictionary<int, MemoryStream> Audio = new Dictionary<int, MemoryStream>();

		public LessonRecording(string question, ScreenCaptureFrame screen, string color, bool savesDrawing, bool keepsInChat)
		{
		}
	}

	public sealed class PrefetchedTurn
	{
		internal readonly string Fact;

		internal readonly TurnCapture Capture;

		internal readonly string AccessToken;

		internal readonly (string? DisplayName, string? ProcessName) FrontApp;

		internal TurnRequest? Request;

		internal readonly NarrationState Narration = new NarrationState
		{
			HeldForAdoption = true
		};

		internal readonly HeldFeeder Feeder;

		internal Task Streaming = Task.CompletedTask;

		private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

		private readonly TaskCompletionSource<bool> commitGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		private volatile Func<bool>? playingTurnIsCurrent;

		private volatile bool isAdopted;

		private volatile bool isDropped;

		public string? AccountId { get; }

		public DateTimeOffset StartedAt { get; internal set; }

		public int CodeCharactersReady => Feeder.LatestLength;

		public bool IsAdopted => isAdopted;

		public bool IsDropped => isDropped;

		public bool DidFail
		{
			get
			{
				lock (Narration.Gate)
				{
					return Narration.Failure != null;
				}
			}
		}

		public string? Failure
		{
			get
			{
				lock (Narration.Gate)
				{
					return Narration.Failure;
				}
			}
		}

		internal CancellationToken Token => cancellation.Token;

		internal Task<bool> CommitGate => commitGate.Task;

		internal PrefetchedTurn(string fact, TurnCapture capture, string accessToken, (string? DisplayName, string? ProcessName) frontApp, string? accountId)
		{
			Fact = fact;
			Capture = capture;
			AccessToken = accessToken;
			FrontApp = frontApp;
			AccountId = accountId;
			Feeder = new HeldFeeder(cancellation.Token);
		}

		internal bool StillCurrent()
		{
			if (!cancellation.IsCancellationRequested)
			{
				return playingTurnIsCurrent?.Invoke() ?? true;
			}
			return false;
		}

		internal void MarkAdopted()
		{
			isAdopted = true;
			commitGate.TrySetResult(result: true);
		}

		internal void Drop()
		{
			isDropped = true;
			commitGate.TrySetResult(result: false);
			cancellation.Cancel();
		}

		internal void Cancel()
		{
			cancellation.Cancel();
		}

		internal async Task PlayAsync(CompanionSession session, CanvasFeeder feeder, Func<bool> stillCurrent, CancellationToken ct)
		{
			playingTurnIsCurrent = stillCurrent;
			using (ct.Register(cancellation.Cancel))
			{
				string statusLabel;
				lock (Narration.Gate)
				{
					Narration.HeldForAdoption = false;
					statusLabel = Narration.StatusLabel;
				}
				if (statusLabel != null)
				{
					session.ShowWaitStatusLabel(statusLabel);
				}
				Feeder.Attach(feeder);
				await Streaming.ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	internal interface IProgramFeeder
	{
		void Push(string code, bool done);

		Task DrainAsync();
	}

	internal sealed class HeldFeeder(CancellationToken dropped) : IProgramFeeder
	{
		private readonly object feedGate = new object();

		private string? latestCode;

		private bool latestDone;

		private CanvasFeeder? target;

		private readonly TaskCompletionSource<CanvasFeeder> attached = new TaskCompletionSource<CanvasFeeder>(TaskCreationOptions.RunContinuationsAsynchronously);

		public int LatestLength
		{
			get
			{
				lock (feedGate)
				{
					return latestCode?.Length ?? 0;
				}
			}
		}

		public void Push(string code, bool done)
		{
			lock (feedGate)
			{
				latestCode = code;
				latestDone |= done;
				target?.Push(code, done);
			}
		}

		public void Attach(CanvasFeeder feeder)
		{
			lock (feedGate)
			{
				target = feeder;
				if (latestCode != null)
				{
					feeder.Push(latestCode, latestDone);
				}
			}
			attached.TrySetResult(feeder);
		}

		public async Task DrainAsync()
		{
			await (await attached.Task.WaitAsync(dropped).ConfigureAwait(continueOnCapturedContext: false)).DrainAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private sealed record GuidedTarget(long Id, PointD CenterPx, double RadiusPx, RectD? ControlFramePx, RectD DisplayBoundsPx, double Scale, string? Label, string SpokenInstruction, int Step, bool IsHover, long ArmedTicks, bool Typed, bool AnswersInText, string? FrontProcess)
	{
		public bool Contains(PointD point)
		{
			RectD? controlFramePx = ControlFramePx;
			if (controlFramePx.HasValue)
			{
				RectD valueOrDefault = controlFramePx.GetValueOrDefault();
				double num = 8.0 * Scale;
				if (point.X >= valueOrDefault.X - num && point.X <= valueOrDefault.Right + num && point.Y >= valueOrDefault.Y - num)
				{
					return point.Y <= valueOrDefault.Bottom + num;
				}
				return false;
			}
			return Distance(point, CenterPx) <= RadiusPx;
		}
	}

	private const string Area = "Companion";

	private const int MaximumToolRounds = 5;

	private static readonly TimeSpan ReleaseTail = TimeSpan.FromMilliseconds(150L, 0L);

	private const int MaximumPressAudioSeconds = 120;

	private readonly ClickyHttp http;

	private readonly SupabaseAuthService auth;

	private readonly BrainClient brainClient;

	private readonly BrainSession brain;

	private readonly SettingsStore settings;

	private readonly IMicrophoneCapture microphone;

	private readonly IAudioOutput audio;

	private readonly IScreenCaptureService screens;

	private readonly IScreenDrawingCanvas canvas;

	private readonly ICursorOverlay cursor;

	private readonly IHighlightOverlay? highlight;

	private readonly INotchPresenter notch;

	private readonly CompanionSessionOptions options;

	private readonly UiInvoker ui;

	private readonly RealtimeMouth mouth;

	private readonly PageReads? pageReads;

	private readonly object gate = new object();

	private VoiceState state;

	private string responseText = "";

	private long generation;

	private CancellationTokenSource? turnCancellation;

	private int activeTurnCount;

	private bool isListening;

	private bool pressWasRefused;

	private long pressStartedTicks;

	private GeminiTranscriber? ears;

	private byte[] pressAudio = Array.Empty<byte>();

	private int pressAudioLength;

	private int pressFrameCount;

	private int consecutiveDeadAirPresses;

	private CancellationTokenSource? liveTranscriptLoop;

	private GeminiTranscriber? warmEars;

	private long warmEarsExpiresTicks;

	private readonly Dictionary<int, double> lineAudioOffsets = new Dictionary<int, double>();

	private readonly Dictionary<int, double> lineGeneratedSeconds = new Dictionary<int, double>();

	private readonly SemaphoreSlim narrationSignal = new SemaphoreSlim(0, int.MaxValue);

	private long turnStartedTicks;

	private int? firstAudioMs;

	private string? waitStatusLabel;

	private volatile bool inFlightTurnIsOnboarding;

	private string currentTurnProgramSoFar = "";

	private static readonly TimeSpan HighlightTextBudget = TimeSpan.FromMilliseconds(900L, 0L);

	private volatile bool lastTurnWasTyped;

	private volatile bool lastTurnAnsweredInText;

	public static volatile bool SendsLibraryItems = true;

	private volatile bool captionsTextAnswer;

	private volatile bool typedAnswerCaptionRequested;

	private int audioPlaybackBeganCount;

	internal const string PlaceholderScreenshot = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAIAAgDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwAooooA/9k=";

	private HeldActions? actionsWaitingForTheDrawing;

	public const int LessonMinimumLineCount = 3;

	private LessonRecording? lessonRecording;

	private bool listensForLessonAudio;

	private bool hasReportedIrrecoverableMicrophone;

	private CancellationTokenSource? bluetoothHold;

	private readonly object prefetchGate = new object();

	private PrefetchedTurn? pendingPrefetchedTurn;

	internal const int WalkthroughMaximumSteps = 15;

	private const double GuidedTargetMaximumAgeSeconds = 600.0;

	private const double GuidedTargetMinimumRadiusPoints = 28.0;

	private const double GuidedTargetMaximumRadiusPoints = 180.0;

	private const double GuidedTargetSmallestModelRadius = 14.0;

	private const double ElementFrameHitSlackPoints = 8.0;

	private const double HoverRestSeconds = 2.0;

	private const int GuidedWatchTickMilliseconds = 120;

	private const int RingRevealDelayMilliseconds = 450;

	private static readonly TimeSpan PostClickCaptureFallback = TimeSpan.FromMilliseconds(900L, 0L);

	private const int AdvanceArmedMilliseconds = 120000;

	private const double StepScreenChangeTolerance = 0.03;

	private static readonly Regex StepInstructionPattern = new Regex("(^|[.!,;:]\\s*|\\b(now|then|next|and)\\s+)(drag|double-click|click|select|choose|scroll|move|extend|stretch|lengthen|resize|pull|slide|grab|press|hold|release|type|enter|pick|set|turn|tap|put|add|draw|open)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	private static readonly string OwnProcessName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "HeyClicky");

	private GuidedTarget? guidedTarget;

	private IDisposable? guidedPressWatch;

	private CancellationTokenSource? guidedWatchLoop;

	private long guidedTargetIds;

	private long advanceArmedUntilTicks;

	private TurnRequest? advanceTurn;

	private ScreenCaptureFrame? advanceBaseline;

	private static readonly TimeSpan PassiveExitGrace = TimeSpan.FromSeconds(1.4);

	private long guidedStepLoadingGeneration = -1L;

	public bool LastTurnWasTyped => lastTurnWasTyped;

	public VoiceState State
	{
		get
		{
			lock (gate)
			{
				return state;
			}
		}
	}

	private bool IsListeningNow
	{
		get
		{
			lock (gate)
			{
				return isListening;
			}
		}
	}

	public string ResponseText
	{
		get
		{
			lock (gate)
			{
				return responseText;
			}
		}
	}

	public RealtimeMouth Mouth => mouth;

	public IPageReader? PageReaderForTest => options.PageReader;

	private bool UsesOpenAIBrain
	{
		get
		{
			string text = settings.Read((AppSettings s) => s.OneBrainModel);
			if (text != null && text.Length > 0)
			{
				return text.StartsWith("gpt-", StringComparison.Ordinal);
			}
			return true;
		}
	}

	private bool DriveOfferedForRequest
	{
		get
		{
			IClickyDriveTurns? drive = options.Drive;
			if (drive != null && drive.OffersDrive)
			{
				return UsesOpenAIBrain;
			}
			return false;
		}
	}

	public int ActiveTurnCount => Volatile.Read(in activeTurnCount);

	public bool IsListening => IsListeningNow;

	public bool InFlightTurnIsOnboarding => Volatile.Read(in inFlightTurnIsOnboarding);

	public bool IsOnboardingAnswerInFlight
	{
		get
		{
			bool flag = InFlightTurnIsOnboarding;
			if (flag)
			{
				VoiceState voiceState = State;
				bool flag2 = (uint)(voiceState - 2) <= 1u;
				flag = flag2;
			}
			return flag;
		}
	}

	public string CurrentTurnProgramSoFar => Volatile.Read(in currentTurnProgramSoFar);

	public string? LastHighlightText { get; private set; }

	public int AudioPlaybackBeganCount
	{
		get
		{
			lock (gate)
			{
				return audioPlaybackBeganCount;
			}
		}
	}

	internal int HeldActionCountForTest
	{
		get
		{
			lock (gate)
			{
				return actionsWaitingForTheDrawing?.Calls.Count ?? 0;
			}
		}
	}

	public IReadOnlyList<string> CurrentTurnSpokenLines => SayLines.InOrder(CurrentTurnProgramSoFar);

	public ClickTargetRing? ArmedClickTarget
	{
		get
		{
			lock (gate)
			{
				GuidedTarget guidedTarget = this.guidedTarget;
				return ((object)guidedTarget != null) ? new ClickTargetRing(guidedTarget.CenterPx, guidedTarget.RadiusPx, guidedTarget.ControlFramePx, guidedTarget.Label) : null;
			}
		}
	}

	public event EventHandler? StateChanged;

	public event EventHandler<string>? UserTranscriptUpdated;

	public event EventHandler<string>? ResponseTextUpdated;

	public event EventHandler<CompanionError>? Failed;

	public event EventHandler<DrawingAction>? UnhandledDrawingAction;

	public event EventHandler<CompanionTurnMetrics>? TurnCompleted;

	public event EventHandler<CompanionTurnFinish>? TurnFinished;

	public event EventHandler<(int CharacterCount, bool Typed, string Trigger)>? MessageSent;

	public event Action<string, string?, bool>? TextDelivered;

	public event EventHandler? TextAnswerHandedToCaptions;

	public event EventHandler<DrawingRecord>? DrawingTold;

	public event EventHandler<ScreenCaptureFrame>? DrawingLeftUp;

	public event Action? OneBrainTurnEnded;

	public event EventHandler<(string Trigger, int? WalkthroughStep)>? TurnStarted;

	public async Task AskWithAppFactAsync(string fact, string trigger)
	{
		if (string.IsNullOrWhiteSpace(fact))
		{
			return;
		}
		InterruptForNewInput(clearDrawing: false);
		if (RefusedOverMessageCap(options.UsageContext()))
		{
			NotifyTerminating();
			SetState(VoiceState.Idle);
			return;
		}
		long turnGeneration;
		lock (gate)
		{
			turnGeneration = generation;
		}
		http.PrewarmWorker();
		long startedTicks = Environment.TickCount64;
		SetState(VoiceState.Processing);
		await RunTurnAsync(new TurnRequest(FrontApp: options.DesktopActions.FrontApplication(), Words: null, Fact: fact, Typed: false, AnswersInText: false, Capture: await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false), AudioSeconds: null, Trigger: trigger, StartedTicks: startedTicks, TranscriptMs: 0, WalkthroughStep: null), turnGeneration).ConfigureAwait(continueOnCapturedContext: false);
	}

	private void RaiseTextDelivered(string text, string? app, bool wasCopied)
	{
		try
		{
			this.TextDelivered?.Invoke(text, app, wasCopied);
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "TextDelivered handler threw: " + ex.Message);
		}
	}

	public CompanionSession(ClickyHttp http, SupabaseAuthService auth, BrainClient brainClient, BrainSession brain, SettingsStore settings, IMicrophoneCapture microphone, IAudioOutput audio, IScreenCaptureService screens, IScreenDrawingCanvas canvas, ICursorOverlay cursor, IHighlightOverlay? highlight, INotchPresenter notch, CompanionSessionOptions? options = null)
	{
		CompanionSession companionSession = this;
		this.http = http;
		this.auth = auth;
		this.brainClient = brainClient;
		this.brain = brain;
		this.settings = settings;
		this.microphone = microphone;
		this.audio = audio;
		this.screens = screens;
		this.canvas = canvas;
		this.cursor = cursor;
		this.highlight = highlight;
		this.notch = notch;
		this.options = options ?? new CompanionSessionOptions();
		ui = new UiInvoker(this.options.UiContext);
		IPageReader pageReader = this.options.PageReader;
		pageReads = ((pageReader != null) ? new PageReads(pageReader) : null);
		mouth = new RealtimeMouth(http, (bool force, CancellationToken ct) => auth.GetValidAccessTokenAsync(force, ct), audio, delegate
		{
			AppSettings current = settings.Current;
			return new MouthConfig(current.VoiceIdentifier, current.SpeechSpeed, companionSession.options.UsageContext(), current.VoiceLanguageCode);
		});
		(string Voice, double Speed, string? Language) mouthPick = settings.Read((AppSettings s) => (VoiceIdentifier: s.VoiceIdentifier, SpeechSpeed: s.SpeechSpeed, VoiceLanguageCode: s.VoiceLanguageCode));
		ClickyAnalytics.TrackVoiceSettings(mouthPick.Voice, mouthPick.Language, mouthPick.Speed);
		settings.Changed += delegate
		{
			(string, double, string) tuple = settings.Read((AppSettings s) => (VoiceIdentifier: s.VoiceIdentifier, SpeechSpeed: s.SpeechSpeed, VoiceLanguageCode: s.VoiceLanguageCode));
			(string, double, string) tuple2 = tuple;
			(string, double, string) tuple3 = mouthPick;
			if (!(tuple2.Item1 == tuple3.Item1) || tuple2.Item2 != tuple3.Item2 || !(tuple2.Item3 == tuple3.Item3))
			{
				(string, double, string) tuple4 = mouthPick;
				mouthPick = tuple;
				ClickyAnalytics.TrackVoiceSettings(tuple.Item1, tuple.Item3, tuple.Item2);
				bool num = tuple.Item1 != tuple4.Item1;
				bool flag = VoiceCatalog.IsGemini(tuple.Item1);
				if (!num && flag && tuple.Item2 != tuple4.Item2 && companionSession.mouth.IsGeminiSession)
				{
					companionSession.mouth.SetPlaybackRate(tuple.Item2);
				}
				bool flag2 = num || ((!flag) ? (tuple.Item2 != tuple4.Item2) : (tuple.Item3 != tuple4.Item3 || (tuple.Item2 != tuple4.Item2 && !companionSession.mouth.IsGeminiSession)));
				if (flag2 && auth.IsSignedIn)
				{
					companionSession.mouth.RefreshAfterConfigChangeAsync();
				}
			}
		};
		mouth.LineAudioQueued += OnLineAudioQueued;
		mouth.LineGenerated += OnLineGenerated;
		mouth.IsAtRest = () => companionSession.State == VoiceState.Idle && Volatile.Read(in companionSession.activeTurnCount) == 0 && !companionSession.IsListeningNow;
		mouth.KeepWarm = () => companionSession.options.KeepMouthWarm?.Invoke() ?? false;
		microphone.Frame += OnMicrophoneFrame;
		microphone.DeviceProvedDead += OnMicrophoneDeviceProvedDead;
		canvas.ActionRequested += OnCanvasActionRequested;
		canvas.UserActedOnScreen += OnCanvasUserActedOnScreen;
		auth.Changed += delegate
		{
			if (!auth.IsSignedIn)
			{
				companionSession.DiscardWarmEarsAsync();
			}
		};
	}

	public RealtimeMouth CreateSilentMouth()
	{
		return new RealtimeMouth(http, (bool force, CancellationToken ct) => auth.GetValidAccessTokenAsync(force, ct), new SilentAudioOutput(audio.SampleRate), delegate
		{
			AppSettings current = settings.Current;
			return new MouthConfig(current.VoiceIdentifier, current.SpeechSpeed, options.UsageContext(), current.VoiceLanguageCode);
		});
	}

	public void Prewarm()
	{
		http.PrewarmAll();
		if (auth.IsSignedIn)
		{
			mouth.EnsureOpenAsync();
		}
	}

	private BrainRequestFlags RequestFlags(bool offersDrive)
	{
		return new BrainRequestFlags(SendsLibraryItems, settings.Read((AppSettings s) => s.SaveDrawingsEffective), offersDrive)
		{
			FiguresFloat = settings.Read((AppSettings s) => s.StreamMode)
		};
	}

	public void StartFreshSession()
	{
		brain.StartFreshSession(auth.UserId);
		DropActionsWaitingForTheDrawing();
	}

	public Task BeginPushToTalkAsync()
	{
		options.Drive?.StopBecauseUserSpoke();
		InterruptForNewInput(clearDrawing: false);
		http.PrewarmWorker();
		http.PrewarmGemini();
		if (!auth.IsSignedIn)
		{
			RaiseFailed(new CompanionError("not_signed_in", "Sign in to talk to HeyClicky.", null));
			return Task.CompletedTask;
		}
		AppSettings current = settings.Current;
		string usageContext = options.UsageContext();
		if (RefusedOverMessageCap(usageContext))
		{
			lock (gate)
			{
				pressWasRefused = true;
			}
			NotifyTerminating();
			SetState(VoiceState.Idle);
			return Task.CompletedTask;
		}
		string text = EarsLanguage.Code();
		List<string> keyterms = NormalizedKeyterms(current.DictationCustomWords);
		GeminiTranscriber geminiTranscriber = TakeWarmEars(text, keyterms, usageContext);
		GeminiTranscriber geminiTranscriber2;
		if (geminiTranscriber != null && geminiTranscriber.SampleRate == microphone.SampleRate && geminiTranscriber.Adopt())
		{
			geminiTranscriber2 = geminiTranscriber;
			Log.Info("Companion", "ears: on the warm socket");
		}
		else
		{
			geminiTranscriber?.DisposeAsync().AsTask();
			geminiTranscriber2 = new GeminiTranscriber(http, (bool force, CancellationToken ct) => auth.GetValidAccessTokenAsync(force, ct), text, keyterms, usageContext, microphone.SampleRate);
			geminiTranscriber2.Start();
		}
		lock (gate)
		{
			ears = geminiTranscriber2;
			pressStartedTicks = Environment.TickCount64;
			pressAudioLength = 0;
			pressFrameCount = 0;
			pressWasRefused = false;
			isListening = true;
		}
		EndBluetoothHold(release: false);
		try
		{
			microphone.Start();
		}
		catch (Exception ex)
		{
			Log.Error("Companion", "microphone did not start", ex);
			ShowLiveEvent(CannotHearMicrophone(microphone.DeviceName));
			CancelListening();
			return Task.CompletedTask;
		}
		IHighlightOverlay highlightOverlay = highlight;
		if (highlightOverlay != null && highlightOverlay.IsEnabled && current.ScreenHighlightEnabled)
		{
			ui.Post(delegate
			{
				highlight.BeginWatch(current.CursorColorHex);
			});
		}
		pageReads?.BeginRead();
		SetState(VoiceState.Listening);
		StartLiveTranscriptLoop(geminiTranscriber2);
		WatchForEarsPaywall(geminiTranscriber2);
		Task.Run(delegate
		{
			brain.AdoptAccount(auth.UserId);
			brain.NoteKeyDown(UsesOpenAIBrain, CursorScreenCanvas(), RequestFlags(DriveOfferedForRequest));
			http.PrewarmOpenAI();
			mouth.EnsureOpenAsync();
		});
		return Task.CompletedTask;
	}

	private void WatchForEarsPaywall(GeminiTranscriber transcriber)
	{
		transcriber.Opened.ContinueWith(delegate
		{
			if (transcriber.PaywallReached)
			{
				lock (gate)
				{
					if (ears != transcriber)
					{
						return;
					}
					pressWasRefused = true;
				}
				CancelListening();
				NotifyTerminating();
				SetState(VoiceState.Idle);
				RaiseFailed(new CompanionError("paywall", "You've reached your message limit.", null));
			}
		}, TaskScheduler.Default);
	}

	public async Task EndPushToTalkAsync()
	{
		GeminiTranscriber transcriber;
		long heldMs;
		int num;
		long releaseGeneration;
		lock (gate)
		{
			if (pressWasRefused)
			{
				pressWasRefused = false;
				return;
			}
			if (!isListening || ears == null)
			{
				return;
			}
			transcriber = ears;
			heldMs = Environment.TickCount64 - pressStartedTicks;
			num = pressFrameCount;
			releaseGeneration = generation;
		}
		long releasedAtTicks = Environment.TickCount64;
		if (TimeSpan.FromMilliseconds(heldMs, 0L) < options.MinimumHold)
		{
			Log.Info("Companion", $"press of {heldMs} ms: too short, cancelled");
			CancelListening();
			ui.Post(delegate
			{
				highlight?.EndWatch();
			});
			SetState(VoiceState.Idle);
			return;
		}
		SetState(VoiceState.Processing);
		audio.PlayEarcon("textSend");
		BrainTurnTiming.NoteReleased();
		OneBrainStats.Shared.NoteKeyUp();
		(string? DisplayName, string? ProcessName) frontApp = options.DesktopActions.FrontApplication();
		double num2 = options.CaptureSettleSeconds?.Invoke() ?? 0.0;
		Task<TurnCapture> captureTask = (Task<TurnCapture>)((num2 > 0.0) ? ((Task)Task.Delay(TimeSpan.FromSeconds(num2)).ContinueWith((Task _) => CaptureForTurnAsync(endHighlight: true), TaskScheduler.Default).Unwrap()) : ((Task)CaptureForTurnAsync(endHighlight: true)));
		bool capturesFromBluetooth = microphone.IsBluetoothDevice;
		bool showedMicrophoneTrouble = false;
		if (heldMs > (capturesFromBluetooth ? 3000 : 1500) && num == 0)
		{
			showedMicrophoneTrouble = true;
			string deviceName = microphone.DeviceName;
			Log.Warn("Companion", $"{(double)heldMs / 1000.0:0.0} s hold, no audio from the mic ('{deviceName ?? "unknown"}')");
			if (Interlocked.Increment(ref consecutiveDeadAirPresses) >= 2)
			{
				MicrophoneProvedDead(deviceName ?? "Your microphone");
			}
			else
			{
				ShowLiveEvent(CannotHearMicrophone(deviceName));
			}
		}
		else if (num > 0)
		{
			consecutiveDeadAirPresses = 0;
			hasReportedIrrecoverableMicrophone = false;
		}
		await Task.Delay(ReleaseTail).ConfigureAwait(continueOnCapturedContext: false);
		lock (gate)
		{
			if (generation != releaseGeneration || ears != transcriber)
			{
				return;
			}
			isListening = false;
			ears = null;
		}
		try
		{
			if (capturesFromBluetooth)
			{
				microphone.StopKeepingDeviceOpen();
				HoldBluetoothMicrophoneOpenUntilTheReplyPlaysOut();
			}
			else
			{
				microphone.Stop();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "microphone stop: " + ex.Message);
		}
		liveTranscriptLoop?.Cancel();
		await Task.Delay(30).ConfigureAwait(continueOnCapturedContext: false);
		byte[] recorded;
		lock (gate)
		{
			recorded = pressAudio.AsSpan(0, pressAudioLength).ToArray();
			pressAudioLength = 0;
		}
		string words = (await transcriber.FinishAsync(null).ConfigureAwait(continueOnCapturedContext: false))?.Trim();
		if (string.IsNullOrEmpty(words) && !transcriber.DidFail)
		{
			words = transcriber.PartialTranscriptSoFar().Trim();
		}
		bool earsFailed = transcriber.DidFail && !transcriber.PaywallReached;
		if (earsFailed && recorded.Length >= microphone.SampleRate * 2 * 3 / 10)
		{
			string text = await TranscribeFallbackAsync(recorded).ConfigureAwait(continueOnCapturedContext: false);
			if (!string.IsNullOrEmpty(text))
			{
				words = text;
				Log.Info("Companion", "Gemini failed; the press was heard by the fallback");
			}
		}
		words = WithoutStrayScript(words);
		if (!string.IsNullOrEmpty(words))
		{
			EarsLanguage.RecordHeard(words);
		}
		double? audioSeconds = ((!string.IsNullOrEmpty(words) && transcriber.SentAudioSeconds > 0.0) ? new double?(transcriber.SentAudioSeconds) : ((double?)null));
		int transcriptMs = (int)(Environment.TickCount64 - releasedAtTicks);
		BrainTurnTiming.NoteTranscriptReady();
		transcriber.DisposeAsync().AsTask();
		if (generation != releaseGeneration)
		{
			Log.Info("Companion", "the user moved on before the transcript finished; this press's turn is dropped");
			return;
		}
		if (string.IsNullOrEmpty(words))
		{
			await captureTask.ConfigureAwait(continueOnCapturedContext: false);
			Log.Info("Companion", earsFailed ? "nothing heard (speech recognition failed)" : "nothing heard");
			ClickyAnalytics.TrackOneBrainNothingHeard((double)heldMs / 1000.0, earsFailed, showedMicrophoneTrouble);
			NotifyTerminating();
			SetState(VoiceState.Idle);
			if (earsFailed)
			{
				SetResponseText("I couldn't hear that, my speech connection dropped. Try again in a moment, or type it instead.");
				RaiseFailed(new CompanionError("ears_failed", "I couldn't hear that, my speech connection dropped. Try again in a moment, or type it instead.", null));
			}
			else if (heldMs >= 1000 && !showedMicrophoneTrouble && !transcriber.PaywallReached)
			{
				SetResponseText("I didn't catch that. Hold the keys while you talk, then let go.");
			}
			return;
		}
		Func<string, Task<bool>> takeSpokenWordsDirectly = options.TakeSpokenWordsDirectly;
		bool flag = takeSpokenWordsDirectly != null;
		if (flag)
		{
			flag = await TakesWordsDirectlyAsync(takeSpokenWordsDirectly, words).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (flag)
		{
			if (!earsFailed)
			{
				PrepareWarmEars();
			}
			await captureTask.ConfigureAwait(continueOnCapturedContext: false);
			pageReads?.Discard();
			Log.Info("Companion", "the words went straight to the agent open in the HeyClicky window");
			if (generation == releaseGeneration)
			{
				NotifyTerminating();
				SetState(VoiceState.Idle);
			}
		}
		else
		{
			this.UserTranscriptUpdated?.Invoke(this, words);
			if (!earsFailed)
			{
				PrepareWarmEars();
			}
			TurnCapture capture = await captureTask.ConfigureAwait(continueOnCapturedContext: false);
			if (generation == releaseGeneration)
			{
				options.ChatHistory?.Invoke()?.NoteUserSpokeWords(words);
			}
			await RunTurnAsync(new TurnRequest(words, null, Typed: false, AnswersInText: false, capture, frontApp, audioSeconds, "voice", releasedAtTicks, transcriptMs, null), releaseGeneration).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private string? WithoutStrayScript(string? words)
	{
		if (string.IsNullOrEmpty(words))
		{
			return words;
		}
		IReadOnlyList<IReadOnlyList<string>> readOnlyList = EarsLanguage.StoredHeardScripts();
		if (readOnlyList.Count < 10)
		{
			return words;
		}
		IReadOnlyList<string> systemLanguageTags = Array.Empty<string>();
		try
		{
			systemLanguageTags = options.SystemLanguageTags?.Invoke() ?? Array.Empty<string>();
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "system languages: " + ex.Message);
		}
		(string? VoiceLanguageCode, string DictationLanguageCode) tuple = settings.Read((AppSettings s) => (VoiceLanguageCode: s.VoiceLanguageCode, DictationLanguageCode: s.DictationLanguageCode));
		string item = tuple.VoiceLanguageCode;
		string item2 = tuple.DictationLanguageCode;
		IReadOnlySet<EarsLanguage.Script> acceptedScripts = EarsLanguage.AcceptedScripts(VoiceCatalog.PickedLanguages(item), item2, systemLanguageTags, readOnlyList);
		string text = EarsLanguage.GuardingNoise(words, acceptedScripts, null) ?? "";
		if (text == words)
		{
			return words;
		}
		Log.Info("Companion", "ears: dropped words in a script the user doesn't use (" + ((text.Length == 0) ? "the whole press" : "the edges") + ")");
		ClickyAnalytics.Capture("one_brain_stray_script_dropped", new JsonObject((JsonNodeOptions?)null)
		{
			["whole_press"] = text.Length == 0,
			["word_count"] = words.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length
		});
		return text;
	}

	private static async Task<bool> TakesWordsDirectlyAsync(Func<string, Task<bool>> takeDirectly, string words)
	{
		try
		{
			return await takeDirectly(words).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "passing the words straight on failed: " + ex.Message);
			return false;
		}
	}

	private async Task<TurnCapture> CaptureForTurnAsync(bool endHighlight)
	{
		long started = Environment.TickCount64;
		IReadOnlyList<HighlightStroke> strokes = Array.Empty<HighlightStroke>();
		if (endHighlight && highlight != null)
		{
			try
			{
				strokes = await ui.InvokeAsync(() => highlight.EndWatch()).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Warn("Companion", "highlight: " + ex.Message);
			}
		}
		Task<string?> highlightTextTask = (Task<string?>)((strokes.Count > 0) ? ((Task)HarvestHighlightTextAsync(strokes)) : ((Task)Task.FromResult<string>(null)));
		IReadOnlyList<ScreenCaptureFrame> frames;
		IReadOnlyList<DisplayInfo> displays;
		try
		{
			frames = await screens.CaptureAllAsync().ConfigureAwait(continueOnCapturedContext: false);
			displays = screens.GetDisplays();
		}
		catch (Exception ex2)
		{
			Log.Error("Companion", "screen capture failed", ex2);
			return new TurnCapture(Array.Empty<ScreenCaptureFrame>(), Array.Empty<DisplayInfo>(), null, (int)(Environment.TickCount64 - started));
		}
		string gesture = null;
		IReadOnlyList<ScreenCaptureFrame> framesWithoutInk = null;
		if (strokes.Count > 0)
		{
			gesture = HighlightCompositor.GestureSummary(strokes, displays);
			string color = settings.Read((AppSettings s) => s.CursorColorHex);
			List<ScreenCaptureFrame> painted = new List<ScreenCaptureFrame>(frames.Count);
			foreach (ScreenCaptureFrame item in frames)
			{
				List<ScreenCaptureFrame> list = painted;
				list.Add(await HighlightCompositor.CompositeAsync(item, strokes, color).ConfigureAwait(continueOnCapturedContext: false));
			}
			framesWithoutInk = frames;
			frames = painted;
		}
		string text = ((gesture != null) ? (await highlightTextTask.ConfigureAwait(continueOnCapturedContext: false)) : null);
		string text2 = text;
		if (gesture != null)
		{
			LastHighlightText = text2 ?? "";
		}
		return new TurnCapture(frames, displays, gesture, (int)(Environment.TickCount64 - started), text2)
		{
			FramesWithoutInk = framesWithoutInk
		};
	}

	private async Task<string?> HarvestHighlightTextAsync(IReadOnlyList<HighlightStroke> strokes)
	{
		Func<IReadOnlyList<HighlightStroke>, CancellationToken, Task<string>> highlightText = options.HighlightText;
		if (highlightText == null)
		{
			return null;
		}
		long started = Environment.TickCount64;
		using CancellationTokenSource budget = new CancellationTokenSource(HighlightTextBudget);
		try
		{
			string text = await highlightText(strokes, budget.Token).WaitAsync(HighlightTextBudget, budget.Token).ConfigureAwait(continueOnCapturedContext: false);
			Log.Info("Companion", $"highlight text: {((text == null) ? "nothing readable" : $"{text.Length} chars")} ({Environment.TickCount64 - started} ms)");
			return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
		}
		catch (Exception ex) when (((ex is OperationCanceledException || ex is TimeoutException) ? 1 : 0) != 0)
		{
			Log.Info("Companion", $"highlight text: no answer within {HighlightTextBudget.TotalMilliseconds:0} ms");
			return null;
		}
		catch (Exception ex2)
		{
			Log.Warn("Companion", "highlight text: " + ex2.Message);
			return null;
		}
	}

	private async Task<string?> TranscribeFallbackAsync(byte[] pcm16)
	{
		string text = await auth.GetValidAccessTokenAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (text == null)
		{
			return null;
		}
		byte[] wav = Pcm16.WavFromPcm16Mono(pcm16, microphone.SampleRate);
		string language = settings.Read((AppSettings s) => s.DictationLanguageCode);
		(int, JsonObject)? tuple = await brainClient.PostJsonAsync("/brain/ears/transcribe", delegate(Utf8JsonWriter writer)
		{
			writer.WriteBase64String("audio_wav_base64", wav);
			writer.WriteString("language", language);
		}, text, TimeSpan.FromSeconds(20L), options.UsageContext()).ConfigureAwait(continueOnCapturedContext: false);
		if (tuple.HasValue)
		{
			(int, JsonObject) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item1 == 402)
			{
				BillingPaywallDetector.HandleIfPaywall(402, JsonSerializer.SerializeToUtf8Bytes(valueOrDefault.Item2, JsonNodeContext.Default.JsonObject));
				return null;
			}
			string text2 = ((string?)valueOrDefault.Item2["text"])?.Trim();
			return (valueOrDefault.Item1 == 200 && !string.IsNullOrEmpty(text2)) ? text2 : null;
		}
		return null;
	}

	private void OnMicrophoneFrame(ReadOnlyMemory<byte> frame)
	{
		GeminiTranscriber geminiTranscriber;
		lock (gate)
		{
			if (!isListening)
			{
				return;
			}
			geminiTranscriber = ears;
			pressFrameCount++;
			int num = microphone.SampleRate * 2 * 120;
			if (pressAudioLength + frame.Length <= num)
			{
				if (pressAudioLength + frame.Length > pressAudio.Length)
				{
					byte[] array = ArrayPool<byte>.Shared.Rent(Math.Min(num, Math.Max(pressAudio.Length * 2, pressAudioLength + frame.Length + 65536)));
					pressAudio.AsSpan(0, pressAudioLength).CopyTo(array);
					if (pressAudio.Length != 0)
					{
						ArrayPool<byte>.Shared.Return(pressAudio);
					}
					pressAudio = array;
				}
				frame.Span.CopyTo(pressAudio.AsSpan(pressAudioLength));
				pressAudioLength += frame.Length;
			}
		}
		geminiTranscriber?.PushAudio(frame.Span);
	}

	private void StartLiveTranscriptLoop(GeminiTranscriber transcriber)
	{
		liveTranscriptLoop?.Cancel();
		CancellationTokenSource loop = new CancellationTokenSource();
		liveTranscriptLoop = loop;
		Task.Run(async delegate
		{
			string last = "";
			try
			{
				while (!loop.IsCancellationRequested)
				{
					await Task.Delay(150, loop.Token).ConfigureAwait(continueOnCapturedContext: false);
					string text = transcriber.PartialTranscriptSoFar().Trim();
					if (text.Length > 0 && text != last)
					{
						last = text;
						this.UserTranscriptUpdated?.Invoke(this, text);
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
		});
	}

	private void CancelListening()
	{
		GeminiTranscriber geminiTranscriber;
		lock (gate)
		{
			geminiTranscriber = ears;
			ears = null;
			isListening = false;
			pressAudioLength = 0;
		}
		liveTranscriptLoop?.Cancel();
		EndBluetoothHold(release: false);
		try
		{
			microphone.Stop();
		}
		catch
		{
		}
		geminiTranscriber?.DisposeAsync().AsTask();
	}

	private void PrepareWarmEars()
	{
		AppSettings current = settings.Current;
		GeminiTranscriber transcriber = new GeminiTranscriber(http, (bool force, CancellationToken ct) => auth.GetValidAccessTokenAsync(force, ct), EarsLanguage.Code(), NormalizedKeyterms(current.DictationCustomWords), options.UsageContext(), microphone.SampleRate);
		GeminiTranscriber geminiTranscriber;
		lock (gate)
		{
			geminiTranscriber = warmEars;
			warmEars = transcriber;
			warmEarsExpiresTicks = Environment.TickCount64 + 120000;
		}
		geminiTranscriber?.DisposeAsync().AsTask();
		transcriber.Start(asWarmSocket: true);
		Task.Delay(TimeSpan.FromSeconds(120L)).ContinueWith(delegate(Task _)
		{
			lock (gate)
			{
				if (warmEars != transcriber)
				{
					return;
				}
				warmEars = null;
			}
			_ = transcriber.DisposeAsync().AsTask();
		}, TaskScheduler.Default);
	}

	private GeminiTranscriber? TakeWarmEars(string language, IReadOnlyList<string> keyterms, string usageContext)
	{
		GeminiTranscriber geminiTranscriber;
		bool flag;
		lock (gate)
		{
			geminiTranscriber = warmEars;
			warmEars = null;
			if (geminiTranscriber == null)
			{
				return null;
			}
			flag = Environment.TickCount64 > warmEarsExpiresTicks;
		}
		if (flag || geminiTranscriber.LanguageCode != language || geminiTranscriber.UsageContextValue != usageContext || !geminiTranscriber.Keyterms.SequenceEqual(keyterms))
		{
			geminiTranscriber.DisposeAsync().AsTask();
			return null;
		}
		return geminiTranscriber;
	}

	private async Task DiscardWarmEarsAsync()
	{
		GeminiTranscriber geminiTranscriber;
		lock (gate)
		{
			geminiTranscriber = warmEars;
			warmEars = null;
		}
		if (geminiTranscriber != null)
		{
			await geminiTranscriber.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	internal static List<string> NormalizedKeyterms(IEnumerable<string> words)
	{
		return (from w in words.SelectMany((string w) => w.Split(new char[2] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries))
			select w.Trim()).Where(delegate(string w)
		{
			int length = w.Length;
			return length > 0 && length <= 60;
		}).DistinctBy((string w) => w.ToLowerInvariant()).Take(50)
			.ToList();
	}

	public Task SubmitTextAsync(string text)
	{
		return SubmitTextAsync(text, (bool?)null);
	}

	public Task SubmitTextAsync(string text, bool? respondWithVoice)
	{
		return SubmitTextAsync(text, respondWithVoice, "text");
	}

	public Task SubmitTextAsync(string text, IReadOnlyList<string> attachedFilePaths)
	{
		return SubmitTextAsync(text, null, "text", attachedFilePaths);
	}

	private async Task SubmitTextAsync(string text, bool? respondWithVoice, string trigger, IReadOnlyList<string>? attachedFilePaths = null)
	{
		string words = text.Trim();
		IReadOnlyList<string> attachments = Attachments.NormalizedPaths(attachedFilePaths);
		if (words.Length == 0 && attachments.Count > 0)
		{
			words = "Please review and act on the attached files.";
		}
		if (words.Length == 0)
		{
			return;
		}
		if (attachments.Count > 0)
		{
			Log.Info("Companion", $"typed turn with {attachments.Count} attachment(s)");
		}
		typedAnswerCaptionRequested = false;
		InterruptForNewInput(clearDrawing: false);
		if (RefusedOverMessageCap(options.UsageContext()))
		{
			NotifyTerminating();
			SetState(VoiceState.Idle);
			return;
		}
		long turnGeneration;
		lock (gate)
		{
			turnGeneration = generation;
		}
		http.PrewarmWorker();
		bool answersInText = !(respondWithVoice ?? settings.Read((AppSettings s) => s.TypedTurnsRespondWithVoice));
		long startedTicks = Environment.TickCount64;
		if (answersInText)
		{
			ui.Post(delegate
			{
				notch.Present(new NotchPresentation.TextResponse(words, "", IsThinking: true));
			});
		}
		SetState(VoiceState.Processing);
		(string? DisplayName, string? ProcessName) frontApp = options.DesktopActions.FrontApplication();
		TurnCapture capture = await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false);
		await RunTurnAsync(new TurnRequest(words, null, Typed: true, answersInText, capture, frontApp, null, trigger, startedTicks, 0, null)
		{
			AttachedFilePaths = attachments
		}, turnGeneration).ConfigureAwait(continueOnCapturedContext: false);
	}

	public void BeginPageReadForTextInput(Func<bool> isStillOpen)
	{
		pageReads?.BeginReadForTextInput(isStillOpen);
	}

	public void TextInputClosed()
	{
		pageReads?.TextInputClosed();
	}

	public async Task AskFromDrawingAsync(string text, string trigger)
	{
		if (lastTurnWasTyped)
		{
			await SubmitTextAsync(text, trigger == "figure_part" || !lastTurnAnsweredInText, trigger).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		string words = text.Trim();
		if (words.Length != 0)
		{
			InterruptForNewInput(clearDrawing: false);
			long turnGeneration;
			lock (gate)
			{
				turnGeneration = generation;
			}
			http.PrewarmWorker();
			long startedTicks = Environment.TickCount64;
			SetState(VoiceState.Processing);
			await RunTurnAsync(new TurnRequest(FrontApp: options.DesktopActions.FrontApplication(), Words: words, Fact: null, Typed: false, AnswersInText: false, Capture: await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false), AudioSeconds: null, Trigger: trigger, StartedTicks: startedTicks, TranscriptMs: 0, WalkthroughStep: null), turnGeneration).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task SubmitFactAsync(string fact, bool pointsOnly = false)
	{
		if (!string.IsNullOrWhiteSpace(fact))
		{
			InterruptForNewInput(clearDrawing: false);
			long turnGeneration;
			lock (gate)
			{
				turnGeneration = generation;
			}
			http.PrewarmWorker();
			long startedTicks = Environment.TickCount64;
			SetState(VoiceState.Processing);
			await RunTurnAsync(new TurnRequest(FrontApp: options.DesktopActions.FrontApplication(), Words: null, Fact: fact, Typed: false, AnswersInText: false, Capture: await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false), AudioSeconds: null, Trigger: "app", StartedTicks: startedTicks, TranscriptMs: 0, WalkthroughStep: null)
			{
				PointsOnly = pointsOnly
			}, turnGeneration).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task<IReadOnlyList<string>?> WriteReportTurnAsync(string fact, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			string text = await auth.GetValidAccessTokenAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (text == null)
			{
				Log.Warn("Companion", "report turn not written: not signed in");
				return null;
			}
			brain.AdoptAccount(auth.UserId);
			brain.BeginTurnStartingNewSessionIfIdle();
			NarrationState narration = new NarrationState
			{
				WriteOnly = true
			};
			TurnRequest turnRequest = new TurnRequest(null, fact, Typed: false, AnswersInText: false, null, options.DesktopActions.FrontApplication(), null, "app", Environment.TickCount64, 0, null)
			{
				Screenless = true
			};
			ScreenCaptureFrame screenCaptureFrame = new ScreenCaptureFrame(screens.GetDisplayUnderCursor(), Array.Empty<byte>(), 0, 0, ContainsCursor: true, default(PointD));
			TurnCapture capture = new TurnCapture(new _003C_003Ez__ReadOnlySingleElementList<ScreenCaptureFrame>(screenCaptureFrame), Array.Empty<DisplayInfo>(), null, 0);
			await StreamBrainAsync(turnRequest, capture, screenCaptureFrame, new List<ScreenCaptureFrame>(), text, narration, null, () => !ct.IsCancellationRequested, new TurnTimer(turnRequest.StartedTicks), ct).ConfigureAwait(continueOnCapturedContext: false);
			lock (narration.Gate)
			{
				string failure = narration.Failure;
				if (failure != null)
				{
					Log.Warn("Companion", "report turn not written: " + failure);
					return null;
				}
				return (from l in SayLines.InOrder(narration.ProgramSoFar)
					select l.Trim() into l
					where l.Length > 0
					select l).ToList();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "report turn not written: " + ex.Message);
			return null;
		}
	}

	public async Task<string?> WriteSpokenProgramAsync(string fact, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			TurnCapture capture = await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false);
			string text = await auth.GetValidAccessTokenAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (capture.Frames.Count == 0 || text == null)
			{
				Log.Warn("Companion", "lines not written: no screenshot or not signed in");
				return null;
			}
			brain.AdoptAccount(auth.UserId);
			brain.BeginTurnStartingNewSessionIfIdle();
			ScreenCaptureFrame cursorFrame = capture.Frames.FirstOrDefault((ScreenCaptureFrame f) => f.ContainsCursor) ?? capture.Frames[0];
			List<ScreenCaptureFrame> otherFrames = capture.Frames.Where((ScreenCaptureFrame f) => f != cursorFrame).ToList();
			NarrationState narration = new NarrationState
			{
				WriteOnly = true
			};
			TurnRequest turnRequest = new TurnRequest(null, fact, Typed: false, AnswersInText: false, capture, options.DesktopActions.FrontApplication(), null, "app", Environment.TickCount64, 0, null);
			await StreamBrainAsync(turnRequest, capture, cursorFrame, otherFrames, text, narration, null, () => !ct.IsCancellationRequested, new TurnTimer(turnRequest.StartedTicks), ct).ConfigureAwait(continueOnCapturedContext: false);
			lock (narration.Gate)
			{
				string failure = narration.Failure;
				if (failure != null)
				{
					Log.Warn("Companion", "lines not written: " + failure);
					return null;
				}
				return narration.ProgramSoFar;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "lines not written: " + ex.Message);
			return null;
		}
	}

	public void Cancel()
	{
		InterruptForNewInput(clearDrawing: true);
		ui.Post(delegate
		{
			canvas.IsStreamingNarration = false;
		});
		CancelListening();
		ui.Post(delegate
		{
			highlight?.EndWatch();
		});
		NotifyTerminating();
		SetState(VoiceState.Idle);
	}

	private void InterruptForNewInput(bool clearDrawing)
	{
		CancellationTokenSource cancellationTokenSource;
		lock (gate)
		{
			generation++;
			cancellationTokenSource = turnCancellation;
			turnCancellation = null;
			lineAudioOffsets.Clear();
			lineGeneratedSeconds.Clear();
		}
		cancellationTokenSource?.Cancel();
		mouth.Interrupt();
		DisarmGuidedTarget(null);
		DisarmAdvance();
		if (clearDrawing)
		{
			brain.ForgetDrawingOnScreen();
			ui.Post(delegate
			{
				canvas.Clear();
			});
		}
	}

	private bool RefusedOverMessageCap(string usageContext)
	{
		if (usageContext == "companion")
		{
			return options.RefuseTurnOverMessageCap?.Invoke(usageContext) ?? false;
		}
		return false;
	}

	private async Task RunTurnAsync(TurnRequest request, long turnGeneration, PrefetchedTurn? prefetched = null)
	{
		string trigger = request.Trigger;
		bool flag = ((trigger == "clicked_target" || trigger == "did_step") ? true : false);
		if (flag && RefusedOverMessageCap(options.UsageContext()))
		{
			FinishTurnQuietly(Interlocked.Read(in generation) == turnGeneration);
			return;
		}
		long startedTicks = ((prefetched == null) ? request.StartedTicks : Environment.TickCount64);
		CancellationTokenSource cancellation;
		lock (gate)
		{
			if (generation != turnGeneration)
			{
				return;
			}
			cancellation = (turnCancellation = new CancellationTokenSource());
			turnStartedTicks = startedTicks;
			firstAudioMs = null;
			waitStatusLabel = null;
			lineAudioOffsets.Clear();
			lineGeneratedSeconds.Clear();
			lastTurnWasTyped = request.Typed;
			lastTurnAnsweredInText = request.AnswersInText;
		}
		Interlocked.Increment(ref activeTurnCount);
		if (prefetched == null)
		{
			DropPrefetchedTurn("another one-brain turn started");
		}
		CancellationToken ct = cancellation.Token;
		if (request.WalkthroughStep.HasValue || request.Flow != null)
		{
			Log.Info("Companion", "walkthrough turn: trigger=" + request.Trigger + " step=" + (request.WalkthroughStep?.ToString() ?? "-"));
		}
		this.TurnStarted?.Invoke(this, (request.Trigger, request.WalkthroughStep));
		try
		{
			this.MessageSent?.Invoke(this, ((request.Words ?? request.Fact ?? "").Length, request.Typed, request.Trigger));
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "MessageSent handler threw: " + ex.Message);
		}
		TurnTimer metrics = new TurnTimer(startedTicks);
		long drawingStartedTicks = Environment.TickCount64;
		NarrationState toldNarration = null;
		bool continuedDrawing = false;
		bool turnThrew = false;
		int displayCount = 0;
		try
		{
			brain.AdoptAccount(auth.UserId);
			if (prefetched == null)
			{
				brain.BeginTurnStartingNewSessionIfIdle();
			}
			OneBrainStats.Shared.NoteTurnSent(request.Words != null && !request.Typed, brain.PreviousTurnAt);
			Task<bool> mouthOpening = (request.AnswersInText ? Task.FromResult(result: false) : mouth.EnsureOpenAsync(ct));
			long setupStarted = Environment.TickCount64;
			TurnCapture turnCapture = request.Capture;
			if ((object)turnCapture == null)
			{
				turnCapture = await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false);
			}
			TurnCapture capture = turnCapture;
			long captureDone = Environment.TickCount64;
			string accessToken = await auth.GetValidAccessTokenAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			long tokenDone = Environment.TickCount64;
			if (capture.Frames.Count == 0 || accessToken == null)
			{
				Log.Warn("Companion", "no screenshot or not signed in; turn dropped");
				FinishTurnQuietly(StillCurrent());
				if (accessToken == null)
				{
					RaiseFailed(new CompanionError("not_signed_in", "Sign in to talk to HeyClicky.", null));
				}
				else
				{
					RaiseFailed(new CompanionError("no_screenshot", "I couldn't see your screen just now, so I couldn't answer. Try again in a moment.", null));
				}
				return;
			}
			ScreenCaptureFrame cursorFrame = capture.Frames.FirstOrDefault((ScreenCaptureFrame f) => f.ContainsCursor) ?? capture.Frames[0];
			List<ScreenCaptureFrame> otherFrames = capture.Frames.Where((ScreenCaptureFrame f) => f != cursorFrame).ToList();
			string colorHex = settings.Read((AppSettings s) => s.CursorColorHex);
			NarrationState narration = prefetched?.Narration ?? new NarrationState();
			bool hidesLoneSources = request.AnswersInText && (options.RendersTextInNotchChat?.Invoke() ?? false);
			(DrawingContinuation, string) tuple = ((!request.IsDrive) ? (await ui.InvokeAsync(delegate
			{
				canvas.IsStreamingNarration = true;
				canvas.HidesLoneSources = hidesLoneSources;
				if (prefetched == null && canvas.CanContinueDrawing(cursorFrame))
				{
					DrawingContinuation drawingContinuation = canvas.ContinueStream();
					if ((object)drawingContinuation != null)
					{
						return (Continued: drawingContinuation, State: canvas.DrawingStateForModel());
					}
				}
				if (canvas.IsShowing)
				{
					canvas.Clear(animated: false);
				}
				canvas.BeginProgram(cursorFrame, colorHex);
				return ((DrawingContinuation Continued, string State))(Continued: null, State: null);
			}).ConfigureAwait(continueOnCapturedContext: false)) : (null, null));
			(DrawingContinuation Continued, string State) drawingSetup = tuple;
			long tickCount = Environment.TickCount64;
			if (tickCount - startedTicks > 900)
			{
				Log.Info("Companion", $"turn setup slow: {tickCount - startedTicks} ms after key-up (before setup {setupStarted - startedTicks} ms, capture {captureDone - setupStarted} ms, token {tokenDone - captureDone} ms, canvas on the UI thread {tickCount - tokenDone} ms)");
			}
			ScreenCaptureFrame requestFrame = cursorFrame;
			ScreenCaptureFrame drawingFrame = cursorFrame;
			bool recordsLesson = !request.AnswersInText && !request.IsDrive && string.IsNullOrEmpty(request.Flow ?? options.TurnFlow?.Invoke());
			if ((object)drawingSetup.Continued == null && recordsLesson)
			{
				BeginLessonRecording(request.Words, capture.WithoutUserInk(cursorFrame), colorHex);
			}
			else
			{
				lock (gate)
				{
					lessonRecording = null;
				}
			}
			var (continuation, _) = drawingSetup;
			if ((object)continuation != null)
			{
				narration.FirstLine = continuation.FirstLine;
				narration.IsContinuationDecided = false;
				long swingDeadline = Environment.TickCount64 + 1150;
				while (true)
				{
					flag = Environment.TickCount64 < swingDeadline;
					if (flag)
					{
						flag = await ui.InvokeAsync(() => canvas.IsSwingingFiguresToFront).ConfigureAwait(continueOnCapturedContext: false);
					}
					if (!flag)
					{
						break;
					}
					await Task.Delay(40, ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				ScreenCaptureFrame screenCaptureFrame = await ui.InvokeAsync(() => canvas.CaptureDrawingForModel(cursorFrame)).ConfigureAwait(continueOnCapturedContext: false);
				if ((object)screenCaptureFrame != null)
				{
					requestFrame = screenCaptureFrame;
				}
				Log.Info("Companion", $"building on the drawing on screen: first new line {continuation.FirstLine}, {continuation.Marks} marks");
			}
			narration.StartOver = async delegate
			{
				Log.Info("Companion", "the model started over: clearing the drawing on screen");
				ui.Post(delegate
				{
					canvas.Clear();
				});
				await Task.Delay(450, ct).ConfigureAwait(continueOnCapturedContext: false);
				if (!StillCurrent())
				{
					throw new OperationCanceledException(ct);
				}
				await ui.InvokeAsync(delegate
				{
					canvas.BeginProgram(drawingFrame, colorHex);
					canvas.IsStreamingNarration = true;
				}).ConfigureAwait(continueOnCapturedContext: false);
				lock (narration.Gate)
				{
					narration.FirstLine = 0;
				}
			};
			List<ScreenCaptureFrame> displays = new List<ScreenCaptureFrame> { cursorFrame };
			displays.AddRange(otherFrames);
			if (!request.IsDrive && displays.Count > 1)
			{
				narration.DisplayCount = displays.Count;
				narration.IsDisplayDecided = false;
				bool buildsOnDrawing = (object)drawingSetup.Continued != null;
				narration.MoveToDisplay = async delegate(int index)
				{
					ScreenCaptureFrame chosen = displays[index];
					if (buildsOnDrawing)
					{
						Log.Info("Companion", $"the model drew on display {index + 1} of {displays.Count}: moving the drawing there");
						ui.Post(delegate
						{
							canvas.Clear();
						});
						await Task.Delay(450, ct).ConfigureAwait(continueOnCapturedContext: false);
						if (!StillCurrent())
						{
							throw new OperationCanceledException(ct);
						}
					}
					else
					{
						Log.Info("Companion", $"the program drew on display {index + 1} of {displays.Count}");
					}
					drawingFrame = chosen;
					await ui.InvokeAsync(delegate
					{
						canvas.BeginProgram(chosen, colorHex);
						canvas.IsStreamingNarration = true;
					}).ConfigureAwait(continueOnCapturedContext: false);
					lock (narration.Gate)
					{
						narration.FirstLine = 0;
						narration.DrewOnOtherDisplay = true;
					}
					narration.IsContinuationDecided = true;
					if (recordsLesson)
					{
						BeginLessonRecording(request.Words, capture.WithoutUserInk(chosen), colorHex);
					}
				};
			}
			if (!StillCurrent())
			{
				return;
			}
			if (!request.IsDrive)
			{
				toldNarration = narration;
			}
			continuedDrawing = (object)drawingSetup.Continued != null;
			displayCount = ((capture.Displays.Count > 0) ? capture.Displays.Count : capture.Frames.Count);
			Task narrationTask = NarrateAsync(narration, request, mouthOpening, StillCurrent, metrics, ct);
			if (prefetched == null)
			{
				await StreamBrainAsync(request, capture, requestFrame, otherFrames, accessToken, narration, drawingSetup.State, StillCurrent, metrics, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				await prefetched.PlayAsync(this, new CanvasFeeder(this, narration, StillCurrent), StillCurrent, ct).ConfigureAwait(continueOnCapturedContext: false);
				lock (narration.Gate)
				{
					TurnTimer turnTimer = metrics;
					if (turnTimer.Model == null)
					{
						turnTimer.Model = narration.Model;
					}
				}
			}
			await narrationTask.ConfigureAwait(continueOnCapturedContext: false);
			if (StillCurrent())
			{
				List<string> lines;
				string programSoFar;
				lock (narration.Gate)
				{
					lines = ((narration.FirstLine == 0 && narration.Failure == null) ? narration.Lines.ToList() : new List<string>());
					programSoFar = narration.ProgramSoFar;
				}
				FinishLessonRecording(lines, programSoFar);
			}
			if (StillCurrent() && !request.IsDrive)
			{
				ui.Post(delegate
				{
					canvas.Finish();
					canvas.IsStreamingNarration = false;
				});
			}
			await RunActionsWaitingForTheDrawingAsync(StillCurrent()).ConfigureAwait(continueOnCapturedContext: false);
			if (StillCurrent() && !request.IsDrive)
			{
				RaiseTurnFinished(request, narration);
			}
			if (StillCurrent() && !request.IsDrive)
			{
				NoteTurnEndedForWalkthrough(narration, request, drawingFrame);
			}
		}
		catch (OperationCanceledException) when (!StillCurrent())
		{
		}
		catch (Exception ex3)
		{
			turnThrew = true;
			Log.Error("Companion", "turn failed", ex3);
			if (StillCurrent())
			{
				RaiseFailed(new CompanionError("turn_failed", ex3.Message, null));
			}
		}
		finally
		{
			if (toldNarration != null)
			{
				TrackScreenDrawingCompleted(toldNarration, request, metrics, StillCurrent(), turnThrew, continuedDrawing, displayCount, drawingStartedTicks);
			}
			if (!request.Typed && !request.AnswersInText)
			{
				options.ChatHistory?.Invoke()?.NoteSpokenAnswerFinished();
			}
			Interlocked.Decrement(ref activeTurnCount);
			if (!StillCurrent())
			{
				DropActionsHeldBy(request);
			}
			prefetched?.Cancel();
			if (Interlocked.Read(in guidedStepLoadingGeneration) == turnGeneration)
			{
				NoteGuidedStepShowed();
			}
			if (StillCurrent())
			{
				lock (gate)
				{
					if (turnCancellation == cancellation)
					{
						turnCancellation = null;
					}
				}
				flag = Volatile.Read(in activeTurnCount) == 0;
				if (flag)
				{
					VoiceState voiceState = State;
					bool flag2 = (uint)(voiceState - 2) <= 1u;
					flag = flag2;
				}
				if (flag)
				{
					if (State == VoiceState.Processing)
					{
						NotifyTerminating();
					}
					SetState(VoiceState.Idle);
				}
				this.TurnCompleted?.Invoke(this, metrics.ToMetrics(request.Typed, request.TranscriptMs, request.Capture?.CaptureMs ?? 0, firstAudioMs));
				Log.Info("Companion", "turn timings: " + metrics.Describe(firstAudioMs));
			}
			cancellation.Dispose();
			if (prefetched == null)
			{
				RaiseOneBrainTurnEnded();
			}
		}
		bool StillCurrent()
		{
			if (!ct.IsCancellationRequested)
			{
				return Interlocked.Read(in generation) == turnGeneration;
			}
			return false;
		}
	}

	private void RaiseTurnFinished(TurnRequest request, NarrationState narration)
	{
		CompanionTurnFinish e;
		lock (narration.Gate)
		{
			e = new CompanionTurnFinish(request.Words != null, narration.HeardLines.ToList(), narration.Marks > 0, request.Words);
		}
		try
		{
			this.TurnFinished?.Invoke(this, e);
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "TurnFinished handler threw: " + ex.Message);
		}
	}

	private static void TrackScreenDrawingCompleted(NarrationState narration, TurnRequest request, TurnTimer metrics, bool stillCurrent, bool threw, bool continuedDrawing, int displayCount, long startedTicks)
	{
		int count;
		int marks;
		int textDeliveries;
		int count2;
		int firstLine;
		bool targetArmed;
		bool drewOnOtherDisplay;
		string failure;
		long? firstLineHeardTicks;
		lock (narration.Gate)
		{
			count = narration.HeardLines.Count;
			marks = narration.Marks;
			textDeliveries = narration.TextDeliveries;
			count2 = narration.Lines.Count;
			firstLine = narration.FirstLine;
			targetArmed = narration.TargetArmed;
			drewOnOtherDisplay = narration.DrewOnOtherDisplay;
			failure = narration.Failure;
			firstLineHeardTicks = narration.FirstLineHeardTicks;
		}
		string text = ((!stillCurrent) ? "superseded" : (threw ? "failed" : (targetArmed ? "target_armed" : ((count > 0) ? "completed" : ((failure == null && count2 <= firstLine) ? "completed" : "failed")))));
		string? model = metrics.Model ?? "unknown";
		bool targetArmed2 = text == "target_armed";
		int valueOrDefault = request.WalkthroughStep.GetValueOrDefault();
		bool typed = request.Typed;
		int? firstLineMilliseconds;
		if (firstLineHeardTicks.HasValue)
		{
			long valueOrDefault2 = firstLineHeardTicks.GetValueOrDefault();
			firstLineMilliseconds = (int)(valueOrDefault2 - startedTicks);
		}
		else
		{
			firstLineMilliseconds = null;
		}
		ClickyAnalytics.TrackScreenDrawingCompleted(model, text, count, marks, targetArmed2, valueOrDefault, displayCount, drewOnOtherDisplay, continuedDrawing, typed, textDeliveries, firstLineMilliseconds, (int)(Environment.TickCount64 - startedTicks));
	}

	private void FinishTurnQuietly(bool stillCurrent)
	{
		if (stillCurrent)
		{
			NotifyTerminating();
			SetState(VoiceState.Idle);
		}
	}

	private async Task StreamBrainAsync(TurnRequest request, TurnCapture capture, ScreenCaptureFrame cursorFrame, List<ScreenCaptureFrame> otherFrames, string accessToken, NarrationState narration, string? drawingOnScreen, Func<bool> stillCurrent, TurnTimer metrics, CancellationToken ct, PrefetchedTurn? prefetched = null)
	{
		string timeLabel = DateTime.Now.ToString("HH:mm, dddd MMM d, yyyy", CultureInfo.InvariantCulture);
		string frontApp = request.FrontApp.DisplayName;
		IReadOnlyList<byte[]> readOnlyList = ((request.AttachedFilePaths.Count <= 0) ? Array.Empty<byte[]>() : (await Attachments.AttachedImagesAsync(request.AttachedFilePaths).ConfigureAwait(continueOnCapturedContext: false)));
		IReadOnlyList<byte[]> attachedImages = readOnlyList;
		string words = request.Words;
		string words2 = ((words != null && request.AttachedFilePaths.Count > 0) ? Attachments.WordsWithSummary(words, request.AttachedFilePaths, attachedImages.Count > 0) : request.Words);
		PageContext pageContext;
		if (words2 != null && pageReads != null)
		{
			PageReads? obj = pageReads;
			CancellationToken ct2 = ct;
			pageContext = await obj.TakeForTurnAsync(null, ct2).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			pageContext = null;
		}
		PageContext page = pageContext;
		bool pageAccessNoted = true;
		if ((object)page != null && page.AccessDenied && !(page.Kind == "web_page"))
		{
			pageAccessNoted = options.DocumentAccessDenied?.Invoke(page.Reference) ?? true;
		}
		string turnNote = "[This turn] " + timeLabel + "." + ((frontApp == null) ? "" : ("\nFront app: " + frontApp + "."));
		if ((object)page != null)
		{
			turnNote = turnNote + "\n" + ((page.Kind == "web_page") ? "Page open: " : "Document open: ") + page.ThreadSafeReference;
		}
		string highlightGesture = (request.Screenless ? null : capture.HighlightGesture);
		if (highlightGesture != null)
		{
			string text = highlightGesture;
			turnNote = turnNote + "\nThey highlighted part of the screen while asking: " + text;
		}
		IClickyDriveTurns drive = options.Drive;
		bool isDrive = request.IsDrive;
		bool offersDrive = DriveOfferedForRequest;
		BrainRequestFlags requestFlags = RequestFlags(offersDrive);
		bool keepsScreens = settings.Read((AppSettings s) => s.OneBrainKeepsScreensInHistory) && !isDrive && !request.Screenless;
		string storedSaid = (keepsScreens ? BrainSession.StoredUserText(words2 ?? "", request.Fact ?? "") : (words2 ?? (request.Screenless ? ("(" + request.Fact + ")") : ("(" + request.Fact + " Here is the screen now.)"))));
		string text2 = (request.Screenless ? null : options.LiveStateNote?.Invoke());
		string text3 = ((words2 == null) ? null : options.EdgeDockFocusNote?.Invoke(request.Typed));
		string text4 = ((!(request.Trigger == "button")) ? null : options.TakePillPlanNote?.Invoke());
		if (!request.Screenless)
		{
			inFlightTurnIsOnboarding = options.IsOnboardingTurn?.Invoke() ?? false;
		}
		JsonObject flowBrief = (request.Screenless ? null : (request.FlowBrief ?? SafeTake(options.TakeFlowBrief)));
		bool flag = (string?)flowBrief?["kind"] == "tour_finale";
		string liveState = string.Join("\n", from note in (flag ? ((IEnumerable<string>)Array.Empty<string>()) : ((IEnumerable<string>)new string[8]
			{
				text4,
				options.RosterNote?.Invoke(),
				text3,
				options.PlanNote?.Invoke(),
				text2,
				options.BrowserStateNote?.Invoke(requestFlags.Drive),
				request.Screenless ? null : FullscreenGameNote.Text(),
				VoiceCatalog.SpokenLanguageNote(settings.Read((AppSettings s) => s.VoiceLanguageCode))
			})).Append<string>(Drawing3DAvailability.Note)
			where !string.IsNullOrWhiteSpace(note)
			select note);
		BrainUserContext userContext = SafeUserContext();
		string flow = options.TurnFlow?.Invoke();
		string usageContext = options.UsageContext();
		string text6;
		if (highlightGesture != null)
		{
			string text5 = highlightGesture;
			string highlightText = capture.HighlightText;
			if (highlightText != null)
			{
				text6 = text5 + " " + highlightText;
				goto IL_08f6;
			}
		}
		text6 = highlightGesture;
		goto IL_08f6;
		IL_08f6:
		string highlightGesture2 = text6;
		CompanionToolContext toolContext = new CompanionToolContext(request.Words, request.Typed, request.AttachedFilePaths, cursorFrame.Jpeg, highlightGesture2, ((object)page == null) ? null : new PageReference((page.Kind == "web_page") ? "web_page" : "file", page.Reference));
		lock (narration.Gate)
		{
			narration.ToolContext = toolContext;
		}
		if (!request.Screenless)
		{
			canvas.ForgetWhetherLatestAnswerPointsAtScreen();
		}
		BrainTurnContext context = brain.BeginRequest((Width: cursorFrame.PixelWidth, Height: cursorFrame.PixelHeight), drawingOnScreen != null, request.Screenless);
		string screenshotBase64 = (request.Screenless ? "" : Convert.ToBase64String(cursorFrame.Jpeg));
		string storedScreenshotDataUrl = (keepsScreens ? ("data:image/jpeg;base64," + screenshotBase64) : null);
		List<JsonNode> turnItems = new List<JsonNode>();
		List<string> searchRecords = new List<string>();
		string turnToken = null;
		string earlierCode = "";
		string streamedCode = "";
		bool keepsReasoning = false;
		int lastInputTokens = 0;
		string turnScreenshotFile = null;
		int audioPlaybackBeganBefore = AudioPlaybackBeganCount;
		lock (gate)
		{
			currentTurnProgramSoFar = "";
		}
		BrainToolRunner runner = null;
		IProgramFeeder programFeeder = prefetched?.Feeder;
		IProgramFeeder feeder = programFeeder ?? new CanvasFeeder(this, narration, stillCurrent);
		int maximumRounds = (isDrive ? 2147483646 : 5);
		try
		{
			for (int round = 0; round <= maximumRounds; round++)
			{
				ct.ThrowIfCancellationRequested();
				string codePrefix = earlierCode;
				BrainToolRunner roundRunner = new BrainToolRunner(async delegate(BrainToolCall call)
				{
					bool flag2 = BrainToolCallPolicy.RunsAfterTheUserMovedOn(call.Name);
					if (ct.IsCancellationRequested && !flag2)
					{
						return ToolOutput.Status("cancelled", "The user moved on before this ran, so it didn't run.");
					}
					if (request.Screenless && BrainToolCallPolicy.IsJobDispatch(call.Name))
					{
						return "{\"status\":\"refused\",\"message\":\"Not in a report turn: the user hasn't said anything yet. Nothing was sent. Tell them what came back and what's left, and ask whether to have it keep going; their answer sends it.\"}";
					}
					OnBrainStatus(narration, "tool:" + call.Name);
					string text7;
					try
					{
						text7 = await RunToolCallAsync(call, request, toolContext, flag2 ? CancellationToken.None : ct).ConfigureAwait(continueOnCapturedContext: false);
					}
					finally
					{
						OnBrainStatus(narration, "tool_done:" + call.Name);
					}
					Log.Info("Companion", "tool " + call.Name + " → " + ((text7.Length > 400) ? (text7.Substring(0, 400) + "…") : text7));
					return text7;
				});
				if (round == 0)
				{
					Log.Info("Companion", $"turn context: page={(((object)page == null) ? "none" : $"{page.Kind} {page.ThreadSafeReference} ({page.Text?.Length ?? 0} chars{(page.AccessDenied ? ", access denied" : "")})")}, attached images={attachedImages.Count}, files={request.AttachedFilePaths.Count}, live state={liveState.Length} chars, user context: apps=[{string.Join(",", userContext.ConnectedApps)}] reconnect=[{string.Join(",", userContext.NeedsReconnect)}] roster={userContext.RosterNote.Length} chars, front app id={AppTeachingIds.FrontAppIdentifier(request.FrontApp.ProcessName)}");
				}
				runner = roundRunner;
				string userLayer = brain.UserLayer;
				string userLayerSignature = brain.UserLayerSignature;
				bool sendEnvironment = context.SendEnvironment && round == 0;
				string roundTurnToken = turnToken;
				List<JsonNode> itemsSoFar = turnItems.ToList();
				bool isTurnsFirstRequest = roundTurnToken == null;
				JsonObject driveBilling = (isDrive ? drive.TakeDriveBillingRequestFields() : null);
				BrainReply reply;
				try
				{
					reply = await StreamOnce(brain.ThreadSnapshotForWorker()).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (ScreenshotFilesUnavailableException)
				{
					brain.TurnOffScreenshotFilesForSession();
					reply = await StreamOnce(brain.ThreadSnapshotForWorker()).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (brain.SessionId != context.SessionId)
				{
					throw new OperationCanceledException("a fresh session started during this turn");
				}
				if (turnScreenshotFile == null)
				{
					turnScreenshotFile = reply.ScreenshotFile;
				}
				if (round == 0)
				{
					int? firstTokenMilliseconds = reply.FirstTokenMilliseconds;
					if (firstTokenMilliseconds.HasValue)
					{
						int valueOrDefault = firstTokenMilliseconds.GetValueOrDefault();
						firstTokenMilliseconds = metrics.RequestSentMs;
						if (firstTokenMilliseconds.HasValue)
						{
							int valueOrDefault2 = firstTokenMilliseconds.GetValueOrDefault();
							TurnTimer turnTimer = metrics;
							turnTimer.FirstTextMs.GetValueOrDefault();
							if (!turnTimer.FirstTextMs.HasValue)
							{
								int value = valueOrDefault2 + valueOrDefault;
								turnTimer.FirstTextMs = value;
							}
						}
					}
				}
				string userLayer2 = reply.UserLayer;
				if (userLayer2 != null)
				{
					brain.AdoptUserLayer(userLayer2, reply.UserLayerSignature);
				}
				metrics.Model = reply.ModelIdentifier ?? metrics.Model;
				lock (narration.Gate)
				{
					narration.Model = metrics.Model;
				}
				OneBrainStats.Shared.NoteRound(round, reply.Usage, reply.CostUsd, ModelDisplayName(settings.Read((AppSettings s) => s.OneBrainModel)) ?? metrics.Model ?? "", reply.FirstTokenMilliseconds);
				if (context.IsSessionsFirstTurn && round == 0 && reply.LastInputTokens > 0)
				{
					string? text8 = settings.Read((AppSettings s) => s.OneBrainModel);
					if (text8 == null || !text8.StartsWith("claude-", StringComparison.Ordinal))
					{
						brain.NoteBaselineInputTokens(reply.LastInputTokens);
					}
				}
				keepsReasoning = reply.KeepsReasoning;
				if (reply.MessageRecorded)
				{
					options.MessageCounted?.Invoke(obj: true);
				}
				if (reply.MessageRefunded)
				{
					options.MessageCounted?.Invoke(obj: false);
				}
				for (int j = 0; j < reply.DriveMessagesRefunded; j++)
				{
					options.MessageCounted?.Invoke(obj: false);
				}
				if (reply.LastInputTokens > 0)
				{
					lastInputTokens = reply.LastInputTokens;
				}
				else if (reply.InputTokens > 0)
				{
					lastInputTokens = reply.InputTokens;
				}
				turnItems.AddRange(reply.Items);
				searchRecords.AddRange(reply.SearchRecords);
				if (reply.Code.Length > 0)
				{
					earlierCode = ((codePrefix.Length == 0) ? reply.Code : (codePrefix + "\n" + reply.Code));
				}
				string turnToken2 = reply.TurnToken;
				if (turnToken2 != null)
				{
					turnToken = turnToken2;
				}
				brain.LogRecord(new JsonObject((JsonNodeOptions?)null)
				{
					["kind"] = "round",
					["round"] = round,
					["words"] = words2 ?? "",
					["fact"] = request.Fact ?? "",
					["code"] = reply.Code,
					["tool_calls"] = new JsonArray(((IEnumerable<BrainToolCall>)reply.ToolCalls).Select((Func<BrainToolCall, JsonNode>)((BrainToolCall c) => JsonValue.Create(c.Name, (JsonNodeOptions?)null))).ToArray()),
					["async_calls"] = new JsonArray(reply.ToolCalls.Where((BrainToolCall c) => c.IsAsync).Select((Func<BrainToolCall, JsonNode>)((BrainToolCall c) => JsonValue.Create(c.Name, (JsonNodeOptions?)null))).ToArray()),
					["computer_actions"] = new JsonArray(reply.ComputerCalls.SelectMany((BrainComputerCall c) => c.Actions).Select((Func<JsonObject, JsonNode>)((JsonObject a) => JsonValue.Create(((string?)a["type"]) ?? "", (JsonNodeOptions?)null))).ToArray()),
					["usage"] = reply.Usage?.DeepClone()
				});
				if (request.IsDrive)
				{
					string spokenSentence = reply.SpokenSentence;
					if (spokenSentence != null)
					{
						drive?.NoteSpokenStep(spokenSentence);
						lock (narration.Gate)
						{
							NarrationState narrationState = narration;
							List<string> lines = narration.Lines;
							List<string> list = new List<string>(1 + lines.Count);
							list.AddRange(lines);
							list.Add(spokenSentence);
							narrationState.Lines = list;
							NarrationState narrationState2 = narration;
							narrationState2.FirstSayLineMs.GetValueOrDefault();
							if (!narrationState2.FirstSayLineMs.HasValue)
							{
								int value = (int)(Environment.TickCount64 - turnStartedTicks);
								narrationState2.FirstSayLineMs = value;
							}
						}
						SignalNarration();
					}
				}
				if (request.IsDrive && drive != null && reply.ComputerCalls.Count > 0)
				{
					foreach (JsonObject item in from i in reply.Items.OfType<JsonObject>()
						where (string?)i["type"] == "computer_call"
						select i)
					{
						Log.Info("Companion", $"drive round {round}: computer_call fields [{string.Join(",", item.Select<KeyValuePair<string, JsonNode>, string>((KeyValuePair<string, JsonNode> p) => p.Key + ((p.Value == null) ? "=null" : "")))}]");
					}
					runner = null;
					bool handedBack = false;
					foreach (BrainComputerCall call2 in reply.ComputerCalls)
					{
						if (call2.PendingSafetyChecks.Count > 0)
						{
							drive.StopForSafetyCheck(call2.PendingSafetyChecks);
							handedBack = true;
							break;
						}
						ScreenCaptureFrame screenCaptureFrame = await drive.PerformComputerActionsAsync(call2.Actions, audioPlaybackBeganBefore).ConfigureAwait(continueOnCapturedContext: false);
						if ((object)screenCaptureFrame == null)
						{
							handedBack = true;
							break;
						}
						ShrinkOlderScreenshots(turnItems);
						turnItems.Add(new JsonObject((JsonNodeOptions?)null)
						{
							["type"] = "computer_call_output",
							["call_id"] = call2.CallId,
							["output"] = new JsonObject((JsonNodeOptions?)null)
							{
								["type"] = "computer_screenshot",
								["image_url"] = "data:image/jpeg;base64," + Convert.ToBase64String(screenCaptureFrame.Jpeg),
								["detail"] = "original"
							}
						});
					}
					if (handedBack || !stillCurrent())
					{
						break;
					}
				}
				else
				{
					if (reply.ToolCalls.Count == 0)
					{
						break;
					}
					drive?.NoteTurnBeforeTools(SayLines.InOrder(earlierCode).Count > 0, turnToken);
					bool pointsAtScreen = canvas.LatestAnswerPointsAtScreen;
					List<BrainToolCall> held = reply.ToolCalls.Where((BrainToolCall c) => pointsAtScreen && BrainToolCallPolicy.BringsAnotherAppForward(c.Name, c.IsAsync) && !roundRunner.HasStarted(c.CallId)).ToList();
					List<BrainToolCall> runNow = reply.ToolCalls.Where((BrainToolCall c) => !held.Contains(c)).ToList();
					List<string> list2 = await roundRunner.OutputsAsync(runNow).ConfigureAwait(continueOnCapturedContext: false);
					for (int k = 0; k < runNow.Count; k++)
					{
						turnItems.Add(new JsonObject((JsonNodeOptions?)null)
						{
							["type"] = "function_call_output",
							["call_id"] = runNow[k].CallId,
							["output"] = list2[k]
						});
					}
					if (held.Count > 0)
					{
						HoldActionsForTheDrawing(context.SessionId, held, request, toolContext);
					}
					runner = null;
					if ((drive != null && reply.ToolCalls.Any((BrainToolCall c) => drive.IsDriveToolName(c.Name))) || (reply.Code.Length != 0 && !BrainToolCallPolicy.NeedsAnotherRound(runNow, list2)) || round >= maximumRounds || !stillCurrent())
					{
						break;
					}
				}
				async Task<BrainReply> StreamOnce(List<JsonNode> thread)
				{
					BrainReplyAccumulator accumulator = new BrainReplyAccumulator(delegate(string codeSoFar)
					{
						metrics.MarkFirstCode();
						streamedCode = ((codePrefix.Length == 0) ? codeSoFar : (codePrefix + "\n" + codeSoFar));
						Volatile.Write(ref currentTurnProgramSoFar, streamedCode);
						if (isDrive)
						{
							lock (narration.Gate)
							{
								narration.ProgramSoFar = streamedCode;
								return;
							}
						}
						feeder.Push(streamedCode, done: false);
					}, delegate(string status)
					{
						OnBrainStatus(narration, status);
					}, delegate(BrainToolCall call)
					{
						IClickyDriveTurns clickyDriveTurns = drive;
						if ((clickyDriveTurns == null || !clickyDriveTurns.IsDriveToolName(call.Name)) && (!BrainToolCallPolicy.BringsAnotherAppForward(call.Name, call.IsAsync) || !canvas.LatestAnswerPointsAtScreen) && stillCurrent())
						{
							roundRunner.Start(call);
						}
					})
					{
						SpeechOnly = isDrive,
						OnDriveLeaseId = ((isDrive && drive != null) ? new Action<string>(drive.NoteDriveLeaseId) : null),
						OnFirstToken = (isTurnsFirstRequest ? new Action(BrainTurnTiming.NoteFirstToken) : null)
					};
					if (isTurnsFirstRequest)
					{
						BrainTurnTiming.NoteRequestStarted(request.Typed);
					}
					metrics.MarkRequestSent();
					return await brainClient.StreamTurnAsync(delegate(Utf8JsonWriter writer)
					{
						WriteTurnBody(writer, new TurnBodyParts(thread, itemsSoFar, context.SessionId, roundTurnToken, userLayer, userLayerSignature, sendEnvironment, words2, request.Fact, timeLabel, frontApp, AppTeachingIds.FrontAppIdentifier(request.FrontApp.ProcessName), highlightGesture, request.Typed, liveState, screenshotBase64, cursorFrame, otherFrames, keepsScreens ? turnNote : null, (round == 0) ? request.AudioSeconds : ((double?)null), request.Trigger, context.DrawingCodeToResend, drawingOnScreen, request.WalkthroughStep, request.Flow ?? flow, settings.Read((AppSettings s) => s.OneBrainModel), settings.Read((AppSettings s) => s.OneBrainReadsApps))
						{
							Page = page,
							PageAccessNoted = pageAccessNoted,
							AttachedImages = attachedImages,
							UserContext = userContext,
							HighlightText = (request.Screenless ? null : capture.HighlightText),
							FlowBrief = flowBrief,
							Screenless = request.Screenless,
							Flags = requestFlags,
							OffersDrive = offersDrive,
							IsDrive = isDrive,
							DriveBillingFields = driveBilling
						});
					}, accessToken, usageContext, accumulator, ct, delegate
					{
						metrics.MarkBrainFirstByte();
						if (isTurnsFirstRequest)
						{
							BrainTurnTiming.NoteResponseHeaders();
						}
					}, isTurnsFirstRequest ? new Action<int, bool>(BrainTurnTiming.NoteRequestBody) : null).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			if (!isDrive)
			{
				feeder.Push(earlierCode, done: true);
				await feeder.DrainAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			lock (narration.Gate)
			{
				narration.Finished = true;
			}
			SignalNarration();
			bool flag3 = prefetched != null;
			if (flag3)
			{
				flag3 = !(await prefetched.CommitGate.ConfigureAwait(continueOnCapturedContext: false));
			}
			if (flag3)
			{
				brain.LogRecord(new JsonObject((JsonNodeOptions?)null)
				{
					["kind"] = "dropped_before_use",
					["fact"] = request.Fact ?? ""
				});
				throw new OperationCanceledException("the prefetched turn was dropped before use");
			}
			if (!brain.CommitTurn(context.SessionId, turnNote, storedSaid, storedScreenshotDataUrl, turnItems, searchRecords, wasInterrupted: false, keepsReasoning && !isDrive, (isDrive || request.Screenless) ? null : earlierCode, turnScreenshotFile))
			{
				return;
			}
			if (isDrive)
			{
				NoteDriveActions(context.SessionId);
			}
			if (drive != null && SayLines.InOrder(earlierCode).Count == 0)
			{
				JsonNode jsonNode = turnItems.LastOrDefault((JsonNode i) => (string?)i["type"] == "function_call" && drive.IsDriveToolName(((string?)i["name"]) ?? ""));
				if (jsonNode != null)
				{
					drive.VoiceSilentToolTurn(((string?)jsonNode["name"]) ?? "", ((string?)jsonNode["arguments"]) ?? "{}");
				}
			}
			brain.CompactThreadIfLarge(lastInputTokens);
			brain.SaveToDisk();
		}
		catch (Exception ex2)
		{
			List<JsonNode> list3 = turnItems.ToList();
			foreach (var (brainToolCall, text9) in runner?.FinishedCallsWithOutputs() ?? new List<(BrainToolCall, string)>())
			{
				list3.Add(new JsonObject((JsonNodeOptions?)null)
				{
					["type"] = "function_call",
					["call_id"] = brainToolCall.CallId,
					["name"] = brainToolCall.Name,
					["arguments"] = brainToolCall.Arguments
				});
				list3.Add(new JsonObject((JsonNodeOptions?)null)
				{
					["type"] = "function_call_output",
					["call_id"] = brainToolCall.CallId,
					["output"] = text9
				});
			}
			string text10 = ((streamedCode.Length > earlierCode.Length) ? streamedCode : earlierCode);
			if (text10.Length > 0)
			{
				JsonObject obj2 = new JsonObject((JsonNodeOptions?)null)
				{
					["type"] = "message",
					["role"] = "assistant"
				};
				JsonNode reference = new JsonObject((JsonNodeOptions?)null)
				{
					["type"] = "output_text",
					["text"] = "```js\n" + text10 + "\n```"
				};
				obj2["content"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
				list3.Add(obj2);
			}
			bool flag4 = ex2 is PaywallReachedException;
			if (prefetched != null)
			{
				brain.LogRecord(new JsonObject((JsonNodeOptions?)null)
				{
					["kind"] = "dropped_before_use",
					["fact"] = request.Fact ?? "",
					["error"] = ex2.Message
				});
			}
			else if (!flag4)
			{
				if (brain.CommitTurn(context.SessionId, turnNote, storedSaid, storedScreenshotDataUrl, list3, searchRecords, wasInterrupted: true, keepsReasoning && !isDrive, null, turnScreenshotFile, BrainSession.IsSafetyRefusal(ex2.Message)) && isDrive)
				{
					NoteDriveActions(context.SessionId);
				}
				brain.SaveToDisk();
			}
			if (request.IsDrive && flag4)
			{
				drive?.StopForOutOfAgentMessages();
			}
			else if (request.IsDrive && !(ex2 is OperationCanceledException))
			{
				drive?.NoteDriveRequestFailed();
				ClickyAnalytics.ReportInternalError("clicky_drive", "drive_failed", ex2);
			}
			lock (narration.Gate)
			{
				narration.Finished = true;
				narration.Failure = ((ex2 is OperationCanceledException) ? "cancelled" : ex2.Message);
			}
			SignalNarration();
			if (ex2 is PaywallReachedException ex3)
			{
				if (stillCurrent())
				{
					NotifyTerminating();
					SetState(VoiceState.Idle);
					RaiseFailed(new CompanionError("paywall", ex3.Payload.Title, ex3.Raw));
				}
				return;
			}
			if (ex2 is OperationCanceledException && !stillCurrent())
			{
				throw;
			}
			Log.Warn("Companion", "brain turn failed: " + ex2.Message);
			if (ex2 is WorkerHttpException { Status: 429 })
			{
				lock (narration.Gate)
				{
					narration.Failure = "HTTP 429";
					return;
				}
			}
		}
	}

	private static JsonObject? SafeTake(Func<JsonObject?>? take)
	{
		if (take == null)
		{
			return null;
		}
		try
		{
			return take();
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "flow brief: " + ex.Message);
			return null;
		}
	}

	private static string? ModelDisplayName(string? pick)
	{
		switch (pick)
		{
		case "":
		case null:
			return null;
		case "gpt-6.1-sol":
			return "GPT-6.1 Sol";
		case "gpt-6.1-sol-medium":
			return "GPT-6.1 Sol (medium)";
		case "gpt-6.1-sol-fast":
			return "GPT-6.1 Sol fast";
		case "gpt-6.1-sol-fast-medium":
			return "GPT-6.1 Sol fast (medium)";
		case "gpt-6.1-sol-ultrafast":
			return "GPT-6.1 Sol Ultrafast (low)";
		case "gpt-6.1-sol-ultrafast-medium":
			return "GPT-6.1 Sol Ultrafast (medium)";
		case "gpt-6.1-sol-ultrafast-high":
			return "GPT-6.1 Sol Ultrafast (high)";
		case "claude-haiku-5-5":
			return "Claude Haiku 5.5 (experimental)";
		case "claude-sonnet-5-5":
			return "Claude Sonnet 5.5 (experimental)";
		case "claude-opus-5-5":
			return "Claude Opus 5.5 (experimental)";
		case "deepseek-v4.1-flash-medium":
			return "DeepSeek V4.1 Flash 1017 (medium, experimental)";
		case "deepseek-v4.1-flash-high":
			return "DeepSeek V4.1 Flash 1017 (high, experimental)";
		default:
			return pick;
		}
	}

	private static void NoteFirstLineHeard(string? model)
	{
		OneBrainStats.Shared.NoteFirstLineHeard();
		BrainTurnTiming.NoteFirstLineHeard(model);
	}

	internal static void WriteTurnBody(Utf8JsonWriter writer, TurnBodyParts p)
	{
		writer.WriteStartObject();
		BrainClient.WriteThread(writer, "thread", p.Thread);
		writer.WriteStartObject("turn");
		writer.WriteString("words", p.Words ?? "");
		writer.WriteString("fact", p.Fact ?? "");
		writer.WriteString("time_label", p.TimeLabel);
		writer.WriteString("front_app", p.FrontApp ?? "");
		writer.WriteString("front_app_bundle_id", p.FrontAppProcess ?? "");
		writer.WriteString("drawing_on_screen", p.DrawingOnScreen ?? "");
		if (p.HighlightGesture == null)
		{
			writer.WriteNull("highlight");
		}
		else
		{
			writer.WriteStartObject("highlight");
			writer.WriteString("gesture", p.HighlightGesture);
			writer.WriteString("text", p.HighlightText ?? "");
			writer.WriteEndObject();
		}
		PageContext page = p.Page;
		if ((object)page != null)
		{
			page.Write(writer, p.PageAccessNoted);
		}
		else
		{
			writer.WriteNull("page");
		}
		writer.WriteString("input", p.Typed ? "text" : "voice");
		writer.WriteString("live_state", p.LiveState);
		if (!p.Screenless)
		{
			writer.WriteStartObject("screenshot");
			writer.WriteString("base64", p.ScreenshotBase64);
			writer.WriteString("mimeType", "image/jpeg");
			writer.WriteNumber("widthInPixels", p.Screenshot.PixelWidth);
			writer.WriteNumber("heightInPixels", p.Screenshot.PixelHeight);
			writer.WriteEndObject();
		}
		if (p.StableNote != null)
		{
			writer.WriteString("stable_note", p.StableNote);
		}
		if (p.FlowBrief != null)
		{
			writer.WritePropertyName("flow_brief");
			p.FlowBrief.WriteTo(writer);
		}
		if (p.DrawingCode != null)
		{
			writer.WriteString("drawing_code", p.DrawingCode);
		}
		double? audioSeconds = p.AudioSeconds;
		if (audioSeconds.HasValue)
		{
			double valueOrDefault = audioSeconds.GetValueOrDefault();
			writer.WriteNumber("audio_seconds", valueOrDefault);
			writer.WriteString("stt_provider", "gemini");
		}
		if (p.AttachedImages.Count > 0)
		{
			writer.WriteStartArray("attached_images");
			foreach (byte[] attachedImage in p.AttachedImages)
			{
				writer.WriteStartObject();
				writer.WriteBase64String("base64", attachedImage);
				writer.WriteString("mimeType", "image/jpeg");
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}
		if (p.OtherScreens.Count > 0)
		{
			writer.WriteStartArray("other_screens");
			foreach (ScreenCaptureFrame otherScreen in p.OtherScreens)
			{
				writer.WriteStartObject();
				writer.WriteBase64String("base64", otherScreen.Jpeg);
				writer.WriteString("mimeType", "image/jpeg");
				writer.WriteNumber("widthInPixels", otherScreen.PixelWidth);
				writer.WriteNumber("heightInPixels", otherScreen.PixelHeight);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}
		int? walkthroughStep = p.WalkthroughStep;
		if (walkthroughStep.HasValue)
		{
			int valueOrDefault2 = walkthroughStep.GetValueOrDefault();
			writer.WriteNumber("walkthrough_step", valueOrDefault2);
		}
		writer.WriteString("trigger", p.Trigger);
		string flow = p.Flow;
		if (flow != null && flow.Length > 0)
		{
			writer.WriteString("flow", flow);
		}
		writer.WriteEndObject();
		BrainClient.WriteThread(writer, "turn_items", p.TurnItems);
		writer.WriteString("session_id", p.SessionId);
		writer.WriteNumber("max_rounds", 5);
		writer.WriteBoolean("async_tools", value: true);
		if (p.TurnToken != null)
		{
			writer.WriteString("turn_token", p.TurnToken);
		}
		if (p.Screenless)
		{
			writer.WriteBoolean("screenless", value: true);
		}
		Drawing3DAvailability.WriteFlags(writer);
		BrainRequestFlags brainRequestFlags = p.Flags._003CClone_003E_0024();
		brainRequestFlags.Drive = (p.Flags.Drive || p.OffersDrive) && !p.IsDrive;
		brainRequestFlags.Write(writer);
		if (p.IsDrive)
		{
			writer.WriteBoolean("takeover", value: true);
		}
		if (p.IsDrive)
		{
			JsonObject driveBillingFields = p.DriveBillingFields;
			if (driveBillingFields != null)
			{
				foreach (var (propertyName, jsonNode2) in driveBillingFields)
				{
					writer.WritePropertyName(propertyName);
					if (jsonNode2 == null)
					{
						writer.WriteNullValue();
					}
					else
					{
						jsonNode2.WriteTo(writer);
					}
				}
			}
		}
		if (p.UserLayer != null && p.UserLayerSignature != null)
		{
			writer.WriteString("user_layer", p.UserLayer);
			writer.WriteString("user_layer_signature", p.UserLayerSignature);
		}
		else
		{
			p.UserContext.Write(writer, IanaTimeZone());
		}
		if (p.SendEnvironment)
		{
			ClickySystemEnvironment.Write(writer, IanaTimeZone());
		}
		if (!string.IsNullOrEmpty(p.BrainModel))
		{
			writer.WriteString("brain_model", p.BrainModel);
		}
		if (p.AppReads)
		{
			writer.WriteBoolean("app_reads", value: true);
		}
		writer.WriteEndObject();
	}

	private static string IanaTimeZone()
	{
		if (!TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, out string ianaId))
		{
			return TimeZoneInfo.Local.Id;
		}
		return ianaId;
	}

	private void OnBrainStatus(NarrationState narration, string status)
	{
		string label;
		bool heldForAdoption;
		lock (narration.Gate)
		{
			if (status.StartsWith("tool:", StringComparison.Ordinal))
			{
				string text = status;
				int length = "tool:".Length;
				narration.RunningToolName = text.Substring(length, text.Length - length);
			}
			else if (status.StartsWith("tool_done:", StringComparison.Ordinal))
			{
				string? runningToolName = narration.RunningToolName;
				string text = status;
				int length = "tool_done:".Length;
				if (runningToolName == text.Substring(length, text.Length - length))
				{
					narration.RunningToolName = null;
				}
			}
			else
			{
				narration.IsSearchingTheWeb = status == "searching";
				narration.IsReadingConnectedApps = status == "reading_apps";
			}
			if (status == "reading_apps" && !narration.ReadConnectedApps)
			{
				narration.ReadConnectedApps = true;
				Log.Info("Companion", "this turn reads the user's connected apps: the drawing's typeText / copyText / openPage / handOff won't run");
			}
			label = (narration.StatusLabel = WaitStatusLabel(narration.IsReadingConnectedApps, narration.IsSearchingTheWeb, narration.RunningToolName));
			heldForAdoption = narration.HeldForAdoption;
		}
		if (!heldForAdoption)
		{
			ShowWaitStatusLabel(label);
		}
	}

	private void ShowWaitStatusLabel(string? label)
	{
		lock (gate)
		{
			waitStatusLabel = label;
		}
		if (State == VoiceState.Processing && notch is NotchPresenter notchPresenter)
		{
			notchPresenter.SetCustomStatusLabel(label);
		}
		SignalNarration();
	}

	internal static string? WaitStatusLabel(bool isReadingConnectedApps, bool isSearchingTheWeb, string? runningToolName)
	{
		if (isReadingConnectedApps)
		{
			return StatusLabelForTool("read_connected_apps");
		}
		if (isSearchingTheWeb)
		{
			return StatusLabelForTool("web_search");
		}
		if (runningToolName != null)
		{
			return StatusLabelForTool(runningToolName);
		}
		return null;
	}

	internal static string StatusLabelForTool(string name, string? argumentsJson = null)
	{
		if (name == "sessions_spawn" && argumentsJson != null)
		{
			string text = new ToolArgs(argumentsJson).Str("task");
			if (text != null && SuggestionVoiceAction.Parse(text).Kind == SuggestionVoiceAction.Kind.Skip)
			{
				return "Next suggestion…";
			}
		}
		return name switch
		{
			"send_to_higher_model" => "Thinking deeper", 
			"tutorial_draw_on_screen" => "Drawing on your screen…", 
			"web_search" => "Searching the web…", 
			"read_connected_apps" => "Reading your apps…", 
			"save_memory" => "Saving to memory…", 
			"insert_text" => "Typing it in…", 
			"capture_screen" => "Looking at your screen…", 
			"play_on_spotify" => "Playing on Spotify…", 
			"open_url" => "Opening link…", 
			"copy_to_clipboard" => "Copying to clipboard…", 
			"open_agent_dashboard" => "Opening Home…", 
			"sessions_answer_request" => "Answering request…", 
			"find_jobs" => "Looking through earlier work…", 
			"routines" => "Updating your routines…", 
			"skip_onboarding_interview" => "Skipping the interview…", 
			"clicky_memory" => "Checking memory…", 
			"clicky_artifacts" => "Checking its files…", 
			"save_drawing" => "Saving the drawing…", 
			"runtime_status" => "Checking status…", 
			"sessions_spawn" => "Starting agent…", 
			"session_status" => "Checking agents…", 
			"sessions_send" => "Sending to agent…", 
			"sessions_interrupt" => "Stopping agent…", 
			"sessions_yield" => "Checking agent…", 
			"open_artifact" => "Opening artifact…", 
			"shell_exec" => "Running command…", 
			"file_read" => "Reading file…", 
			"file_write" => "Writing file…", 
			"file_list" => "Listing files…", 
			_ => "Working…", 
		};
	}

	private void SignalNarration()
	{
		if (narrationSignal.CurrentCount < 4)
		{
			narrationSignal.Release();
		}
	}

	private async Task WaitForChangeAsync(int milliseconds, CancellationToken ct)
	{
		try
		{
			await narrationSignal.WaitAsync(milliseconds, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
		}
	}

	private void OnLineAudioQueued(int lineIndex, double offsetSeconds)
	{
		lock (gate)
		{
			if (!lineAudioOffsets.ContainsKey(lineIndex))
			{
				audioPlaybackBeganCount++;
			}
			lineAudioOffsets[lineIndex] = offsetSeconds;
			int? num = firstAudioMs;
			if (!num.HasValue && lineIndex >= 0)
			{
				firstAudioMs = (int)(Environment.TickCount64 - turnStartedTicks);
			}
		}
		SignalNarration();
	}

	private void OnLineGenerated(int lineIndex, double seconds)
	{
		lock (gate)
		{
			lineGeneratedSeconds[lineIndex] = seconds;
		}
		if (lineIndex >= 0 && seconds > 0.0)
		{
			ui.Post(delegate
			{
				canvas.SetLineLength(lineIndex, seconds);
			});
		}
		if (lineIndex >= 0)
		{
			NoteLessonLineLength(lineIndex, seconds);
		}
		SignalNarration();
	}

	private async Task NarrateAsync(NarrationState narration, TurnRequest request, Task<bool> mouthOpening, Func<bool> stillCurrent, TurnTimer metrics, CancellationToken ct)
	{
		await Task.Yield();
		canvas.NarrationAudioEndsAt = null;
		HashSet<int> deliveredActions = new HashSet<int>();
		List<string> corrections = new List<string>();
		_ = request.Words;
		long firstLineDeadline = Environment.TickCount64 + 90000;
		while (stillCurrent())
		{
			lock (narration.Gate)
			{
				if (narration.Lines.Count > narration.FirstLine || narration.Finished || (!request.AnswersInText && narration.EarlyFirstLine != null))
				{
					break;
				}
				goto IL_015b;
			}
			IL_015b:
			if (Environment.TickCount64 > firstLineDeadline)
			{
				break;
			}
			await WaitForChangeAsync(250, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!stillCurrent())
		{
			return;
		}
		int num;
		string failure;
		lock (narration.Gate)
		{
			num = Math.Max(0, narration.Lines.Count - narration.FirstLine);
			if (num == 0 && !request.AnswersInText && narration.EarlyFirstLine != null)
			{
				num = 1;
			}
			failure = narration.Failure;
		}
		if (num == 0)
		{
			if (failure == null || failure == "cancelled")
			{
				await DeliverActionsAsync(narration, int.MaxValue, deliveredActions, corrections, request, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			string line = (failure.StartsWith("HTTP 429", StringComparison.Ordinal) ? "You're asking me a lot right now. Give me a minute, then try again." : (BrainSession.IsSafetyRefusal(failure) ? "My AI provider's safety filter blocked this because it looks like hands-on security work. I can still explain the concepts." : "Sorry, I couldn't get an answer just now. Try asking me again."));
			await TellAsync(line, request, mouthOpening, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
			RaiseFailed(new CompanionError("turn_failed", failure, null));
			return;
		}
		metrics.FirstSayLineMs = narration.FirstSayLineMs;
		if (!request.AnswersInText)
		{
			await NarrateAloudAsync(narration, request, mouthOpening, deliveredActions, corrections, stillCurrent, metrics, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await NarrateInTextAsync(narration, request, deliveredActions, corrections, stillCurrent, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		while (stillCurrent())
		{
			lock (narration.Gate)
			{
				if (narration.Finished)
				{
					break;
				}
			}
			await WaitForChangeAsync(250, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!stillCurrent())
		{
			return;
		}
		await DeliverActionsAsync(narration, int.MaxValue, deliveredActions, corrections, request, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
		string failure2;
		lock (narration.Gate)
		{
			failure2 = narration.Failure;
		}
		if (BrainSession.IsSafetyRefusal(failure2))
		{
			corrections.Add("My AI provider's safety filter blocked this because it looks like hands-on security work. I can still explain the concepts.");
		}
		foreach (string item in corrections)
		{
			await TellAsync(item, request, mouthOpening, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task NarrateAloudAsync(NarrationState narration, TurnRequest request, Task<bool> mouthOpening, HashSet<int> deliveredActions, List<string> corrections, Func<bool> stillCurrent, TurnTimer metrics, CancellationToken ct)
	{
		bool flag = await Task.WhenAny(mouthOpening, Task.Delay(8000, ct)).ConfigureAwait(continueOnCapturedContext: false) == mouthOpening && mouthOpening.Result;
		if (!flag)
		{
			flag = await mouth.EnsureOpenAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!stillCurrent())
		{
			return;
		}
		if (!flag)
		{
			Log.Warn("Companion", "the mouth could not open; answering in text");
			Task<RealtimeProviderUnavailableReason?> diagnosis = RealtimeProviderAvailabilityDiagnosis.DiagnoseAsync(mouth.LastOpenFailure);
			await NarrateInTextAsync(narration, request with
			{
				AnswersInText = true
			}, deliveredActions, corrections, stillCurrent, ct).ConfigureAwait(continueOnCapturedContext: false);
			await ExplainMouthOpenFailureAsync(diagnosis, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		int firstLine;
		lock (narration.Gate)
		{
			firstLine = narration.FirstLine;
		}
		int requested = firstLine;
		List<string> spokenLines = new List<string>();
		string silentSpeaker = (request.Typed ? null : OutputSilenceKindNow());
		string earlyFirstLineSpoken = null;
		SpeakNext();
		long handedOverTicks = Environment.TickCount64;
		while (stillCurrent() && Environment.TickCount64 - handedOverTicks < 6000 && mouth.LastServerMessageTicks <= handedOverTicks && !HasAudioOffset(firstLine))
		{
			await WaitForChangeAsync(50, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (stillCurrent() && mouth.LastServerMessageTicks <= handedOverTicks && !HasAudioOffset(firstLine))
		{
			Log.Warn("Companion", "the mouth never answered line 1: replacing its connection");
			if (await mouth.ReplaceAsync(ct).ConfigureAwait(continueOnCapturedContext: false) && stillCurrent())
			{
				requested = firstLine;
				SpeakNext();
			}
		}
		bool chatMirrorEnded = false;
		int heardSoFar = -1;
		long nextStepAsideCheck = 0L;
		int lineIndex = firstLine;
		while (stillCurrent())
		{
			long lineDeadline = Environment.TickCount64 + ((lineIndex == firstLine) ? 20000 : 30000);
			double? offset = null;
			while (stillCurrent() && Environment.TickCount64 < lineDeadline)
			{
				lock (gate)
				{
					double value2;
					if (lineAudioOffsets.TryGetValue(lineIndex, out var value))
					{
						offset = value;
					}
					else if (lineGeneratedSeconds.TryGetValue(lineIndex, out value2) && value2 <= 0.0)
					{
						offset = -1.0;
					}
				}
				if (offset.HasValue)
				{
					break;
				}
				StepAsideOnceDrawingStarts();
				await WaitForChangeAsync(20, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (!offset.HasValue || !stillCurrent())
			{
				break;
			}
			while (stillCurrent() && offset >= 0.0 && audio.PlayedSeconds + 0.02 < offset)
			{
				PumpNextLine(narration, lineIndex, ref requested, SpeakNext);
				StepAsideOnceDrawingStarts();
				await WaitForChangeAsync(10, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (!stillCurrent())
			{
				break;
			}
			int heard = lineIndex;
			string captionLine = (settings.Read((AppSettings s) => s.DrawingVoiceCaptions) ? narration.LineText(lineIndex) : null);
			double spokenSeconds = ((captionLine == null) ? 0.0 : DrawingCaptionTiming.SpokenSeconds(captionLine, settings.Read((AppSettings s) => s.SpeechSpeed)));
			ui.Post(delegate
			{
				canvas.ReleaseLine(heard);
				if (captionLine != null)
				{
					canvas.ShowLiveCaption(captionLine, spokenSeconds, spokenSeconds + 1.0);
				}
			});
			NoteLessonLineHeard(heard);
			if (lineIndex == firstLine)
			{
				metrics.MarkFirstSpoken();
				NoteGuidedStepShowed();
				SetState(VoiceState.Responding);
			}
			else if (State == VoiceState.Processing)
			{
				SetState(VoiceState.Responding);
			}
			string text = narration.LineText(lineIndex) ?? "";
			if (lineIndex == firstLine && earlyFirstLineSpoken != null && text != earlyFirstLineSpoken)
			{
				Log.Warn("Companion", "line 1 read early from the code differs from the engine's line");
			}
			spokenLines.Add(text);
			bool flag2;
			lock (narration.Gate)
			{
				narration.HeardLines.Add(text);
				long? firstLineHeardTicks = narration.FirstLineHeardTicks;
				flag2 = !firstLineHeardTicks.HasValue;
				NarrationState narrationState = narration;
				narrationState.FirstLineHeardTicks.GetValueOrDefault();
				if (!narrationState.FirstLineHeardTicks.HasValue)
				{
					long tickCount = Environment.TickCount64;
					narrationState.FirstLineHeardTicks = tickCount;
				}
			}
			if (flag2)
			{
				NoteFirstLineHeard(narration.Model);
			}
			SetResponseText(string.Join(" ", spokenLines));
			heardSoFar = lineIndex;
			if (!request.Typed)
			{
				options.ChatHistory?.Invoke()?.NoteSpokenLineHeard(text);
			}
			if (!chatMirrorEnded && request.Typed && (options.RendersTextInNotchChat?.Invoke() ?? false) && text.Trim().Length > 0)
			{
				string chatText = NotchChatText(spokenLines);
				string words = request.Words ?? "";
				ui.Post(delegate
				{
					notch.Present(new NotchPresentation.TextResponse(words, chatText, IsThinking: false));
				});
			}
			StepAsideOnceDrawingStarts();
			await DeliverActionsAsync(narration, lineIndex, deliveredActions, corrections, request, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
			lineIndex++;
			long waitStarted = Environment.TickCount64;
			long lastProgress = waitStarted;
			int programLength = narration.ProgramSoFar.Length;
			while (stillCurrent())
			{
				bool flag3;
				bool finished;
				bool flag4;
				lock (narration.Gate)
				{
					flag3 = narration.Lines.Count > lineIndex;
					finished = narration.Finished;
					flag4 = narration.ProgramSoFar.Length != programLength || narration.StatusLabel != null;
					programLength = narration.ProgramSoFar.Length;
				}
				if (flag3 || finished)
				{
					break;
				}
				if (flag4)
				{
					lastProgress = Environment.TickCount64;
				}
				if (audio.Buffered <= TimeSpan.Zero && State == VoiceState.Responding)
				{
					SetState(VoiceState.Processing);
				}
				if (Environment.TickCount64 - lastProgress > 60000 || Environment.TickCount64 - waitStarted > 300000)
				{
					break;
				}
				PumpNextLine(narration, lineIndex - 1, ref requested, SpeakNext);
				StepAsideOnceDrawingStarts();
				await WaitForChangeAsync(50, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			lock (narration.Gate)
			{
				if (narration.Lines.Count <= lineIndex)
				{
					break;
				}
			}
			if (requested <= lineIndex)
			{
				PumpNextLine(narration, lineIndex - 1, ref requested, SpeakNext, force: true);
			}
		}
		long drainDeadline = Environment.TickCount64 + 60000;
		while (stillCurrent() && audio.Buffered > TimeSpan.Zero && Environment.TickCount64 < drainDeadline)
		{
			if (!canvas.NarrationAudioEndsAt.HasValue && !Mouth.IsResponseInFlight)
			{
				canvas.NarrationAudioEndsAt = DateTime.UtcNow + audio.Buffered;
			}
			StepAsideOnceDrawingStarts();
			await WaitForChangeAsync(50, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (stillCurrent() && !canvas.NarrationAudioEndsAt.HasValue)
		{
			canvas.NarrationAudioEndsAt = DateTime.UtcNow;
		}
		if (!request.Typed && silentSpeaker != null && stillCurrent())
		{
			if (MutedSpeakerReplyExemptNow())
			{
				Log.Info("Companion", "the speaker was silent, but this was the New Clicky interview or its introduction: no clipboard card");
			}
			else
			{
				PresentMutedSpeakerReply(string.Join(" ", spokenLines));
			}
		}
		void SpeakNext()
		{
			string text2 = narration.LineText(requested) ?? "";
			if (requested == firstLine)
			{
				metrics.MarkFirstLineToMouth();
				lock (narration.Gate)
				{
					if (narration.Lines.Count <= firstLine)
					{
						earlyFirstLineSpoken = text2;
					}
				}
			}
			audio.PrepareForUpcomingAudio();
			mouth.SpeakLine(text2, requested);
			requested++;
		}
		void StepAsideOnceDrawingStarts()
		{
			if (request.Typed && !chatMirrorEnded && ConsumeTypedAnswerCaptionRequest())
			{
				chatMirrorEnded = true;
			}
			if (heardSoFar >= 0 && canvas.InkedMarkCount > 0 && Environment.TickCount64 >= nextStepAsideCheck)
			{
				Func<bool>? stepNotchChatAsideForDrawing = options.StepNotchChatAsideForDrawing;
				if (stepNotchChatAsideForDrawing != null && stepNotchChatAsideForDrawing())
				{
					nextStepAsideCheck = Environment.TickCount64 + 500;
					if (!chatMirrorEnded)
					{
						Log.Info("Companion", "the answer draws on the screen: the notch's chat steps aside");
					}
					chatMirrorEnded = true;
				}
			}
		}
	}

	private async Task ExplainMouthOpenFailureAsync(Task<RealtimeProviderUnavailableReason?> diagnosis, Func<bool> stillCurrent)
	{
		RealtimeProviderUnavailableReason reason = null;
		try
		{
			reason = await diagnosis.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "provider diagnosis: " + ex.Message);
		}
		if ((object)reason == null)
		{
			RaiseFailed(new CompanionError("voice_unavailable", "My voice connection didn't open, so I wrote the answer instead.", null));
			return;
		}
		Log.Warn("Companion", "the mouth can't open where they are: " + reason.AnalyticsKey);
		ClickyAnalytics.ReportInternalError("realtime", "provider_unavailable", null, reason.AnalyticsKey + " surface=push_to_talk", CrashReportLevel.Warning, reason.AnalyticsKey);
		RaiseFailed(new CompanionError("voice_provider_unavailable", reason.UserFacingMessage, null));
		if (stillCurrent())
		{
			string message = reason.UserFacingMessage;
			ui.Post(delegate
			{
				cursor.ShowMessageAsync(message, streamed: true, 8.0);
			});
		}
	}

	public async Task<bool> AnnounceAsync(string line, bool showAtCursor = true, CancellationToken ct = default(CancellationToken))
	{
		line = line.Trim();
		if (line.Length == 0 || !auth.IsSignedIn)
		{
			return false;
		}
		long waitDeadline = Environment.TickCount64 + 45000;
		while (State != 0 || Volatile.Read(in activeTurnCount) > 0)
		{
			if (Environment.TickCount64 > waitDeadline)
			{
				Log.Info("Companion", "announcement dropped: HeyClicky stayed busy");
				return false;
			}
			await Task.Delay(150, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		long announcementGeneration;
		lock (gate)
		{
			announcementGeneration = generation;
		}
		if (showAtCursor)
		{
			ui.Post(delegate
			{
				cursor.ShowMessage(line);
			});
		}
		if (!(await mouth.EnsureOpenAsync(ct).ConfigureAwait(continueOnCapturedContext: false)) || !StillCurrent())
		{
			Log.Warn("Companion", "announcement not spoken: the mouth didn't open");
			return false;
		}
		lock (gate)
		{
			lineAudioOffsets.Remove(-1);
		}
		audio.PrepareForUpcomingAudio();
		Log.Info("Companion", "announcing: " + line);
		mouth.SpeakLine(line, -1);
		long startDeadline = Environment.TickCount64 + 10000;
		while (StillCurrent() && !HasAudioOffset(-1) && Environment.TickCount64 < startDeadline)
		{
			await Task.Delay(30, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!StillCurrent() || !HasAudioOffset(-1))
		{
			return false;
		}
		SetState(VoiceState.Responding);
		SetResponseText(line);
		long playDeadline = Environment.TickCount64 + 60000;
		await Task.Delay(200, ct).ConfigureAwait(continueOnCapturedContext: false);
		while (StillCurrent() && audio.Buffered > TimeSpan.Zero && Environment.TickCount64 < playDeadline)
		{
			await Task.Delay(60, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (Interlocked.Read(in generation) == announcementGeneration && State == VoiceState.Responding)
		{
			SetState(VoiceState.Idle);
		}
		return true;
		bool StillCurrent()
		{
			bool flag = !ct.IsCancellationRequested && Interlocked.Read(in generation) == announcementGeneration;
			if (flag)
			{
				VoiceState voiceState = State;
				bool flag2 = ((voiceState == VoiceState.Idle || voiceState == VoiceState.Responding) ? true : false);
				flag = flag2;
			}
			return flag;
		}
	}

	private void PresentMutedSpeakerReply(string answer)
	{
		string text = answer.Trim();
		if (text.Length != 0 && options.DesktopActions.CopyToClipboard(text))
		{
			string text2 = OutputSilenceKindNow() ?? "sound_came_back";
			Log.Info("Companion", $"the speaker was silent ({text2}): the answer ({text.Length} chars) is on the clipboard");
			NotifyTerminating();
			ShowLiveEvent(new NotchLiveEvent.VoiceReplyMutedCopiedToClipboard(text2));
		}
	}

	private bool MutedSpeakerReplyExemptNow()
	{
		try
		{
			return options.MutedSpeakerReplyExempt?.Invoke() ?? false;
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "muted-speaker exemption: " + ex.Message);
			return false;
		}
	}

	private string? OutputSilenceKindNow()
	{
		try
		{
			return options.OutputSilenceKind?.Invoke();
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "output volume: " + ex.Message);
			return null;
		}
	}

	private void PumpNextLine(NarrationState narration, int currentLine, ref int requested, Action speakNext, bool force = false)
	{
		bool flag;
		lock (narration.Gate)
		{
			flag = narration.Lines.Count > requested;
		}
		if (flag && requested == currentLine + 1)
		{
			bool flag2;
			lock (gate)
			{
				flag2 = lineGeneratedSeconds.ContainsKey(currentLine);
			}
			if (flag2 || force)
			{
				speakNext();
			}
		}
	}

	private bool HasAudioOffset(int line)
	{
		lock (gate)
		{
			return lineAudioOffsets.ContainsKey(line);
		}
	}

	public void MoveTypedAnswerToCaptions()
	{
		Log.Info("Companion", "the chat was put away mid-answer: the rest of the typed answer shows as captions");
		typedAnswerCaptionRequested = true;
	}

	private bool ConsumeTypedAnswerCaptionRequest()
	{
		if (!typedAnswerCaptionRequested)
		{
			return false;
		}
		typedAnswerCaptionRequested = false;
		return true;
	}

	private void HandTextAnswerToDrawingCaption()
	{
		Log.Info("Companion", "the typed answer draws on the screen: its lines show as captions");
		notch.ReturnToIdle();
		this.TextAnswerHandedToCaptions?.Invoke(this, EventArgs.Empty);
	}

	internal async Task NarrateInTextAsync(NarrationState narration, TurnRequest request, HashSet<int> deliveredActions, List<string> corrections, Func<bool> stillCurrent, CancellationToken ct)
	{
		captionsTextAnswer = false;
		List<string> shown = new List<string>();
		string lastShownLine = null;
		string question = request.Words ?? "";
		int lineIndex;
		lock (narration.Gate)
		{
			lineIndex = narration.FirstLine;
		}
		int firstLine = lineIndex;
		while (stillCurrent())
		{
			string line = null;
			bool finished;
			lock (narration.Gate)
			{
				if (narration.Lines.Count > lineIndex)
				{
					line = narration.Lines[lineIndex];
				}
				finished = narration.Finished;
			}
			if (line == null)
			{
				if (!finished)
				{
					StepAsideOnceDrawingStarts();
					await WaitForChangeAsync(50, ct).ConfigureAwait(continueOnCapturedContext: false);
					continue;
				}
				break;
			}
			AdoptCaptionRequest();
			shown.Add(line);
			lock (narration.Gate)
			{
				narration.HeardLines.Add(line);
				long? firstLineHeardTicks = narration.FirstLineHeardTicks;
				if (!firstLineHeardTicks.HasValue)
				{
					NoteFirstLineHeard(narration.Model);
				}
				narration.FirstLineHeardTicks.GetValueOrDefault();
				if (!narration.FirstLineHeardTicks.HasValue)
				{
					long tickCount = Environment.TickCount64;
					narration.FirstLineHeardTicks = tickCount;
				}
			}
			string answer = ((options.RendersTextInNotchChat?.Invoke() ?? false) ? NotchChatText(shown) : string.Join(" ", shown));
			int index = lineIndex;
			double reading = ReadingSeconds(line);
			bool enteringCaptions = !captionsTextAnswer && canvas.InkedMarkCount > 0;
			if (enteringCaptions)
			{
				captionsTextAnswer = true;
			}
			bool captioned = captionsTextAnswer;
			ui.Post(delegate
			{
				canvas.SetLineLength(index, reading);
				canvas.ReleaseLine(index);
				if (enteringCaptions)
				{
					HandTextAnswerToDrawingCaption();
				}
				if (captioned)
				{
					canvas.ShowLiveCaption(line, DrawingCaptionTiming.RevealSeconds(line, reading), reading + 1.2);
				}
				else
				{
					notch.Present(new NotchPresentation.TextResponse(question, answer, IsThinking: false));
				}
			});
			if (lineIndex == firstLine)
			{
				NoteGuidedStepShowed();
				SetState(VoiceState.Responding);
			}
			SetResponseText(answer);
			lastShownLine = line;
			await DeliverActionsAsync(narration, lineIndex, deliveredActions, corrections, request, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
			lineIndex++;
			long readUntil = Environment.TickCount64 + (long)(reading * 1000.0);
			while (Environment.TickCount64 < readUntil)
			{
				StepAsideOnceDrawingStarts();
				try
				{
					await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(40L, Math.Max(1L, readUntil - Environment.TickCount64)), 0L), ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}
		}
		void AdoptCaptionRequest()
		{
			if (!captionsTextAnswer && ConsumeTypedAnswerCaptionRequest())
			{
				captionsTextAnswer = true;
				if (lastShownLine != null)
				{
					string current2 = lastShownLine;
					double currentReading2 = ReadingSeconds(current2);
					ui.Post(delegate
					{
						canvas.ShowLiveCaption(current2, DrawingCaptionTiming.RevealSeconds(current2, currentReading2), currentReading2 + 1.2);
					});
				}
			}
		}
		void StepAsideOnceDrawingStarts()
		{
			AdoptCaptionRequest();
			if (!captionsTextAnswer && lastShownLine != null)
			{
				string current = lastShownLine;
				if (canvas.InkedMarkCount > 0)
				{
					captionsTextAnswer = true;
					double currentReading = ReadingSeconds(current);
					ui.Post(delegate
					{
						HandTextAnswerToDrawingCaption();
						canvas.ShowLiveCaption(current, DrawingCaptionTiming.RevealSeconds(current, currentReading), currentReading + 1.2);
					});
				}
			}
		}
	}

	public static string NotchChatText(IEnumerable<string> lines)
	{
		return string.Concat(lines.Select((string l) => l.Trim() + "\n\n"));
	}

	public static double ReadingSeconds(string line)
	{
		int num = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
		return Math.Min(6.0, Math.Max(1.6, 0.8 + (double)num * 0.28));
	}

	private async Task TellAsync(string line, TurnRequest request, Task<bool> mouthOpening, Func<bool> stillCurrent)
	{
		if (!stillCurrent())
		{
			return;
		}
		SetResponseText(line);
		if (request.AnswersInText)
		{
			if (ConsumeTypedAnswerCaptionRequest())
			{
				captionsTextAnswer = true;
			}
			if (captionsTextAnswer)
			{
				double reading = ReadingSeconds(line);
				ui.Post(delegate
				{
					canvas.ShowLiveCaption(line, DrawingCaptionTiming.RevealSeconds(line, reading), reading + 1.2);
				});
			}
			else
			{
				ui.Post(delegate
				{
					notch.Present(new NotchPresentation.TextResponse(request.Words ?? "", line, IsThinking: false));
				});
			}
			return;
		}
		bool flag = mouthOpening.IsCompletedSuccessfully && mouthOpening.Result;
		if (!flag)
		{
			flag = await mouth.EnsureOpenAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		if (flag)
		{
			mouth.SpeakLine(line, -1);
			if (!request.Typed)
			{
				options.ChatHistory?.Invoke()?.NoteSpokenLineHeard(line);
			}
		}
		else
		{
			ui.Post(delegate
			{
				notch.Present(new NotchPresentation.TextResponse(request.Words ?? "", line, IsThinking: false));
			});
		}
	}

	private async Task DeliverActionsAsync(NarrationState narration, int throughLine, HashSet<int> delivered, List<string> corrections, TurnRequest request, Func<bool> stillCurrent)
	{
		List<DrawingAction> actions;
		lock (narration.Gate)
		{
			actions = narration.Actions;
		}
		for (int i = 0; i < actions.Count; i++)
		{
			DrawingAction action = actions[i];
			if (!delivered.Contains(i) && ActionLine(action) <= throughLine)
			{
				delivered.Add(i);
				await PerformDrawingActionAsync(action, corrections, request, narration, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private static int ActionLine(DrawingAction action)
	{
		if (action.Payload.ValueKind != JsonValueKind.Object || !action.Payload.TryGetProperty("line", out var value) || !value.TryGetInt32(out var value2))
		{
			return 0;
		}
		return value2;
	}

	private static string? PayloadString(JsonElement payload, params string[] names)
	{
		if (payload.ValueKind == JsonValueKind.String)
		{
			return payload.GetString();
		}
		if (payload.ValueKind != JsonValueKind.Object)
		{
			return null;
		}
		foreach (string propertyName in names)
		{
			if (payload.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
			{
				return value.GetString();
			}
		}
		return null;
	}

	private async Task PerformDrawingActionAsync(DrawingAction action, List<string> corrections, TurnRequest request, NarrationState narration, Func<bool> stillCurrent)
	{
		IDesktopActions desktopActions = options.DesktopActions;
		bool readConnectedApps;
		lock (narration.Gate)
		{
			readConnectedApps = narration.ReadConnectedApps;
		}
		bool flag = readConnectedApps;
		if (flag)
		{
			bool flag2;
			switch (action.Kind)
			{
			case "type":
			case "copy":
			case "open":
			case "handoff":
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
			Log.Info("Companion", $"{action.Kind} in beat {ActionLine(action) + 1} not run: this turn read their connected apps");
			if (!corrections.Contains("I didn't do that part, since I'd just read your apps. Ask me again and I'll do it."))
			{
				corrections.Add("I didn't do that part, since I'd just read your apps. Ask me again and I'll do it.");
			}
			return;
		}
		switch (action.Kind)
		{
		case "copy":
		case "open":
		case "type":
		case "handoff":
		case "tourjob":
		case "tourasks":
		case "tourfade":
		case "tourtuck":
		case "button":
		case "tournotch":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			lock (narration.Gate)
			{
				narration.TextDeliveries++;
			}
		}
		switch (action.Kind)
		{
		case "handoff":
			await StartDrawingHandOffAsync(action, corrections, narration).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case "open":
		{
			string text = PayloadString(action.Payload, "text", "url");
			if (text == null || !desktopActions.OpenUrl(text))
			{
				corrections.Add("I couldn't open that page, so ask me again and I'll try another link.");
			}
			break;
		}
		case "copy":
		{
			string text2 = PayloadString(action.Payload, "text");
			bool flag3 = !string.IsNullOrEmpty(text2) && desktopActions.CopyToClipboard(text2);
			if (!string.IsNullOrEmpty(text2))
			{
				ClickyAnalytics.TrackClipboardCopyCompleted("screen_drawing", text2.Length, ClickyAnalytics.WordCount(text2), flag3);
			}
			if (!flag3)
			{
				corrections.Add("The clipboard copy didn't go through, so ask me again if you need that text.");
			}
			else
			{
				RaiseTextDelivered(text2, null, wasCopied: true);
			}
			break;
		}
		case "type":
		{
			string typeText = PayloadString(action.Payload, "text");
			if (!string.IsNullOrEmpty(typeText))
			{
				TextInsertOutcome textInsertOutcome = await desktopActions.InsertIntoFocusedFieldAsync(TextForInsertion(typeText)).ConfigureAwait(continueOnCapturedContext: false);
				Log.Info("Companion", "typeText: " + textInsertOutcome.Status + ((textInsertOutcome.Reason == null) ? "" : (" (" + textInsertOutcome.Reason + ")")));
				ClickyAnalytics.TrackTextInjectionCompleted(ClickyAnalytics.TextInjectionOutcomeName(textInsertOutcome.Status, textInsertOutcome.Reason), typeText.Length, ClickyAnalytics.WordCount(typeText), "screen_drawing", (textInsertOutcome.Status == "inserted") ? textInsertOutcome.App : null, (textInsertOutcome.Status == "failed") ? textInsertOutcome.Reason : null);
				if (textInsertOutcome.Status == "inserted")
				{
					RaiseTextDelivered(typeText, textInsertOutcome.App, wasCopied: false);
				}
				else if (textInsertOutcome.Status == "copied_to_clipboard")
				{
					RaiseTextDelivered(typeText, null, wasCopied: true);
				}
				if (textInsertOutcome.Status == "copied_to_clipboard")
				{
					corrections.Add("I couldn't drop that straight into the field, so it's on your clipboard. Press Control V to paste it.");
				}
				else if (textInsertOutcome.Status == "unconfirmed")
				{
					corrections.Add("I pasted it, but I couldn't see it show up in the field, so check it's there.");
				}
				else if (textInsertOutcome.Status == "refused" && textInsertOutcome.Reason == "secure_field")
				{
					corrections.Add("I didn't fill that in, it's a password field, so type that one yourself.");
				}
				else if (textInsertOutcome.Status != "inserted")
				{
					corrections.Add("I didn't fill that in, that field didn't look safe to type into.");
				}
			}
			break;
		}
		case "point":
		case "hover":
		case "target":
		{
			if (TryScreenPoint(action.Payload, request.Capture, out var point))
			{
				string label = PayloadString(action.Payload, "label");
				RectD? frame = null;
				bool armsClickTarget = action.Kind != "point" && !request.PointsOnly;
				if (armsClickTarget && TryScreenBox(action.Payload, request.Capture, out RectD box, out DisplayInfo display))
				{
					(PointD, RectD) tuple = await SnappedTargetAsync(box, display, label, point).ConfigureAwait(continueOnCapturedContext: false);
					RectD? rectD = tuple.Item2;
					point = tuple.Item1;
					frame = rectD;
				}
				ui.Post(delegate
				{
					cursor.PointAtAsync(point, label);
				});
				if (armsClickTarget)
				{
					await ArmGuidedTargetIfLastBeatAsync(narration, action, point, frame, request, stillCurrent).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			break;
		}
		default:
			this.UnhandledDrawingAction?.Invoke(this, action);
			break;
		case "link":
			break;
		case "photo":
			break;
		case "button":
			break;
		}
	}

	private async Task StartDrawingHandOffAsync(DrawingAction action, List<string> corrections, NarrationState narration)
	{
		CompanionToolContext toolContext;
		lock (narration.Gate)
		{
			toolContext = narration.ToolContext;
		}
		string text = PayloadString(action.Payload, "text");
		Func<BrainToolCall, CompanionToolContext, CancellationToken, Task<string>> toolCallHandler = options.ToolCallHandler;
		if (toolCallHandler != null && text != null)
		{
			string text2 = AgentTools.HandOffArguments(text);
			if (text2 != null)
			{
				ToolArgs toolArgs = new ToolArgs(text2);
				string task = toolArgs.Str("task") ?? "";
				Log.Info("Companion", $"handOff in beat {ActionLine(action) + 1}: {toolArgs.Str("label") ?? ""}");
				string text3;
				try
				{
					text3 = await toolCallHandler(new BrainToolCall("drawing-handoff-" + Guid.NewGuid().ToString("N").Substring(0, 12), "sessions_spawn", text2, IsAsync: true), toolContext ?? CompanionToolContext.None, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception ex)
				{
					text3 = ToolOutput.Error(ex.Message);
				}
				ToolArgs toolArgs2 = new ToolArgs(text3);
				string text4 = toolArgs2.Str("status");
				if ((text4 == "dispatched" || text4 == "founded") ? true : false)
				{
					string value = toolArgs2.Str("clicky_name") ?? "a Clicky";
					string text5 = toolArgs2.Str("clicky_slug");
					string value2 = ((text5 != null) ? (" (clicky_slug: " + text5 + ")") : "");
					brain.NoteForNextTurn($"[clicky drawing] The drawing handed this work to {value}{value2}, which started it: {((task.Length > 300) ? task.Substring(0, 300) : task)}. It is already running: never start it again; follow-ups about it go to that Clicky.");
				}
				else
				{
					Log.Info("Companion", "handOff not started: " + text3);
					corrections.Add("I couldn't start a job for that, so nothing was started yet. Ask me again in a moment.");
				}
				return;
			}
		}
		Log.Info("Companion", $"handOff in beat {ActionLine(action) + 1} not started: {((options.ToolCallHandler == null) ? "no dispatcher" : "unreadable payload")}");
		corrections.Add("I couldn't start a job for that, so nothing was started. Ask me again and I'll start one.");
	}

	private static bool TryScreenPoint(JsonElement payload, TurnCapture? capture, out PointD point)
	{
		point = default(PointD);
		if (payload.ValueKind != JsonValueKind.Object || (object)capture == null || capture.Frames.Count == 0)
		{
			return false;
		}
		if (!payload.TryGetProperty("x", out var value) || !payload.TryGetProperty("y", out var value2) || !value.TryGetDouble(out var value3) || !value2.TryGetDouble(out var value4))
		{
			return false;
		}
		ScreenCaptureFrame screenCaptureFrame = capture.Frames.FirstOrDefault((ScreenCaptureFrame f) => f.ContainsCursor) ?? capture.Frames[0];
		RectD boundsPx = screenCaptureFrame.Display.BoundsPx;
		point = new PointD(boundsPx.X + value3 * boundsPx.Width / (double)Math.Max(1, screenCaptureFrame.PixelWidth), boundsPx.Y + value4 * boundsPx.Height / (double)Math.Max(1, screenCaptureFrame.PixelHeight));
		return true;
	}

	private static bool TryScreenBox(JsonElement payload, TurnCapture? capture, out RectD box, out DisplayInfo display)
	{
		box = default(RectD);
		display = null;
		if (payload.ValueKind != JsonValueKind.Object || (object)capture == null || capture.Frames.Count == 0)
		{
			return false;
		}
		if (!payload.TryGetProperty("box", out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 4)
		{
			return false;
		}
		double[] array = new double[4];
		for (int i = 0; i < 4; i++)
		{
			if (!value[i].TryGetDouble(out array[i]))
			{
				return false;
			}
		}
		ScreenCaptureFrame screenCaptureFrame = capture.Frames.FirstOrDefault((ScreenCaptureFrame f) => f.ContainsCursor) ?? capture.Frames[0];
		RectD boundsPx = screenCaptureFrame.Display.BoundsPx;
		double num = boundsPx.Width / (double)Math.Max(1, screenCaptureFrame.PixelWidth);
		double num2 = boundsPx.Height / (double)Math.Max(1, screenCaptureFrame.PixelHeight);
		box = new RectD(boundsPx.X + array[0] * num, boundsPx.Y + array[1] * num2, array[2] * num, array[3] * num2);
		display = screenCaptureFrame.Display;
		if (box.Width > 0.0)
		{
			return box.Height > 0.0;
		}
		return false;
	}

	private async Task<(PointD Point, RectD Frame)> SnappedTargetAsync(RectD modelBox, DisplayInfo display, string? label, PointD fallback)
	{
		Func<RectD, DisplayInfo, CancellationToken, Task<RectD?>> snapTargetToControl = options.SnapTargetToControl;
		if (snapTargetToControl == null)
		{
			return (Point: fallback, Frame: modelBox);
		}
		long started = Environment.TickCount64;
		RectD? control = null;
		try
		{
			control = await snapTargetToControl(modelBox, display, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "target snap failed: " + ex.Message);
		}
		long value = Environment.TickCount64 - started;
		string value2 = $"{modelBox.Width:0}x{modelBox.Height:0}";
		if (control.HasValue)
		{
			RectD valueOrDefault = control.GetValueOrDefault();
			Log.Info("Companion", $"target \"{label}\": snapped to the control {valueOrDefault.Width:0}x{valueOrDefault.Height:0} from the model's {value2} ({value} ms)");
			return (Point: new PointD(valueOrDefault.X + valueOrDefault.Width / 2.0, valueOrDefault.Y + valueOrDefault.Height / 2.0), Frame: valueOrDefault);
		}
		Log.Info("Companion", $"target \"{label}\": kept the model's box {value2}, no control found ({value} ms)");
		return (Point: fallback, Frame: modelBox);
	}

	private void OnCanvasActionRequested(object? sender, DrawingAction action)
	{
		switch (action.Kind)
		{
		case "open":
		case "link":
		{
			string text = PayloadString(action.Payload, "url", "text");
			if (text != null && options.DesktopActions.OpenUrl(text))
			{
				brain.NoteForNextTurn("[clicky drawing] The user pressed the drawing's button, which opened " + text + " in their browser.");
			}
			break;
		}
		case "copy":
		{
			string text3 = PayloadString(action.Payload, "text");
			if (text3 != null)
			{
				bool succeeded = options.DesktopActions.CopyToClipboard(text3);
				ClickyAnalytics.TrackClipboardCopyCompleted("screen_drawing", text3.Length, ClickyAnalytics.WordCount(text3), succeeded);
			}
			break;
		}
		case "ask":
		{
			string text2 = PayloadString(action.Payload, "words", "text");
			if (text2 != null && text2.Length > 0)
			{
				audio.PlayEarcon("textSend");
				AskFromDrawingAsync(text2, "button");
			}
			break;
		}
		default:
			this.UnhandledDrawingAction?.Invoke(this, action);
			break;
		}
	}

	private async Task<string> RunToolCallAsync(BrainToolCall call, TurnRequest request, CompanionToolContext context, CancellationToken ct)
	{
		if (ct.IsCancellationRequested)
		{
			return ToolOutput.Status("cancelled", "The user moved on before this ran, so it didn't run.");
		}
		Log.Info("Companion", "tool " + call.Name);
		if (options.IsOnboardingTutorialRestrictingNormalUse() && BrainToolCallPolicy.ToolsHeldDuringTheTour.Contains(call.Name))
		{
			return ToolOutput.Status("not_during_the_tour", "The user is in HeyClicky's welcome tour. Nothing was started. Say in one short line that you'll do it right after the tour, and carry on with this step.");
		}
		IClickyDriveTurns drive = options.Drive;
		if (drive != null && drive.IsDriveToolName(call.Name))
		{
			return await drive.HandleToolAsync(call.Name, call.Arguments).ConfigureAwait(continueOnCapturedContext: false);
		}
		ToolArgs toolArgs = new ToolArgs(call.Arguments);
		switch (call.Name)
		{
		case "open_url":
		{
			string text = toolArgs.Str("url");
			if (text == null || !Uri.TryCreate(text, UriKind.Absolute, out Uri _))
			{
				return "{\"status\":\"error\",\"message\":\"missing or malformed url\"}";
			}
			return options.DesktopActions.OpenUrl(text) ? ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "opened",
				["url"] = text
			}) : ToolOutput.Error("That URL couldn't be opened.");
		}
		case "copy_to_clipboard":
		{
			string text2 = toolArgs.Exact("text");
			if (string.IsNullOrEmpty(text2))
			{
				return "{\"status\":\"error\",\"message\":\"missing or empty text\"}";
			}
			bool flag = options.DesktopActions.CopyToClipboard(text2);
			ClickyAnalytics.TrackClipboardCopyCompleted("copy_to_clipboard_tool", text2.Length, ClickyAnalytics.WordCount(text2), flag);
			if (!flag)
			{
				return "{\"status\":\"error\",\"message\":\"clipboard write failed\"}";
			}
			RaiseTextDelivered(text2, null, wasCopied: true);
			return ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "ok",
				["copied_to_clipboard"] = true,
				["chars"] = text2.Length
			});
		}
		case "insert_text":
			return await InsertTextToolAsync(toolArgs.Exact("text") ?? "").ConfigureAwait(continueOnCapturedContext: false);
		default:
		{
			Func<BrainToolCall, CompanionToolContext, CancellationToken, Task<string>> toolCallHandler = options.ToolCallHandler;
			if (toolCallHandler != null)
			{
				try
				{
					return await toolCallHandler(call, context, ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (OperationCanceledException) when (ct.IsCancellationRequested)
				{
					return ToolOutput.Status("cancelled", "The user moved on before this finished.");
				}
				catch (Exception ex2)
				{
					return ToolOutput.Error(ex2.Message);
				}
			}
			return ToolOutput.Error(call.Name + " isn't available in HeyClicky for Windows yet. Tell them in one short line.");
		}
		}
	}

	internal async Task<string> InsertTextToolAsync(string raw)
	{
		string text = TextForInsertion(raw);
		if (text.Length == 0)
		{
			return "{\"status\":\"error\",\"message\":\"missing text\"}";
		}
		TextInsertOutcome textInsertOutcome = await options.DesktopActions.InsertIntoFocusedFieldAsync(text).ConfigureAwait(continueOnCapturedContext: false);
		if (textInsertOutcome.Status == "inserted")
		{
			RaiseTextDelivered(text, textInsertOutcome.App, wasCopied: false);
		}
		else if (textInsertOutcome.Status == "copied_to_clipboard")
		{
			RaiseTextDelivered(text, null, wasCopied: true);
		}
		string result;
		switch (textInsertOutcome.Status)
		{
		case "inserted":
		{
			string app = textInsertOutcome.App;
			result = ToolResults.Json((app != null) ? new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "inserted",
				["characters"] = text.Length,
				["app"] = app
			} : new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "inserted",
				["characters"] = text.Length
			});
			break;
		}
		case "refused":
		{
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
			jsonObject["status"] = "refused";
			jsonObject["reason"] = textInsertOutcome.Reason ?? "unsafe_target";
			JsonObject jsonObject2 = jsonObject;
			jsonObject2["message"] = textInsertOutcome.Reason switch
			{
				"no_focused_text_field" => "No editable text field has focus right now. If the user means a visible field, type it into the field you see with typeText(x, y, text) in your drawing.", 
				"own_window" => "Focus is in HeyClicky's own UI, not an app text field. Insert it once the user is in their field.", 
				"secure_field" => "The focused field is a password/secure field — text is never inserted there. Tell the user to type it themselves.", 
				"address_bar" => "The focused field is the browser's address bar — not a valid insert target for composed text.", 
				_ => "That field didn't look safe to type into.", 
			};
			result = ToolResults.Json(jsonObject);
			break;
		}
		case "copied_to_clipboard":
			result = ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "copied_to_clipboard",
				["message"] = "It couldn't go into the field, so it's on their clipboard: tell them to press Control V."
			});
			break;
		case "unconfirmed":
			result = ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "unconfirmed",
				["message"] = "The paste was sent, but the text never showed up in the field (an app that reformats text can hide it, so it may still be there). Don't say it's in: tell the user to check, and if it's missing offer to copy it to their clipboard (copy_to_clipboard)."
			});
			break;
		default:
			result = ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "failed",
				["reason"] = textInsertOutcome.Reason ?? "paste_failed",
				["message"] = "Couldn't insert into the focused field. Type it into the field you see with typeText(x, y, text) in your drawing instead."
			});
			break;
		}
		return result;
	}

	internal static string TextForInsertion(string raw)
	{
		string text = raw.Replace("\r\n", "\n").Replace('\r', '\n');
		if (text.Contains('—'))
		{
			text = Regex.Replace(text, "[ \\t]*—[ \\t]*", ", ");
			text = Regex.Replace(text, "(?m)^, ", "");
		}
		return text.Trim();
	}

	private BrainUserContext SafeUserContext()
	{
		try
		{
			return options.UserContext?.Invoke() ?? BrainUserContext.Empty;
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "user context: " + ex.Message);
			return BrainUserContext.Empty;
		}
	}

	private void SetState(VoiceState next)
	{
		string customStatus;
		lock (gate)
		{
			if (state == next)
			{
				return;
			}
			state = next;
			customStatus = ((next == VoiceState.Processing && Volatile.Read(in activeTurnCount) > 0) ? waitStatusLabel : null);
		}
		if (notch is NotchPresenter notchPresenter)
		{
			notchPresenter.SetVoiceState(next, customStatus);
		}
		else
		{
			ui.Post(delegate
			{
				switch (next)
				{
				case VoiceState.Listening:
					notch.ShowLiveEvent(new NotchLiveEvent.ActivityListening(), null);
					break;
				case VoiceState.Processing:
					notch.ShowLiveEvent(new NotchLiveEvent.ActivityThinking(), null);
					break;
				case VoiceState.Responding:
					notch.ShowLiveEvent(new NotchLiveEvent.ActivitySpeaking(), null);
					break;
				default:
					notch.DismissLiveEvent(typeof(NotchLiveEvent.ActivitySpeaking));
					break;
				}
			});
		}
		ui.Post(delegate
		{
			cursor.SetVoiceState(next, 0f);
		});
		this.StateChanged?.Invoke(this, EventArgs.Empty);
	}

	private void NotifyTerminating()
	{
		if (notch is NotchPresenter notchPresenter)
		{
			notchPresenter.NotifyVoiceTurnTerminating();
		}
	}

	private void ShowLiveEvent(NotchLiveEvent liveEvent)
	{
		if (notch is NotchPresenter notchPresenter)
		{
			notchPresenter.ShowLiveEvent(liveEvent, null);
			return;
		}
		ui.Post(delegate
		{
			notch.ShowLiveEvent(liveEvent, null);
		});
	}

	private void SetResponseText(string text)
	{
		lock (gate)
		{
			responseText = text;
		}
		this.ResponseTextUpdated?.Invoke(this, text);
	}

	private void RaiseFailed(CompanionError error)
	{
		Log.Warn("Companion", "failed: " + error.Code + " " + error.Message);
		try
		{
			this.Failed?.Invoke(this, error);
		}
		catch (Exception ex)
		{
			Log.Error("Companion", "Failed handler threw", ex);
		}
	}

	private (int Width, int Height) CursorScreenCanvas()
	{
		try
		{
			DisplayInfo displayUnderCursor = screens.GetDisplayUnderCursor();
			double num = displayUnderCursor.BoundsPx.Width / Math.Max(1.0, displayUnderCursor.BoundsPx.Height);
			return (num >= 1.0) ? (Width: 1280, Height: (int)(1280.0 / num)) : (Width: (int)(1280.0 * num), Height: 1280);
		}
		catch
		{
			return (Width: 1280, Height: 800);
		}
	}

	public async ValueTask DisposeAsync()
	{
		microphone.Frame -= OnMicrophoneFrame;
		microphone.DeviceProvedDead -= OnMicrophoneDeviceProvedDead;
		EndBluetoothHold(release: true);
		canvas.ActionRequested -= OnCanvasActionRequested;
		canvas.UserActedOnScreen -= OnCanvasUserActedOnScreen;
		DisarmGuidedTarget(null);
		CancelListening();
		await DiscardWarmEarsAsync().ConfigureAwait(continueOnCapturedContext: false);
		await mouth.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (pressAudio.Length != 0)
		{
			ArrayPool<byte>.Shared.Return(pressAudio);
		}
	}

	public void AttachDrive(IClickyDriveTurns drive)
	{
		options.Drive = drive;
	}

	public async Task RunDriveTurnAsync(string fact, ScreenCaptureFrame frame, IReadOnlyList<DisplayInfo> displays)
	{
		InterruptForNewInput(clearDrawing: true);
		long turnGeneration;
		lock (gate)
		{
			turnGeneration = generation;
		}
		http.PrewarmWorker();
		SetState(VoiceState.Processing);
		TurnCapture capture = new TurnCapture(new _003C_003Ez__ReadOnlySingleElementList<ScreenCaptureFrame>(frame with
		{
			ContainsCursor = true
		}), displays, null, 0);
		TurnRequest request = new TurnRequest(null, fact, Typed: false, AnswersInText: false, capture, options.DesktopActions.FrontApplication(), null, "app", Environment.TickCount64, 0, null)
		{
			IsDrive = true
		};
		await RunTurnAsync(request, turnGeneration).ConfigureAwait(continueOnCapturedContext: false);
	}

	public void ClearDrawingForDrive()
	{
		brain.ForgetDrawingOnScreen();
		ui.Post(delegate
		{
			canvas.Clear();
		});
	}

	public void InterruptForDrive()
	{
		InterruptForNewInput(clearDrawing: false);
		if (State != VoiceState.Listening)
		{
			NotifyTerminating();
			SetState(VoiceState.Idle);
		}
	}

	public void SettleAfterDrive()
	{
		if (State == VoiceState.Processing)
		{
			NotifyTerminating();
			SetState(VoiceState.Idle);
		}
	}

	public async Task SpeakVerbatimLineAsync(string line, CancellationToken ct = default(CancellationToken))
	{
		line = line.Trim();
		if (line.Length == 0 || !auth.IsSignedIn)
		{
			return;
		}
		try
		{
			if (!(await mouth.EnsureOpenAsync(ct).ConfigureAwait(continueOnCapturedContext: false)))
			{
				Log.Warn("Companion", "drive line not spoken: the mouth didn't open");
				return;
			}
			lock (gate)
			{
				lineAudioOffsets.Remove(-1);
			}
			audio.PrepareForUpcomingAudio();
			Log.Info("Companion", "drive line: " + line);
			mouth.SpeakLine(line, -1);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			Log.Warn("Companion", "drive line not spoken: " + ex.Message);
		}
	}

	private void NoteDriveActions(string sessionId)
	{
		IReadOnlyList<string> readOnlyList = options.Drive?.ActionLog;
		if (readOnlyList != null && readOnlyList.Count > 0)
		{
			brain.AppendDeveloperNote(sessionId, ClickyDriveLines.ActionsNote(readOnlyList));
		}
	}

	internal static void ShrinkOlderScreenshots(List<JsonNode> items)
	{
		List<JsonNode> list = items.Where((JsonNode i) => (string?)i["type"] == "computer_call_output").ToList();
		foreach (JsonNode item in list.Take(Math.Max(0, list.Count - 1)))
		{
			if (item["output"] is JsonObject jsonObject && !((string?)jsonObject["image_url"] == "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAIAAgDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwAooooA/9k="))
			{
				jsonObject["image_url"] = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAIAAgDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwAooooA/9k=";
			}
		}
	}

	private void HoldActionsForTheDrawing(string sessionId, List<BrainToolCall> calls, TurnRequest request, CompanionToolContext context)
	{
		lock (gate)
		{
			HeldActions heldActions = actionsWaitingForTheDrawing;
			List<BrainToolCall> list = (((object)heldActions != null && heldActions.SessionId == sessionId) ? heldActions.Calls : new List<BrainToolCall>());
			int num = list.Count + calls.Count;
			List<BrainToolCall> list2 = new List<BrainToolCall>(num);
			CollectionsMarshal.SetCount(list2, num);
			Span<BrainToolCall> span = CollectionsMarshal.AsSpan(list2);
			int num2 = 0;
			Span<BrainToolCall> span2 = CollectionsMarshal.AsSpan(list);
			span2.CopyTo(span.Slice(num2, span2.Length));
			num2 += span2.Length;
			Span<BrainToolCall> span3 = CollectionsMarshal.AsSpan(calls);
			span3.CopyTo(span.Slice(num2, span3.Length));
			num2 += span3.Length;
			actionsWaitingForTheDrawing = new HeldActions(sessionId, list2, request, context);
		}
		Log.Info("Companion", $"{calls.Count} action(s) wait until the drawing is told ({string.Join(", ", calls.Select((BrainToolCall c) => c.Name))}): the screen stays as it is meanwhile");
	}

	private void DropActionsHeldBy(TurnRequest request)
	{
		HeldActions heldActions = null;
		lock (gate)
		{
			HeldActions heldActions2 = actionsWaitingForTheDrawing;
			if ((object)heldActions2 != null && (object)heldActions2.Request == request)
			{
				heldActions = heldActions2;
				actionsWaitingForTheDrawing = null;
			}
		}
		if ((object)heldActions != null)
		{
			Log.Info("Companion", "a new turn began before the drawing was told: " + string.Join(", ", heldActions.Calls.Select((BrainToolCall c) => c.Name)) + " dropped");
		}
	}

	private void DropActionsWaitingForTheDrawing()
	{
		lock (gate)
		{
			actionsWaitingForTheDrawing = null;
		}
	}

	internal async Task<List<string>> PerformDrawingActionsForTestAsync(IReadOnlyList<DrawingAction> actions, bool readConnectedApps)
	{
		NarrationState narration = new NarrationState
		{
			ReadConnectedApps = readConnectedApps
		};
		List<string> corrections = new List<string>();
		TurnRequest request = new TurnRequest("question", null, Typed: false, AnswersInText: false, null, (DisplayName: null, ProcessName: null), null, "voice", Environment.TickCount64, 0, null);
		foreach (DrawingAction action in actions)
		{
			await PerformDrawingActionAsync(action, corrections, request, narration, () => true).ConfigureAwait(continueOnCapturedContext: false);
		}
		return corrections;
	}

	private async Task RunActionsWaitingForTheDrawingAsync(bool stillCurrent)
	{
		HeldActions waiting;
		lock (gate)
		{
			waiting = actionsWaitingForTheDrawing;
			actionsWaitingForTheDrawing = null;
		}
		if ((object)waiting == null)
		{
			return;
		}
		if (!stillCurrent || waiting.SessionId != brain.SessionId)
		{
			Log.Info("Companion", "a new turn began before the drawing was told: " + string.Join(", ", waiting.Calls.Select((BrainToolCall c) => c.Name)) + " dropped");
			return;
		}
		List<string> failures = new List<string>();
		foreach (BrainToolCall call in waiting.Calls)
		{
			string text = await RunToolCallAsync(call, waiting.Request, waiting.Context, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			Log.Info("Companion", "after the drawing: " + call.Name + " → " + ((text.Length > 400) ? (text.Substring(0, 400) + "…") : text));
			brain.AppendToolResult(call, text);
			if (!BrainToolCallPolicy.IsCleanAsyncSuccess(call.Name, text))
			{
				failures.Add(call.Name + ": " + ((text.Length > 400) ? text.Substring(0, 400) : text));
			}
		}
		brain.SaveToDisk();
		brain.LogRecord(new JsonObject((JsonNodeOptions?)null)
		{
			["kind"] = "after_drawing",
			["calls"] = new JsonArray(((IEnumerable<BrainToolCall>)waiting.Calls).Select((Func<BrainToolCall, JsonNode>)((BrainToolCall c) => JsonValue.Create(c.Name, (JsonNodeOptions?)null))).ToArray()),
			["failures"] = new JsonArray(((IEnumerable<string>)failures).Select((Func<string, JsonNode>)((string f) => JsonValue.Create(f, (JsonNodeOptions?)null))).ToArray())
		});
		if (failures.Count == 0 || IsListeningNow)
		{
			return;
		}
		long generationNow;
		lock (gate)
		{
			generationNow = generation;
		}
		string fact = "[App] After your answer was told, this didn't work: " + string.Join("; ", failures) + ". Tell them in a few words.";
		Task.Run(async delegate
		{
			for (int i = 0; i < 40; i++)
			{
				if (Volatile.Read(in activeTurnCount) <= 0)
				{
					break;
				}
				await Task.Delay(50).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (Interlocked.Read(in generation) != generationNow || IsListeningNow)
			{
				return;
			}
			try
			{
				await RunAppFactTurnAsync(fact, waiting.Request.Typed, waiting.Request.AnswersInText).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Log.Warn("Companion", "the after-drawing failure turn failed: " + ex.Message);
			}
		});
	}

	private async Task RunAppFactTurnAsync(string fact, bool typed, bool answersInText)
	{
		InterruptForNewInput(clearDrawing: false);
		long turnGeneration;
		lock (gate)
		{
			turnGeneration = generation;
		}
		long startedTicks = Environment.TickCount64;
		SetState(VoiceState.Processing);
		await RunTurnAsync(new TurnRequest(FrontApp: options.DesktopActions.FrontApplication(), Words: null, Fact: fact, Typed: typed, AnswersInText: answersInText, Capture: await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false), AudioSeconds: null, Trigger: "app", StartedTicks: startedTicks, TranscriptMs: 0, WalkthroughStep: null), turnGeneration).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<IReadOnlyList<string>?> RunReportTurnAsync(string fact, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			string text = await auth.GetValidAccessTokenAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (text == null)
			{
				Log.Warn("Companion", "report turn: not signed in");
				return null;
			}
			brain.AdoptAccount(auth.UserId);
			brain.BeginTurnStartingNewSessionIfIdle();
			ScreenCaptureFrame screenCaptureFrame = new ScreenCaptureFrame(new DisplayInfo(0, "", new RectD(0.0, 0.0, 0.0, 0.0), new RectD(0.0, 0.0, 0.0, 0.0), 1.0, IsPrimary: true), Array.Empty<byte>(), 0, 0, ContainsCursor: false, new PointD(0.0, 0.0));
			TurnCapture capture = new TurnCapture(new _003C_003Ez__ReadOnlySingleElementList<ScreenCaptureFrame>(screenCaptureFrame), Array.Empty<DisplayInfo>(), null, 0);
			NarrationState narration = new NarrationState
			{
				WriteOnly = true
			};
			TurnRequest turnRequest = new TurnRequest(null, fact, Typed: false, AnswersInText: false, capture, (DisplayName: null, ProcessName: null), null, "app", Environment.TickCount64, 0, null)
			{
				Screenless = true
			};
			await StreamBrainAsync(turnRequest, capture, screenCaptureFrame, new List<ScreenCaptureFrame>(), text, narration, null, () => !ct.IsCancellationRequested, new TurnTimer(turnRequest.StartedTicks), ct).ConfigureAwait(continueOnCapturedContext: false);
			lock (narration.Gate)
			{
				string failure = narration.Failure;
				if (failure != null)
				{
					Log.Warn("Companion", "report turn failed: " + failure);
					return null;
				}
				if (string.IsNullOrWhiteSpace(narration.ProgramSoFar))
				{
					Log.Warn("Companion", "report turn failed: it wrote nothing");
					return null;
				}
				return (from l in SayLines.InOrder(narration.ProgramSoFar)
					select l.Trim() into l
					where l.Length > 0
					select l).ToList();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "report turn failed: " + ex.Message);
			return null;
		}
	}

	internal void BeginLessonRecording(string? question, ScreenCaptureFrame screen, string color)
	{
		bool flag = settings.Read((AppSettings s) => s.SaveDrawingsEffective);
		bool flag2 = settings.Read((AppSettings s) => s.KeepDrawingsInChatEffective);
		lock (gate)
		{
			lessonRecording = new LessonRecording(question ?? "", screen, color, flag, flag2);
			if ((flag || flag2) && !listensForLessonAudio)
			{
				listensForLessonAudio = true;
				mouth.LineAudioChunk += OnLessonLineAudio;
			}
		}
	}

	private void OnLessonLineAudio(int index, ReadOnlySpan<byte> pcm16)
	{
		if (index < 0)
		{
			return;
		}
		lock (gate)
		{
			LessonRecording lessonRecording = this.lessonRecording;
			if (lessonRecording != null && lessonRecording.SavesDrawing)
			{
				if (!lessonRecording.Audio.TryGetValue(index, out MemoryStream value))
				{
					value = (lessonRecording.Audio[index] = new MemoryStream());
				}
				value.Write(pcm16);
			}
		}
	}

	internal void NoteLessonLineHeard(int index)
	{
		lock (gate)
		{
			LessonRecording lessonRecording = this.lessonRecording;
			if (lessonRecording != null && !lessonRecording.Starts.ContainsKey(index))
			{
				lessonRecording.Starts[index] = (double)(Environment.TickCount64 - lessonRecording.StartedTicks) / 1000.0;
			}
		}
	}

	private void NoteLessonLineLength(int index, double seconds)
	{
		lock (gate)
		{
			LessonRecording lessonRecording = this.lessonRecording;
			if (lessonRecording != null && seconds > 0.0)
			{
				lessonRecording.Lengths[index] = seconds;
			}
		}
	}

	internal void FinishLessonRecording(IReadOnlyList<string> lines, string program)
	{
		LessonRecording lessonRecording;
		lock (gate)
		{
			lessonRecording = this.lessonRecording;
			this.lessonRecording = null;
		}
		if (lessonRecording == null)
		{
			return;
		}
		DrawingRecord drawingRecord = (lessonRecording.SavesDrawing ? LessonRecord(lessonRecording, lines, program, audio.SampleRate) : null);
		if ((object)drawingRecord == null)
		{
			try
			{
				this.DrawingLeftUp?.Invoke(this, lessonRecording.screen);
				return;
			}
			catch (Exception ex)
			{
				Log.Warn("Companion", "DrawingLeftUp handler: " + ex.Message);
				return;
			}
		}
		string obj = $"drawing told: \"{drawingRecord.Title}\", {drawingRecord.Lines.Count} lines, {drawingRecord.Duration:0.0} s, ";
		byte[] narration = drawingRecord.Narration;
		Log.Info("Companion", obj + ((narration != null) ? $"voice {(double)narration.Length / 2.0 / (double)drawingRecord.NarrationSampleRate:0.0} s" : "no voice (a line's audio is missing)"));
		try
		{
			this.DrawingTold?.Invoke(this, drawingRecord);
		}
		catch (Exception ex2)
		{
			Log.Warn("Companion", "DrawingTold handler: " + ex2.Message);
		}
	}

	private static DrawingRecord? LessonRecord(LessonRecording r, IReadOnlyList<string> lines, string program, int sampleRate)
	{
		if (lines.Count < (r.keepsInChat ? 1 : 3) || program.Trim().Length == 0)
		{
			return null;
		}
		if (!Enumerable.Range(0, lines.Count).All(r.Starts.ContainsKey))
		{
			return null;
		}
		MemoryStream value;
		bool hasEveryLineAudio = sampleRate > 0 && Enumerable.Range(0, lines.Count).All((int i) => r.Audio.TryGetValue(i, out value) && value.Length > 0);
		double value2;
		List<DrawingRecord.Line> list = lines.Select((string text, int i) => new DrawingRecord.Line(text, r.Starts[i], hasEveryLineAudio ? new double?((double)r.Audio[i].Length / 2.0 / (double)sampleRate) : (r.Lengths.TryGetValue(i, out value2) ? new double?(value2) : ((double?)null)))).ToList();
		double duration = list.Max((DrawingRecord.Line l) => l.Start + (l.Length ?? ReadingSeconds(l.Text))) + 0.8;
		byte[] narration = (hasEveryLineAudio ? LessonNarration.BuildTrack(list.Select((DrawingRecord.Line l) => l.Start).ToList(), (from i in Enumerable.Range(0, lines.Count)
			select r.Audio[i].ToArray()).ToList(), sampleRate, duration) : null);
		return new DrawingRecord(r.question, LessonTitle(program, lines[0]), program, list, duration, r.screen, r.color, DateTimeOffset.Now, narration, hasEveryLineAudio ? sampleRate : 0)
		{
			KeepsInChat = r.keepsInChat
		};
	}

	public static string LessonTitle(string program, string firstLine)
	{
		Match match = Regex.Match(program, "lessonTitle\\(\\s*(?:\"([^\"\\n]{1,80})\"|'([^'\\n]{1,80})')\\s*\\)");
		if (match.Success)
		{
			string text = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Trim();
			if (text.Length > 0)
			{
				return text;
			}
		}
		string[] array = firstLine.Trim().Trim('.', '!', '?', ',', ';', ':').Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string text2 = string.Join(' ', array.Take(7));
		if (array.Length <= 7)
		{
			return text2;
		}
		return text2 + "…";
	}

	private static NotchLiveEvent.MicrophoneTrouble CannotHearMicrophone(string? deviceName)
	{
		return new NotchLiveEvent.MicrophoneTrouble((string.IsNullOrWhiteSpace(deviceName) ? "Your microphone" : deviceName) + " isn't sending audio", "HeyClicky can't hear your microphone");
	}

	private static NotchLiveEvent.MicrophoneTrouble FailedOverToBuiltInMicrophone(string deadDeviceName)
	{
		return new NotchLiveEvent.MicrophoneTrouble(deadDeviceName + " stopped sending audio", "Switched to your PC's microphone", ShowsMicrophoneSettingsAction: false);
	}

	private void OnMicrophoneDeviceProvedDead(string deadDeviceName)
	{
		bool flag;
		lock (gate)
		{
			flag = isListening;
		}
		Log.Warn("Companion", "the mic's capture gave up on '" + deadDeviceName + "'" + (flag ? " mid-press" : ""));
		MicrophoneProvedDead(deadDeviceName);
	}

	private void MicrophoneProvedDead(string deadDeviceName)
	{
		string text;
		try
		{
			text = microphone.FailOverToBuiltInMicrophone(deadDeviceName);
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "mic failover: " + ex.Message);
			text = null;
		}
		if (text == null)
		{
			if (!hasReportedIrrecoverableMicrophone)
			{
				hasReportedIrrecoverableMicrophone = true;
				Log.Warn("Companion", "mic irrecoverable: '" + deadDeviceName + "' dead with no failover left");
				ShowLiveEvent(CannotHearMicrophone(deadDeviceName));
			}
		}
		else
		{
			Interlocked.Exchange(ref consecutiveDeadAirPresses, 0);
			Log.Warn("Companion", $"mic failover: '{deadDeviceName}' proved dead, using '{text}'");
			ShowLiveEvent(FailedOverToBuiltInMicrophone(deadDeviceName));
		}
	}

	private void HoldBluetoothMicrophoneOpenUntilTheReplyPlaysOut()
	{
		CancellationTokenSource hold = new CancellationTokenSource();
		Interlocked.Exchange(ref bluetoothHold, hold)?.Cancel();
		Task.Run(async delegate
		{
			long mutedAt = Environment.TickCount64;
			bool sawReply = false;
			long? quietSince = null;
			try
			{
				while (!hold.IsCancellationRequested)
				{
					await Task.Delay(250, hold.Token).ConfigureAwait(continueOnCapturedContext: false);
					long tickCount = Environment.TickCount64;
					VoiceState voiceState = State;
					if ((uint)(voiceState - 2) <= 1u)
					{
						sawReply = true;
						quietSince = null;
					}
					else
					{
						quietSince.GetValueOrDefault();
						if (!quietSince.HasValue)
						{
							long value = tickCount;
							quietSince = value;
						}
					}
					double num;
					if (quietSince.HasValue)
					{
						long valueOrDefault = quietSince.GetValueOrDefault();
						num = (double)(tickCount - valueOrDefault) / 1000.0;
					}
					else
					{
						num = 0.0;
					}
					double num2 = num;
					double num3 = (double)(tickCount - mutedAt) / 1000.0;
					if ((sawReply && num2 >= 5.0) || (!sawReply && num3 >= 20.0) || num3 >= 300.0)
					{
						if (Interlocked.CompareExchange(ref bluetoothHold, null, hold) == hold)
						{
							Log.Info("Companion", $"closing the held Bluetooth mic ({(sawReply ? "the reply played out" : "no reply came")}, {num3:0.0} s)");
							microphone.ReleaseHeldDevice();
						}
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
		});
	}

	private void EndBluetoothHold(bool release)
	{
		CancellationTokenSource cancellationTokenSource = Interlocked.Exchange(ref bluetoothHold, null);
		if (cancellationTokenSource != null)
		{
			cancellationTokenSource.Cancel();
			if (release)
			{
				microphone.ReleaseHeldDevice();
			}
		}
	}

	private void RaiseOneBrainTurnEnded()
	{
		try
		{
			this.OneBrainTurnEnded?.Invoke();
		}
		catch (Exception ex)
		{
			Log.Warn("Companion", "OneBrainTurnEnded handler threw: " + ex.Message);
		}
	}

	public async Task<PrefetchedTurn?> PreparePrefetchedFactTurnAsync(string fact)
	{
		TurnCapture capture = await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false);
		string text = await auth.GetValidAccessTokenAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (capture.Frames.Count == 0 || text == null)
		{
			return null;
		}
		return new PrefetchedTurn(fact, capture, text, options.DesktopActions.FrontApplication(), auth.UserId);
	}

	public bool StartPrefetchedTurn(PrefetchedTurn turn, JsonObject? flowBrief)
	{
		lock (prefetchGate)
		{
			if ((object)turn.Request != null || Volatile.Read(in activeTurnCount) > 0 || pendingPrefetchedTurn != null)
			{
				return false;
			}
			pendingPrefetchedTurn = turn;
			turn.Request = new TurnRequest(null, turn.Fact, Typed: false, AnswersInText: false, turn.Capture, turn.FrontApp, null, "app", Environment.TickCount64, 0, null)
			{
				FlowBrief = flowBrief
			};
			turn.StartedAt = DateTimeOffset.Now;
		}
		TurnRequest request = turn.Request;
		brain.AdoptAccount(auth.UserId);
		brain.BeginTurnStartingNewSessionIfIdle();
		mouth.EnsureOpenAsync();
		TurnCapture capture = turn.Capture;
		ScreenCaptureFrame cursorFrame = capture.Frames.FirstOrDefault((ScreenCaptureFrame f) => f.ContainsCursor) ?? capture.Frames[0];
		List<ScreenCaptureFrame> otherFrames = capture.Frames.Where((ScreenCaptureFrame f) => f != cursorFrame).ToList();
		turn.Streaming = Task.Run(() => StreamBrainAsync(request, capture, cursorFrame, otherFrames, turn.AccessToken, turn.Narration, null, turn.StillCurrent, new TurnTimer(request.StartedTicks), turn.Token, turn));
		turn.Streaming.ContinueWith(delegate(Task t)
		{
			_ = t.Exception;
		}, TaskContinuationOptions.OnlyOnFaulted);
		return true;
	}

	public bool AdoptPrefetchedTurn(PrefetchedTurn turn)
	{
		lock (prefetchGate)
		{
			if (turn.IsDropped || turn.IsAdopted || (object)turn.Request == null)
			{
				return false;
			}
			if (pendingPrefetchedTurn == turn)
			{
				pendingPrefetchedTurn = null;
			}
			turn.MarkAdopted();
		}
		BrainTurnTiming.DiscardCurrent();
		return true;
	}

	public void DropPrefetchedTurn(string reason)
	{
		lock (prefetchGate)
		{
			PrefetchedTurn prefetchedTurn = pendingPrefetchedTurn;
			if (prefetchedTurn == null)
			{
				return;
			}
			pendingPrefetchedTurn = null;
			prefetchedTurn.Drop();
		}
		Log.Info("Companion", "prefetched turn dropped: " + reason);
	}

	public async Task PlayPrefetchedTurnAsync(PrefetchedTurn turn)
	{
		TurnRequest request = turn.Request;
		if ((object)request != null && turn.IsAdopted)
		{
			InterruptForNewInput(clearDrawing: false);
			long turnGeneration;
			lock (gate)
			{
				turnGeneration = generation;
			}
			http.PrewarmWorker();
			SetState(VoiceState.Processing);
			await RunTurnAsync(request, turnGeneration, turn).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public void SimulatePressForTest(PointD point)
	{
		long num;
		lock (gate)
		{
			num = guidedTarget?.Id ?? (-1);
		}
		if (num >= 0)
		{
			OnGuidedPress(num, point);
		}
	}

	private async Task ArmGuidedTargetIfLastBeatAsync(NarrationState narration, DrawingAction action, PointD point, RectD? frame, TurnRequest request, Func<bool> stillCurrent)
	{
		int line = ActionLine(action);
		long deadline = Environment.TickCount64 + 8000;
		while (stillCurrent() && Environment.TickCount64 < deadline)
		{
			lock (narration.Gate)
			{
				if (narration.Finished || narration.Lines.Count > line + 1)
				{
					break;
				}
				goto IL_0093;
			}
			IL_0093:
			await WaitForChangeAsync(100, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!stillCurrent())
		{
			return;
		}
		string spokenInstruction;
		lock (narration.Gate)
		{
			if (narration.Lines.Count > line + 1)
			{
				Log.Info("Companion", $"click target in beat {line + 1} skipped: more beats follow");
				return;
			}
			spokenInstruction = ((line < narration.Lines.Count) ? narration.Lines[line] : "");
		}
		string text = PayloadString(action.Payload, "label");
		int valueOrDefault = request.WalkthroughStep.GetValueOrDefault();
		JsonElement value;
		double value2;
		double val = ((action.Payload.TryGetProperty("r", out value) && value.TryGetDouble(out value2)) ? value2 : 0.0);
		ClickyAnalytics.TrackAnnotationMarkShown("screen_drawing", text, action.Kind == "hover", (int)Math.Round(Math.Max(val, 14.0)));
		if (valueOrDefault >= 15)
		{
			Log.Info("Companion", $"guided target suppressed: step {valueOrDefault} is the walkthrough's last (label \"{text}\")");
			return;
		}
		TurnCapture capture = request.Capture;
		object obj;
		if ((object)capture != null)
		{
			IReadOnlyList<ScreenCaptureFrame> frames = capture.Frames;
			if (frames != null && frames.Count > 0)
			{
				obj = capture.Frames.FirstOrDefault((ScreenCaptureFrame f) => f.ContainsCursor) ?? capture.Frames[0];
				goto IL_0358;
			}
		}
		obj = null;
		goto IL_0358;
		IL_0358:
		ScreenCaptureFrame screenCaptureFrame = (ScreenCaptureFrame)obj;
		if ((object)screenCaptureFrame == null)
		{
			return;
		}
		DisplayInfo display = screenCaptureFrame.Display;
		double num = Math.Max(0.5, display.Scale);
		double num2 = Math.Max(display.BoundsPx.Width / (double)Math.Max(1, screenCaptureFrame.PixelWidth), display.BoundsPx.Height / (double)Math.Max(1, screenCaptureFrame.PixelHeight));
		double radiusPx = Math.Clamp(Math.Max(val, 14.0) * num2, 28.0 * num, 180.0 * num);
		PointD centerPx = point;
		if (frame.HasValue)
		{
			RectD valueOrDefault2 = frame.GetValueOrDefault();
			centerPx = new PointD(valueOrDefault2.X + valueOrDefault2.Width / 2.0, valueOrDefault2.Y + valueOrDefault2.Height / 2.0);
			radiusPx = Math.Max(28.0 * num, Math.Sqrt(valueOrDefault2.Width * valueOrDefault2.Width + valueOrDefault2.Height * valueOrDefault2.Height) / 2.0);
		}
		string frontProcess = IfNotOurs(options.DesktopActions.FrontApplication().ProcessName) ?? IfNotOurs(request.FrontApp.ProcessName);
		GuidedTarget target = new GuidedTarget(Interlocked.Increment(ref guidedTargetIds), centerPx, radiusPx, frame, display.BoundsPx, num, text, spokenInstruction, valueOrDefault, action.Kind == "hover", Environment.TickCount64, request.Typed, request.AnswersInText, frontProcess);
		ArmGuidedTarget(target);
		lock (narration.Gate)
		{
			narration.TargetArmed = true;
		}
		static string? IfNotOurs(string? process)
		{
			if (process == null || process.Length <= 0 || process.Equals(OwnProcessName, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			return process;
		}
	}

	private void ArmGuidedTarget(GuidedTarget target)
	{
		DisarmAdvance();
		DisarmGuidedTarget(null);
		CancellationTokenSource loop = new CancellationTokenSource();
		lock (gate)
		{
			guidedTarget = target;
			guidedWatchLoop = loop;
		}
		IPointerInput pointerInput = options.PointerInput;
		if (pointerInput != null)
		{
			guidedPressWatch = pointerInput.WatchPresses(delegate(PointD press)
			{
				OnGuidedPress(target.Id, press);
			});
		}
		else
		{
			Log.Warn("Companion", "no pointer input: the click target can't see clicks");
		}
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(62, 7);
		defaultInterpolatedStringHandler.AppendLiteral("guided target arm label=\"");
		defaultInterpolatedStringHandler.AppendFormatted(target.Label ?? "target");
		defaultInterpolatedStringHandler.AppendLiteral("\" x=");
		defaultInterpolatedStringHandler.AppendFormatted(target.CenterPx.X, "0");
		defaultInterpolatedStringHandler.AppendLiteral(" y=");
		defaultInterpolatedStringHandler.AppendFormatted(target.CenterPx.Y, "0");
		defaultInterpolatedStringHandler.AppendLiteral(" radius=");
		defaultInterpolatedStringHandler.AppendFormatted(target.RadiusPx, "0");
		defaultInterpolatedStringHandler.AppendLiteral(" ");
		defaultInterpolatedStringHandler.AppendLiteral("control=");
		RectD? controlFramePx = target.ControlFramePx;
		object value;
		if (controlFramePx.HasValue)
		{
			RectD valueOrDefault = controlFramePx.GetValueOrDefault();
			value = $"{valueOrDefault.Width:0}x{valueOrDefault.Height:0}";
		}
		else
		{
			value = "none";
		}
		defaultInterpolatedStringHandler.AppendFormatted((string?)value);
		defaultInterpolatedStringHandler.AppendLiteral(" step=");
		defaultInterpolatedStringHandler.AppendFormatted(target.Step);
		defaultInterpolatedStringHandler.AppendLiteral(" hover=");
		defaultInterpolatedStringHandler.AppendFormatted(target.IsHover);
		Log.Info("Companion", defaultInterpolatedStringHandler.ToStringAndClear());
		ClickyAnalytics.TrackAnnotationTargetArmed(target.Label, target.IsHover, target.Step);
		Task.Run(() => WatchGuidedTargetAsync(target, loop.Token));
		ClickTargetRing ring = new ClickTargetRing(target.CenterPx, target.RadiusPx, target.ControlFramePx, target.Label);
		Task.Delay(450).ContinueWith(delegate
		{
			if (IsArmed(target.Id))
			{
				ui.Post(delegate
				{
					cursor.ShowClickTarget(ring);
				});
			}
		}, TaskScheduler.Default);
	}

	private bool IsArmed(long id)
	{
		lock (gate)
		{
			GuidedTarget? obj = guidedTarget;
			return (object)obj != null && obj.Id == id;
		}
	}

	private void DisarmGuidedTarget(string? reason)
	{
		GuidedTarget guidedTarget;
		IDisposable disposable;
		CancellationTokenSource cancellationTokenSource;
		lock (gate)
		{
			guidedTarget = this.guidedTarget;
			disposable = guidedPressWatch;
			cancellationTokenSource = guidedWatchLoop;
			this.guidedTarget = null;
			guidedPressWatch = null;
			guidedWatchLoop = null;
		}
		if ((object)guidedTarget != null)
		{
			cancellationTokenSource?.Cancel();
			disposable?.Dispose();
			ui.Post(delegate
			{
				cursor.HideClickTarget();
			});
			if (reason != null)
			{
				Log.Info("Companion", "guided target cleared label=\"" + (guidedTarget.Label ?? "target") + "\": " + reason);
			}
		}
	}

	private async Task WatchGuidedTargetAsync(GuidedTarget target, CancellationToken ct)
	{
		double restSeconds = 0.0;
		int ticks = 0;
		try
		{
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(120, ct).ConfigureAwait(continueOnCapturedContext: false);
				if (!IsArmed(target.Id))
				{
					break;
				}
				if ((double)(Environment.TickCount64 - target.ArmedTicks) / 1000.0 > 600.0)
				{
					DisarmGuidedTarget("expired");
					break;
				}
				int num = ticks + 1;
				ticks = num;
				if (num % 4 == 0)
				{
					string frontProcess = target.FrontProcess;
					if (frontProcess != null && frontProcess.Length > 0)
					{
						string item = options.DesktopActions.FrontApplication().ProcessName;
						if (item != null && item.Length > 0 && !item.Equals(frontProcess, StringComparison.OrdinalIgnoreCase) && !item.Equals(OwnProcessName, StringComparison.OrdinalIgnoreCase))
						{
							DisarmGuidedTarget("another app came to the front (" + item + ")");
							break;
						}
					}
				}
				if (!target.IsHover)
				{
					continue;
				}
				IPointerInput pointerInput = options.PointerInput;
				if (pointerInput != null)
				{
					PointD cursorPosition = pointerInput.CursorPosition;
					restSeconds = (target.Contains(cursorPosition) ? (restSeconds + 0.12) : 0.0);
					if (restSeconds >= 2.0)
					{
						Log.Info("Companion", "guided hover-rest complete label=\"" + (target.Label ?? "target") + "\"");
						CompleteGuidedTarget(target, cursorPosition, completedByHoverRest: true);
						break;
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
	}

	private void OnGuidedPress(long targetId, PointD press)
	{
		GuidedTarget guidedTarget;
		lock (gate)
		{
			guidedTarget = this.guidedTarget;
		}
		if ((object)guidedTarget != null && guidedTarget.Id == targetId)
		{
			if ((double)(Environment.TickCount64 - guidedTarget.ArmedTicks) / 1000.0 > 600.0 || guidedTarget.Step >= 15)
			{
				DisarmGuidedTarget("expired");
			}
			else if (!guidedTarget.Contains(press))
			{
				string text = guidedTarget.Label ?? "the target";
				Log.Info("Companion", $"guided target off-target click at {press.X:0},{press.Y:0}: ending the guide (label \"{text}\")");
				brain.NoteForNextTurn("During a guided step I pointed at \"" + text + "\" and asked them to click it, but they clicked elsewhere — the guide ended and that step was likely not done.");
				RectD displayBoundsPx = guidedTarget.DisplayBoundsPx;
				bool sameDisplay = new RectD(displayBoundsPx.X - guidedTarget.RadiusPx, displayBoundsPx.Y - guidedTarget.RadiusPx, displayBoundsPx.Width + 2.0 * guidedTarget.RadiusPx, displayBoundsPx.Height + 2.0 * guidedTarget.RadiusPx).Contains(press);
				ClickyAnalytics.TrackAnnotationTargetMissed(guidedTarget.Label, 1, sameDisplay);
				DisarmGuidedTarget("clicked elsewhere");
			}
			else
			{
				CompleteGuidedTarget(guidedTarget, press, completedByHoverRest: false);
			}
		}
	}

	private void CompleteGuidedTarget(GuidedTarget target, PointD press, bool completedByHoverRest)
	{
		if (!IsArmed(target.Id))
		{
			return;
		}
		DisarmGuidedTarget(null);
		int step = target.Step + 1;
		string value = target.Label ?? "target";
		Log.Info("Companion", $"guided target hit label=\"{value}\" distance={Distance(press, target.CenterPx):0}px hoverRest={completedByHoverRest}: step {step} follows");
		ClickyAnalytics.TrackAnnotationTargetCompleted(target.Label, target.IsHover, completedByHoverRest, target.Step);
		ClickyAnalytics.TrackAnnotationFollowUpRequested(clickedOnTarget: true, step);
		InterruptForNewInput(clearDrawing: true);
		long turnGeneration;
		lock (gate)
		{
			turnGeneration = generation;
		}
		SetState(VoiceState.Processing);
		string fact = $"[App] The user {(completedByHoverRest ? "rested the cursor on" : "clicked")} \"{value}\", the step your drawing asked for, and the screen changed.";
		WatchForPassiveExitWhileTheNextStepLoads(turnGeneration, target.FrontProcess);
		Task.Run(async delegate
		{
			_ = 2;
			try
			{
				await ScreenSettle.WaitForScreenToSettleAsync(() => CaptureDisplayJpegAsync(target.DisplayBoundsPx), PostClickCaptureFallback, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				if (Interlocked.Read(in generation) == turnGeneration)
				{
					(string? DisplayName, string? ProcessName) frontApp = options.DesktopActions.FrontApplication();
					TurnCapture capture = await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false);
					await RunTurnAsync(new TurnRequest(null, fact, target.Typed, target.AnswersInText, capture, frontApp, null, "clicked_target", Environment.TickCount64, 0, step), turnGeneration).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Companion", "guided follow-up failed", ex);
				FinishTurnQuietly(Interlocked.Read(in generation) == turnGeneration);
			}
		});
	}

	private void WatchForPassiveExitWhileTheNextStepLoads(long turnGeneration, string? sourceProcess)
	{
		Interlocked.Exchange(ref guidedStepLoadingGeneration, turnGeneration);
		if (sourceProcess == null || sourceProcess.Length <= 0)
		{
			return;
		}
		long graceEnds = Environment.TickCount64 + (long)PassiveExitGrace.TotalMilliseconds;
		Task.Run(async delegate
		{
			while (Interlocked.Read(in guidedStepLoadingGeneration) == turnGeneration && Interlocked.Read(in generation) == turnGeneration)
			{
				await Task.Delay(120).ConfigureAwait(continueOnCapturedContext: false);
				string item = options.DesktopActions.FrontApplication().ProcessName;
				if (item != null && item.Length > 0 && !item.Equals(sourceProcess, StringComparison.OrdinalIgnoreCase) && !item.Equals(OwnProcessName, StringComparison.OrdinalIgnoreCase))
				{
					if (Environment.TickCount64 >= graceEnds)
					{
						if (Interlocked.Read(in guidedStepLoadingGeneration) == turnGeneration && Interlocked.Read(in generation) == turnGeneration)
						{
							Log.Info("Companion", "guided follow-up passive exit (app switch to " + item + "): staying quiet");
							PassivelyExitGuidedFollowUp();
						}
						break;
					}
					Log.Info("Companion", "guided follow-up: " + item + " came to the front during the post-target grace (the step caused it): keeps going");
				}
			}
		});
	}

	private void NoteGuidedStepShowed()
	{
		Interlocked.Exchange(ref guidedStepLoadingGeneration, -1L);
	}

	private void PassivelyExitGuidedFollowUp()
	{
		Interlocked.Exchange(ref guidedStepLoadingGeneration, -1L);
		InterruptForNewInput(clearDrawing: false);
		brain.ForgetDrawingOnScreen();
		ui.Post(delegate
		{
			canvas.IsStreamingNarration = false;
			canvas.Clear(animated: false);
		});
		NotifyTerminating();
		SetState(VoiceState.Idle);
	}

	private async Task<byte[]?> CaptureDisplayJpegAsync(RectD displayBounds)
	{
		IReadOnlyList<ScreenCaptureFrame> source = await screens.CaptureAllAsync(256, 60).ConfigureAwait(continueOnCapturedContext: false);
		return (source.FirstOrDefault((ScreenCaptureFrame f) => f.Display.BoundsPx == displayBounds) ?? source.FirstOrDefault())?.Jpeg;
	}

	private void NoteTurnEndedForWalkthrough(NarrationState narration, TurnRequest request, ScreenCaptureFrame screen)
	{
		if ((object)ArmedClickTarget != null)
		{
			DisarmAdvance();
			return;
		}
		List<string> lines;
		int marks;
		lock (narration.Gate)
		{
			lines = narration.Lines.Skip(narration.FirstLine).ToList();
			marks = narration.Marks;
		}
		if (!ShouldArmAdvance(lines, marks > 0))
		{
			DisarmAdvance();
			return;
		}
		lock (gate)
		{
			advanceArmedUntilTicks = Environment.TickCount64 + 120000;
			advanceTurn = request;
			advanceBaseline = screen;
		}
		Log.Info("Companion", "a step with no click target: the next click or scroll that changes the screen moves on");
	}

	internal static bool ShouldArmAdvance(IReadOnlyList<string> lines, bool drewMarks)
	{
		if (!drewMarks || lines.Count == 0)
		{
			return false;
		}
		string text = lines[lines.Count - 1].Trim();
		if (text.Length > 0 && !text.EndsWith('?'))
		{
			return StepInstructionPattern.IsMatch(text);
		}
		return false;
	}

	private void DisarmAdvance()
	{
		lock (gate)
		{
			advanceArmedUntilTicks = 0L;
			advanceTurn = null;
			advanceBaseline = null;
		}
	}

	private void OnCanvasUserActedOnScreen(object? sender, string reason)
	{
		TurnRequest turn;
		ScreenCaptureFrame baseline;
		lock (gate)
		{
			if ((object)advanceTurn == null || Environment.TickCount64 > advanceArmedUntilTicks || isListening)
			{
				return;
			}
			turn = advanceTurn;
			baseline = advanceBaseline;
		}
		if ((object)baseline == null)
		{
			return;
		}
		Task.Run(async delegate
		{
			_ = 5;
			try
			{
				for (int i = 0; i < 300; i++)
				{
					if (!(options.PointerInput?.IsAnyButtonDown ?? false))
					{
						break;
					}
					await Task.Delay(100).ConfigureAwait(continueOnCapturedContext: false);
				}
				await ScreenSettle.WaitForScreenToSettleAsync(() => CaptureDisplayJpegAsync(baseline.Display.BoundsPx), TimeSpan.FromMilliseconds(800L, 0L), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				ScreenCaptureFrame screenCaptureFrame = (await screens.CaptureAllAsync().ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault((ScreenCaptureFrame f) => f.Display.BoundsPx == baseline.Display.BoundsPx);
				bool flag = (object)screenCaptureFrame == null;
				if (!flag)
				{
					flag = await ScreenSettle.LookTheSameAsync(baseline.Jpeg, screenCaptureFrame.Jpeg, 0.03).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (flag)
				{
					Log.Info("Companion", "the " + reason + " left the screen as it was: not a step");
				}
				else
				{
					lock (gate)
					{
						if (advanceTurn != turn || Environment.TickCount64 > advanceArmedUntilTicks || isListening)
						{
							return;
						}
						advanceArmedUntilTicks = 0L;
						advanceTurn = null;
						advanceBaseline = null;
					}
					Log.Info("Companion", "the user acted on screen (" + reason + "): moving on");
					InterruptForNewInput(clearDrawing: false);
					long turnGeneration;
					lock (gate)
					{
						turnGeneration = generation;
					}
					SetState(VoiceState.Processing);
					string text = ((reason == "scroll") ? "they scrolled" : "they clicked or dragged");
					string fact = "[App] The user acted on their screen after your last step (" + text + "). If they did the step, say so in a few words and give the next one; if not, help them with it.";
					(string? DisplayName, string? ProcessName) frontApp = options.DesktopActions.FrontApplication();
					TurnCapture capture = await CaptureForTurnAsync(endHighlight: false).ConfigureAwait(continueOnCapturedContext: false);
					await RunTurnAsync(new TurnRequest(null, fact, turn.Typed, turn.AnswersInText, capture, frontApp, null, "did_step", Environment.TickCount64, 0, null), turnGeneration).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Companion", "the did-the-step turn failed", ex);
			}
		});
	}

	private static double Distance(PointD a, PointD b)
	{
		return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
	}
}
