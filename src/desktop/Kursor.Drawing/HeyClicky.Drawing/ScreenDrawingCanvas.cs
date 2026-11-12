using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Drawing.Audio;
using HeyClicky.Drawing.Engine;
using HeyClicky.Drawing.Media;
using HeyClicky.Drawing.Overlay;
using HeyClicky.Drawing.Painter;
using HeyClicky.Drawing3D;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Dispatching;
using Windows.Graphics;
using Windows.Storage.Streams;
using Windows.UI.Composition;

namespace HeyClicky.Drawing;

public sealed class ScreenDrawingCanvas : IScreenDrawingCanvas, IDisposable
{
	private sealed class StandaloneButtonHandle(ScreenDrawingCanvas canvas) : IDisposable
	{
		public ActionButtons.ButtonWindow? Window;

		public bool IsDisposed;

		public void Dispose()
		{
			IsDisposed = true;
			canvas.PostToDrawingThread(delegate
			{
				Window?.Retire();
			});
		}
	}

	private readonly DrawingThread thread;

	private readonly DispatcherQueue? eventQueue;

	private readonly PhotoLoader photoLoader;

	private readonly Task initialized;

	private Compositor? compositor;

	private OverlayWindow? overlay;

	private ScreenPainter? painter;

	private NativeEngine? engine;

	private ActionButtons? buttons;

	private CardHandles? cardHandles;

	private PinnedCards? pinnedCards;

	private DrawingCaption? caption;

	private LiveCaption? liveCaption;

	private volatile bool isStreamingNarration;

	private bool isShowing;

	private DisplayInfo? display;

	private Vector2 canvasSize;

	private Task<(byte[] Pixels, int Width, int Height)?>? screenshotDecode;

	private int lastReleasedLine = -1;

	private readonly HashSet<int> deliveredActionMarks = new HashSet<int>();

	private readonly HashSet<string> prefetchedPhotos = new HashSet<string>();

	private List<(string Role, int Line, int Mark, JsonElement Payload)> knownActions = new List<(string, int, int, JsonElement)>();

	private int clearGeneration;

	private int drawnMarkCount;

	private byte[]? screenshotJpeg;

	private RectD drawingDisplayBounds;

	private volatile bool latestAnswerPointsAtScreen;

	private IDisposable? staleWatch;

	private int staleWatchGeneration;

	private bool defersScreenChangesUntilFirstLine;

	private volatile bool drawsPicture;

	private volatile int inkedMarkCount;

	private bool isNotesOnly;

	private volatile bool isDrawingFadedAway;

	private long latestLineReleasedTicks;

	private long narrationAudioEndsTicks;

	private static readonly JsonElement EmptyRaw = JsonDocument.Parse("{}").RootElement.Clone();

	private static readonly Regex WebSearchCitationPattern = new Regex("\\(?\\[[^\\]\\n]{1,200}\\]\\(https?://[^\\s)]{1,2000}\\)\\)?", RegexOptions.Compiled);

	internal static string? DownloadsFolderOverride;

	public static double Now => DrawingThread.Now;

	public static bool ExcludeFromCapture
	{
		get
		{
			return OverlayWindow.ExcludeFromCapture;
		}
		set
		{
			OverlayWindow.ExcludeFromCapture = value;
		}
	}

	public static bool ShowInScreenRecordings
	{
		get
		{
			return OverlayWindow.ShowInScreenRecordings;
		}
		set
		{
			OverlayWindow.ShowInScreenRecordings = value;
		}
	}

	public bool IsShowing => isShowing;

	public bool PlaysSounds { get; set; } = true;

	public bool PlaysPenSounds { get; set; }

	public static bool JavaScriptJitEnabled
	{
		get
		{
			return !NativeEngine.DisableJit;
		}
		set
		{
			NativeEngine.DisableJit = !value;
		}
	}

	public static bool FiguresFloat
	{
		get
		{
			return NativeEngine.FiguresFloat;
		}
		set
		{
			NativeEngine.FiguresFloat = value;
		}
	}

	public bool ShowsSubtitles { get; set; }

	public Func<DisplayInfo, Action<string>, IDisposable>? ScreenChangeWatchFactory { get; set; }

	public string? ReplayPhotosFolder { get; set; }

	public double EngineLoadMilliseconds => engine?.LoadMilliseconds ?? 0.0;

	public long EngineHeapBytes => engine?.HeapBytes ?? 0;

	internal static string AssetsDirectory
	{
		get
		{
			string text = Path.Combine(AppContext.BaseDirectory, "HeyClicky.Drawing", "Assets");
			if (!Directory.Exists(text))
			{
				return Path.Combine(AppContext.BaseDirectory, "Assets");
			}
			return text;
		}
	}

	public bool IsStreamingNarration
	{
		get
		{
			return isStreamingNarration;
		}
		set
		{
			if (isStreamingNarration == value)
			{
				return;
			}
			isStreamingNarration = value;
			if (value)
			{
				Interlocked.Exchange(ref latestLineReleasedTicks, 0L);
				Interlocked.Exchange(ref narrationAudioEndsTicks, 0L);
			}
			PostToDrawingThread(delegate
			{
				if (value)
				{
					cardHandles.LeaveFullScreen();
					cardHandles.Hide();
				}
				else if (isShowing && !isDrawingFadedAway)
				{
					cardHandles.Refresh();
				}
			});
		}
	}

	public bool IsShowingDrawing
	{
		get
		{
			if (isShowing)
			{
				return drawnMarkCount > 0;
			}
			return false;
		}
	}

	public bool HidesLoneSources { get; set; }

	public int InkedMarkCount => inkedMarkCount;

	public bool DrawsPicture => drawsPicture;

	public string LiveCaptionText => OnDrawingThread(() => liveCaption?.ShownText ?? "", "");

	public Func<nint>? NotchWindowProvider { get; set; }

	public bool IsDrawingFadedAway => isDrawingFadedAway;

	public DisplayInfo? DrawingDisplay
	{
		get
		{
			if (!isShowing)
			{
				return null;
			}
			return display;
		}
	}

	public DateTime? LatestLineReleasedAt
	{
		get
		{
			long num = Interlocked.Read(in latestLineReleasedTicks);
			if (num <= 0)
			{
				return null;
			}
			return new DateTime(num, DateTimeKind.Utc);
		}
	}

	public DateTime? NarrationAudioEndsAt
	{
		get
		{
			long num = Interlocked.Read(in narrationAudioEndsTicks);
			if (num <= 0)
			{
				return null;
			}
			return new DateTime(num, DateTimeKind.Utc);
		}
		set
		{
			Interlocked.Exchange(ref narrationAudioEndsTicks, value?.ToUniversalTime().Ticks ?? 0);
		}
	}

	public bool LatestAnswerPointsAtScreen => latestAnswerPointsAtScreen;

	public bool IsSwingingFiguresToFront => OnDrawingThread(() => painter.Figures?.IsSwingingToFront ?? false, fallback: false);

	internal int PainterUnitCount => OnDrawingThread(() => painter.UnitCount, 0);

	internal long TimerFires => thread.TimerFires;

	internal string PainterActivity => OnDrawingThread(() => $"masks applied {painter.AppliedMaskCount}, animations still running {painter.RecordedAnimationCount}", "");

	internal Task Ready => initialized;

	internal int PinnedCardCount => OnDrawingThread(() => pinnedCards?.Count ?? 0, 0);

	internal nint OverlayHandle => overlay?.Handle ?? IntPtr.Zero;

	internal static bool ShowsDownloadButtonForHarness
	{
		get
		{
			return WindowButtonShapes.OverrideDownloadButton;
		}
		set
		{
			WindowButtonShapes.OverrideDownloadButton = value;
		}
	}

	internal string? LastDownloadPath => OnDrawingThread(() => cardHandles?.LastDownloadPath, null);

	public string CaptionText => OnDrawingThread(() => caption?.ShownText ?? "", "");

	public event EventHandler<DrawingAction>? ActionRequested;

	public event EventHandler<IReadOnlyList<PenStrokePath>>? PenStrokes;

	public event EventHandler? Cleared;

	public event EventHandler<string>? UserActedOnScreen;

	public event EventHandler<string>? CaptionChanged;

	public event EventHandler? PenHandedBack;

	public static void WarmUpFigures()
	{
		Drawing3DAdapter.WarmUpForTurn();
	}

	public static Task<bool> HideFromOwnCaptureAsync(bool hidden)
	{
		return OverlayWindow.SetHiddenFromOwnCaptureAsync(hidden);
	}

	public IReadOnlyDictionary<string, byte[]> KnownPhotos(string program)
	{
		return photoLoader.KnownPhotos(program);
	}

	public Task<byte[]?> PhotoAsync(string query, CancellationToken cancellationToken, string? savedPhotosFolder)
	{
		return photoLoader.ImageAsync(query, cancellationToken, savedPhotosFolder);
	}

	public ScreenDrawingCanvas(Func<CancellationToken, Task<string?>> accessTokenProvider, Uri workerBaseUrl, DispatcherQueue? eventQueue = null, Action<HttpRequestMessage>? configureWorkerRequest = null)
	{
		ScreenDrawingCanvas screenDrawingCanvas = this;
		this.eventQueue = eventQueue;
		photoLoader = new PhotoLoader(accessTokenProvider, workerBaseUrl, configureWorkerRequest);
		thread = new DrawingThread("Clicky drawing");
		TaskCompletionSource ready = new TaskCompletionSource();
		initialized = ready.Task;
		thread.Post(delegate
		{
			try
			{
				screenDrawingCanvas.Initialize();
			}
			catch (Exception ex)
			{
				Log.Error("Drawing", "the drawing canvas failed to start", ex);
			}
			finally
			{
				ready.TrySetResult();
			}
		});
	}

	private void Initialize()
	{
		double now = DrawingThread.Now;
		compositor = new Compositor();
		GpuMemoryProbe.Install();
		CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
		string assetsDirectory = AssetsDirectory;
		TextShaper textShaper = new TextShaper(sharedDevice, Path.Combine(assetsDirectory, "Fonts"));
		Drawing3DAdapter drawing3DAdapter = new Drawing3DAdapter
		{
			Thread = thread,
			Compositor = compositor
		};
		painter = new ScreenPainter(compositor, thread, sharedDevice, textShaper)
		{
			Figures = drawing3DAdapter
		};
		drawing3DAdapter.CanvasPlacement = () => ((RectInt32 Bounds, double PixelsPerUnit))(Bounds: overlay.BoundsPx, PixelsPerUnit: (canvasSize.X > 0f) ? ((float)overlay.BoundsPx.Width / canvasSize.X) : 1f);
		drawing3DAdapter.CanvasWindow = () => overlay?.Handle ?? IntPtr.Zero;
		drawing3DAdapter.PartClicked += delegate(JsonElement click)
		{
			Raise(delegate
			{
				this.ActionRequested?.Invoke(this, new DrawingAction("figurePart", click));
			});
		};
		painter.PhotoProvider = (string query, CancellationToken token) => photoLoader.ImageAsync(query, token, ReplayPhotosFolder);
		painter.PenStrokesReady += OnPenStrokes;
		overlay = new OverlayWindow(compositor, "Clicky drawing");
		overlay.Root.Children.InsertAtTop(painter.StageVisual);
		engine = new NativeEngine(thread, painter, Path.Combine(assetsDirectory, "Engine"), SoundPlayer.Shared);
		caption = new DrawingCaption(compositor, overlay);
		engine.Caption += delegate(string json)
		{
			if (ShowsSubtitles || json.Length == 0)
			{
				caption.Show(json);
			}
			Raise(delegate
			{
				this.CaptionChanged?.Invoke(this, json);
			});
		};
		engine.ButtonReady += delegate(int mark, double seconds)
		{
			buttons?.BeatDrawn(mark, seconds);
		};
		liveCaption = new LiveCaption(thread, compositor, overlay);
		if (!engine.Load())
		{
			Log.Error("Drawing", "the drawing engine failed to load: nothing is drawn");
		}
		else
		{
			engine.WarmUp(painter);
		}
		buttons = new ActionButtons(thread, compositor, painter, engine, overlay);
		buttons.Pressed += OnButtonPressed;
		NotchVisibleSurfaces.Changed += delegate
		{
			PostToDrawingThread(delegate
			{
				buttons?.StepAroundNotch(NotchVisibleSurfaces.OpenFrame);
			});
		};
		cardHandles = new CardHandles(compositor, painter, engine, overlay, buttons)
		{
			NothingLeft = delegate
			{
				thread.ScheduleTimer(DrawingThread.Now + 0.55, delegate
				{
					if (!isStreamingNarration)
					{
						Clear(animated: false);
					}
				});
			},
			PlaySound = delegate(string id, float gain)
			{
				if (PlaysSounds)
				{
					SoundPlayer.Shared.Schedule(new List<SoundItem> { SoundItem.Event(id, DrawingThread.Now, gain) }, this);
				}
			},
			CurrentDisplay = () => display,
			After = delegate(double seconds, Action action)
			{
				thread.ScheduleTimer(DrawingThread.Now + seconds, action);
			},
			DownloadsFolder = () => DownloadsFolderOverride ?? LessonVideoRecorder.DownloadsFolder()
		};
		pinnedCards = new PinnedCards(thread, compositor, painter);
		cardHandles.Pinned = pinnedCards;
		GpuPriority.Raise(CanvasDevice.GetSharedDevice());
		Log.Info("Drawing", $"canvas ready in {(DrawingThread.Now - now) * 1000.0:F0} ms (engine {engine.LoadMilliseconds:F0} ms)");
	}

	private void Raise(Action action)
	{
		if ((object)eventQueue == null)
		{
			try
			{
				action();
				return;
			}
			catch (Exception ex)
			{
				Log.Error("Drawing", "event handler failed", ex);
				return;
			}
		}
		eventQueue.TryEnqueue(delegate
		{
			try
			{
				action();
			}
			catch (Exception ex2)
			{
				Log.Error("Drawing", "event handler failed", ex2);
			}
		});
	}

	private T OnDrawingThread<T>(Func<T> function, T fallback)
	{
		try
		{
			if (!thread.IsCurrent)
			{
				initialized.Wait(TimeSpan.FromSeconds(10L));
			}
			return thread.Invoke(() => (T)((engine != null && painter != null) ? ((object)function()) : ((object)fallback)));
		}
		catch (Exception ex)
		{
			Log.Error("Drawing", "drawing call failed", ex);
			return fallback;
		}
	}

	private void PostToDrawingThread(Action action)
	{
		thread.Post(delegate
		{
			if (initialized.IsCompleted && engine != null && painter != null)
			{
				action();
			}
		});
	}

	public void BeginProgram(ScreenCaptureFrame screenshot, string cursorColorHex)
	{
		Task<(byte[] Pixels, int Width, int Height)?> decode = Task.Run(() => ScreenshotSampler.DecodeAsync(screenshot.Jpeg, screenshot.PixelWidth, screenshot.PixelHeight));
		OnDrawingThread(delegate
		{
			clearGeneration++;
			caption?.Hide();
			buttons.RetireLingeringAsks();
			buttons.Clear();
			isDrawingFadedAway = false;
			isNotesOnly = false;
			drawsPicture = false;
			inkedMarkCount = 0;
			cardHandles.Hide();
			painter.EndIsolation(animated: false);
			NativeEngine.ClickyInkHex = (string.IsNullOrWhiteSpace(cursorColorHex) ? "#3380FF" : cursorColorHex.ToUpperInvariant());
			engine.PlaysSounds = PlaysSounds;
			engine.PlaysPenSounds = PlaysPenSounds;
			display = screenshot.Display;
			canvasSize = new Vector2(screenshot.PixelWidth, screenshot.PixelHeight);
			RectD boundsPx = screenshot.Display.BoundsPx;
			overlay.ShowOver(new RectInt32((int)boundsPx.X, (int)boundsPx.Y, (int)boundsPx.Width, (int)boundsPx.Height));
			double num = Math.Max(0.5, screenshot.Display.Scale);
			double pointWidth = boundsPx.Width / num;
			Drawing3DAdapter.WarmUp();
			engine.Screenshot.Clear();
			screenshotDecode = decode;
			screenshotJpeg = screenshot.Jpeg;
			painter.ClearAll();
			painter.Configure(canvasSize, pointWidth, num);
			painter.StageVisual.Offset = Vector3.Zero;
			bool flag = engine.BeginStream(screenshot.PixelWidth, screenshot.PixelHeight, pointWidth, ShowsSubtitles);
			lastReleasedLine = -1;
			deliveredActionMarks.Clear();
			prefetchedPhotos.Clear();
			drawnMarkCount = 0;
			drawingDisplayBounds = screenshot.Display.BoundsPx;
			knownActions = new List<(string, int, int, JsonElement)>();
			isShowing = flag;
			defersScreenChangesUntilFirstLine = true;
			StartStaleWatch(screenshot.Display);
			Log.Info("Drawing", $"beginStream opened={flag} canvas={screenshot.PixelWidth}x{screenshot.PixelHeight} display={boundsPx} scale={num}");
			return flag;
		}, fallback: false);
	}

	private void ApplyScreenshotIfReady(bool wait)
	{
		Task<(byte[], int, int)?> task = screenshotDecode;
		if (task == null)
		{
			return;
		}
		if (!task.IsCompleted && wait)
		{
			task.Wait(TimeSpan.FromMilliseconds(300L, 0L));
		}
		if (!task.IsCompleted)
		{
			return;
		}
		screenshotDecode = null;
		try
		{
			engine.Screenshot.Set(task.Result);
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "screenshot did not decode: " + ex.Message);
		}
	}

	public DrawingFeedResult Feed(string codeSoFar, bool done)
	{
		string code = WithoutLeadingProse(WithoutWebSearchCitations(codeSoFar ?? ""));
		return OnDrawingThread(delegate
		{
			ApplyScreenshotIfReady(wait: true);
			engine.SetGlobalFlag("__hideLoneSources", HidesLoneSources);
			string text = engine.Feed(code, done);
			if (text != null)
			{
				DrawingFeedResult result = ParseFeed(text, done);
				DeliverActions();
				return result;
			}
			return new DrawingFeedResult(Array.Empty<string>(), Array.Empty<DrawingAction>(), 0, EmptyRaw);
		}, new DrawingFeedResult(Array.Empty<string>(), Array.Empty<DrawingAction>(), 0, EmptyRaw));
	}

	private DrawingFeedResult ParseFeed(string json, bool done)
	{
		using JsonDocument jsonDocument = JsonDocument.Parse(json);
		JsonElement raw = jsonDocument.RootElement.Clone();
		List<string> list = new List<string>();
		if (raw.TryGetProperty("lines", out var value) && value.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item3 in value.EnumerateArray())
			{
				list.Add(item3.GetString() ?? "");
			}
		}
		List<DrawingAction> list2 = new List<DrawingAction>();
		List<(string, int, int, JsonElement)> list3 = new List<(string, int, int, JsonElement)>();
		if (raw.TryGetProperty("actions", out var value2) && value2.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item4 in value2.EnumerateArray())
			{
				JsonElement value3;
				string text = (item4.TryGetProperty("role", out value3) ? (value3.GetString() ?? "") : "");
				list2.Add(new DrawingAction(text, item4));
				JsonElement value4;
				int item = ((item4.TryGetProperty("line", out value4) && value4.ValueKind == JsonValueKind.Number) ? value4.GetInt32() : 0);
				JsonElement value5;
				int item2 = ((item4.TryGetProperty("i", out value5) && value5.ValueKind == JsonValueKind.Number) ? value5.GetInt32() : (-1));
				list3.Add((text, item, item2, item4));
				if (text == "photo" && item4.TryGetProperty("text", out var value6))
				{
					string @string = value6.GetString();
					if (@string != null && @string.Length > 0 && prefetchedPhotos.Add(@string))
					{
						photoLoader.ImageAsync(@string, CancellationToken.None, ReplayPhotosFolder);
					}
				}
			}
		}
		knownActions = list3;
		JsonElement value7;
		int num = ((raw.TryGetProperty("marks", out value7) && value7.ValueKind == JsonValueKind.Number) ? value7.GetInt32() : 0);
		drawnMarkCount = Math.Max(drawnMarkCount, num);
		inkedMarkCount = ((raw.TryGetProperty("inked", out var value8) && value8.ValueKind == JsonValueKind.Number) ? value8.GetInt32() : num);
		drawsPicture = raw.TryGetProperty("picture", out var value9) && value9.ValueKind == JsonValueKind.True;
		if (raw.TryGetProperty("pointingLines", out var value10) && value10.ValueKind == JsonValueKind.Array && value10.GetArrayLength() > 0)
		{
			latestAnswerPointsAtScreen = true;
		}
		if (done && raw.TryGetProperty("error", out var value11) && value11.ValueKind == JsonValueKind.String)
		{
			Log.Warn("Drawing", "engine error in program: " + value11.GetString());
		}
		return new DrawingFeedResult(list, list2, num, raw);
	}

	private void DeliverActions()
	{
		foreach (var knownAction in knownActions)
		{
			var (role, num, num2, action) = knownAction;
			if (num > lastReleasedLine || !deliveredActionMarks.Add((num2 < 0) ? HashCode.Combine(role, num, action.GetRawText()) : num2))
			{
				continue;
			}
			switch (role)
			{
			case "button":
			case "link":
				buttons.Queue(role, num2, action);
				continue;
			case "point":
			case "hover":
			case "photo":
			case "target":
			case "open":
			case "type":
			case "copy":
			case "handoff":
			case "tourjob":
			case "tourtuck":
			case "tourasks":
			case "tourfade":
			case "tournotch":
				continue;
			}
			JsonElement copy = action.Clone();
			Raise(delegate
			{
				this.ActionRequested?.Invoke(this, new DrawingAction(role, copy));
			});
		}
	}

	private void OnButtonPressed(string kind, JsonElement payload)
	{
		JsonElement value;
		string text = (payload.TryGetProperty("kind", out value) ? (value.GetString() ?? "") : "");
		JsonElement value2;
		string text2 = ((payload.TryGetProperty("value", out value2) && value2.ValueKind == JsonValueKind.String) ? value2.GetString() : null);
		if (kind == "open" || text == "open")
		{
			JsonElement value3;
			string text3 = ((!(kind == "open")) ? text2 : (payload.TryGetProperty("url", out value3) ? value3.GetString() : null));
			if (text3 != null && Uri.TryCreate(text3, UriKind.Absolute, out Uri result) && (result.Scheme == "https" || result.Scheme == "http"))
			{
				ExternalActions.OpenPage(result);
			}
		}
		else if (text == "copy")
		{
			if (text2 != null)
			{
				ExternalActions.CopyText(text2);
			}
		}
		else
		{
			Raise(delegate
			{
				this.ActionRequested?.Invoke(this, new DrawingAction("button", payload));
			});
		}
	}

	public void ReleaseLine(int index)
	{
		Interlocked.Exchange(ref latestLineReleasedTicks, DateTime.UtcNow.Ticks);
		ReleaseLineOnDrawingThread(index);
	}

	private void ReleaseLineOnDrawingThread(int index)
	{
		PostToDrawingThread(delegate
		{
			bool value = engine.Line(index);
			lastReleasedLine = Math.Max(lastReleasedLine, index);
			defersScreenChangesUntilFirstLine = false;
			DeliverActions();
			Log.Info("Drawing", $"line {index} released accepted={value}");
		});
	}

	public void SetLineLength(int index, double seconds)
	{
		if (seconds > 0.0)
		{
			PostToDrawingThread(delegate
			{
				engine.LineLength(index, seconds);
			});
		}
	}

	public void Finish()
	{
		PostToDrawingThread(delegate
		{
			engine.Finish();
			lastReleasedLine = int.MaxValue;
			DeliverActions();
			int generation = clearGeneration;
			thread.ScheduleTimer(DrawingThread.Now + 1.0, delegate
			{
				if (generation == clearGeneration && !isStreamingNarration && !isDrawingFadedAway)
				{
					cardHandles.Refresh();
				}
			});
			thread.ScheduleTimer(DrawingThread.Now + 1.2, delegate
			{
				if (generation == clearGeneration && isShowing)
				{
					LogTimeline();
				}
			});
		});
	}

	private void LogTimeline()
	{
		string text = DrawingTimelineReport.Format(engine.Timeline());
		if (text != null)
		{
			Log.Info("Drawing", text);
		}
	}

	public void Clear(bool animated = true)
	{
		PostToDrawingThread(delegate
		{
			int generation = ++clearGeneration;
			isDrawingFadedAway = false;
			if (isShowing || overlay.IsShown)
			{
				Raise(delegate
				{
					this.Cleared?.Invoke(this, EventArgs.Empty);
				});
			}
			caption?.Hide();
			liveCaption?.Hide();
			StopStaleWatch();
			buttons.Clear();
			cardHandles.Dispose();
			painter.EndIsolation(animated: false);
			if (!isShowing && !overlay.IsShown)
			{
				engine.Clear();
			}
			else
			{
				isShowing = false;
				if (animated)
				{
					painter.FadeOut();
					engine.StopSounds();
					thread.ScheduleTimer(DrawingThread.Now + 0.48, delegate
					{
						if (generation == clearGeneration)
						{
							engine.Clear();
							overlay.Hide();
						}
					});
				}
				else
				{
					engine.Clear();
					overlay.Hide();
				}
			}
		});
	}

	private void StartStaleWatch(DisplayInfo watchedDisplay)
	{
		StopStaleWatch();
		Func<DisplayInfo, Action<string>, IDisposable> screenChangeWatchFactory = ScreenChangeWatchFactory;
		if (screenChangeWatchFactory == null)
		{
			return;
		}
		int generation = staleWatchGeneration;
		try
		{
			staleWatch = screenChangeWatchFactory(watchedDisplay, delegate(string reason)
			{
				PostToDrawingThread(delegate
				{
					ScreenMayHaveChanged(reason, generation);
				});
			});
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "the screen-change watch did not start: " + ex.Message);
		}
	}

	private void StopStaleWatch()
	{
		staleWatchGeneration++;
		staleWatch?.Dispose();
		staleWatch = null;
	}

	private void ScreenMayHaveChanged(string reason, int generation)
	{
		if (generation != staleWatchGeneration || !isShowing)
		{
			return;
		}
		bool flag = defersScreenChangesUntilFirstLine || isStreamingNarration;
		bool flag2;
		switch (reason)
		{
		case "click":
		case "app switch":
		case "space change":
			flag2 = true;
			break;
		default:
			flag2 = false;
			break;
		}
		bool flag3 = flag2;
		flag2 = !flag;
		if (flag2)
		{
			string text = reason;
			bool flag4 = ((text == "click" || text == "scroll") ? true : false);
			flag2 = flag4;
		}
		if (flag2)
		{
			Raise(delegate
			{
				this.UserActedOnScreen?.Invoke(this, reason);
			});
		}
		if (!flag && !flag3 && !isNotesOnly)
		{
			if (drawsPicture)
			{
				(int, int)? tuple = engine.DropPointing();
				if (tuple.HasValue && tuple.GetValueOrDefault().Item2 != 0)
				{
					isNotesOnly = true;
					Log.Info("Drawing", "the screen changed under the drawing (" + reason + "): its pointing marks left, the picture stays");
					goto IL_015e;
				}
			}
			Log.Info("Drawing", "the screen changed under the drawing (" + reason + "): clearing it");
			LogTimeline();
			Clear(animated: false);
			return;
		}
		goto IL_015e;
		IL_015e:
		if (flag || !flag3)
		{
			Log.Info("Drawing", $"the screen may have changed ({reason}): {(flag ? "the drawing is still being told" : "not a move away")}, it stays");
			DisplayInfo displayInfo = display;
			if ((object)displayInfo != null)
			{
				StartStaleWatch(displayInfo);
			}
		}
		else
		{
			Log.Info("Drawing", "the screen changed under the drawing (" + reason + "): clearing it");
			LogTimeline();
			Clear(animated: false);
		}
	}

	public DrawingContinuation? ContinueStream()
	{
		return OnDrawingThread(delegate
		{
			if (!isShowing || drawnMarkCount == 0)
			{
				return (DrawingContinuation?)null;
			}
			(int, int, string)? tuple = engine.ContinueStream(ShowsSubtitles);
			if (!tuple.HasValue)
			{
				Log.Info("Drawing", "continueStream failed: the next drawing starts fresh");
				return (DrawingContinuation?)null;
			}
			lastReleasedLine = -1;
			deliveredActionMarks.Clear();
			knownActions = new List<(string, int, int, JsonElement)>();
			latestAnswerPointsAtScreen = false;
			isNotesOnly = false;
			buttons.RetireAsks();
			cardHandles.Hide();
			Log.Info("Drawing", $"continueStream: building on {tuple.Value.Item2} marks, first new line {tuple.Value.Item1}");
			return new DrawingContinuation(tuple.Value.Item1, tuple.Value.Item2, tuple.Value.Item3);
		}, null);
	}

	public void PlayClearSoundIfShowingDrawing()
	{
		if (IsShowingDrawing && PlaysSounds)
		{
			Log.Info("Drawing", "the user cleared the drawing: page-clear");
			PostToDrawingThread(delegate
			{
				SoundPlayer.Shared.Schedule(new List<SoundItem> { SoundItem.Event("page-clear", DrawingThread.Now, 0.55f) }, this);
			});
		}
	}

	public void ShowLiveCaption(string line, double revealSeconds, double holdSeconds)
	{
		PostToDrawingThread(delegate
		{
			if (overlay.IsShown)
			{
				liveCaption.Show(line, revealSeconds, holdSeconds);
			}
		});
	}

	public void FadeDrawingKeepingAskButtons()
	{
		PostToDrawingThread(delegate
		{
			int num = buttons.KeepAsksAfterTheDrawing();
			Clear();
			if (num > 0)
			{
				Log.Info("Drawing", $"drawn tour: {num} next question(s) stay after the drawing");
			}
		});
	}

	public void SetDrawingOpacity(double opacity, double durationSeconds)
	{
		PostToDrawingThread(delegate
		{
			if (overlay.IsShown)
			{
				painter.FadeTo((float)opacity, PinnedCards.AnimationsAreReduced ? 0.0 : durationSeconds);
				isDrawingFadedAway = opacity == 0.0;
				if (opacity == 0.0)
				{
					Raise(delegate
					{
						this.PenHandedBack?.Invoke(this, EventArgs.Empty);
					});
					caption?.Hide();
					painter.Figures?.StopTakingTheMouse();
					cardHandles.Hide();
				}
			}
		});
	}

	public void SetBelowNotch(bool isBelow)
	{
		nint notch = ((!isBelow) ? IntPtr.Zero : (NotchWindowProvider?.Invoke() ?? IntPtr.Zero));
		PostToDrawingThread(delegate
		{
			if (overlay.IsShown)
			{
				overlay.SetBelow(notch, isBelow);
			}
		});
	}

	public void PlaySound(string soundId, float gain)
	{
		PostToDrawingThread(delegate
		{
			SoundPlayer.Shared.Schedule(new List<SoundItem> { SoundItem.Event(soundId, DrawingThread.Now, gain) }, this);
		});
	}

	public (double Width, double Height) StandaloneButtonSize(string title, string kind)
	{
		return OnDrawingThread(delegate
		{
			Vector2 vector = buttons.StandaloneSize(title, kind);
			return ((double, double))(vector.X, vector.Y);
		}, (0.0, 0.0));
	}

	public IDisposable ShowStandaloneButton(string title, string kind, RectInt32 rectPx, double scale, Action pressed)
	{
		StandaloneButtonHandle handle = new StandaloneButtonHandle(this);
		PostToDrawingThread(delegate
		{
			if (!handle.IsDisposed)
			{
				handle.Window = buttons.ShowStandalone(title, kind, rectPx, scale, delegate
				{
					Raise(pressed);
				});
			}
		});
		return handle;
	}

	public void FlyIntoNotch(byte[] picture, RectInt32 sourcePx, DisplayInfo display, Action completion)
	{
		PostToDrawingThread(delegate
		{
			try
			{
				using IRandomAccessStream stream = new MemoryStream(picture).AsRandomAccessStream();
				CanvasBitmap result = CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), stream).AsTask().GetAwaiter()
					.GetResult();
				pinnedCards.FlyIntoNotch(result, sourcePx, display, delegate
				{
					Raise(completion);
				});
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "fly into the notch failed: " + ex.Message);
				Raise(completion);
			}
		});
	}

	public void PlayOneShot(string soundId, float gain)
	{
		if (PlaysSounds)
		{
			PostToDrawingThread(delegate
			{
				SoundPlayer.Shared.Schedule(new List<SoundItem> { SoundItem.Event(soundId, DrawingThread.Now, gain) }, this);
			});
		}
	}

	public bool CanContinueDrawing(ScreenCaptureFrame capture)
	{
		if (IsShowingDrawing && capture.Display.BoundsPx == drawingDisplayBounds)
		{
			return canvasSize == new Vector2(capture.PixelWidth, capture.PixelHeight);
		}
		return false;
	}

	public string? DrawingStateForModel()
	{
		return DrawingState();
	}

	public void ForgetWhetherLatestAnswerPointsAtScreen()
	{
		latestAnswerPointsAtScreen = false;
	}

	public string? DrawingState()
	{
		return OnDrawingThread(() => engine.DrawingState(), null);
	}

	public string? Timeline()
	{
		return OnDrawingThread(() => engine.Timeline(), null);
	}

	public (int Kept, int Dropped)? KeepNotesOnly()
	{
		return OnDrawingThread(() => engine.KeepNotesOnly(), null);
	}

	public (int Dropped, int Left)? DropPointing()
	{
		return OnDrawingThread(() => engine.DropPointing(), null);
	}

	public byte[]? RenderDrawingPng(double? atTime = null, double pixelsPerUnit = 1.0, bool overScreenshot = true, bool includeButtons = false)
	{
		return OnDrawingThread(() => RenderDrawing(atTime, pixelsPerUnit, overScreenshot ? screenshotJpeg : null, includeButtons, asJpeg: false), null);
	}

	public ScreenCaptureFrame? CaptureDrawingForModel(ScreenCaptureFrame capture)
	{
		return OnDrawingThread(delegate
		{
			if (!isShowing || canvasSize != new Vector2(capture.PixelWidth, capture.PixelHeight))
			{
				return (ScreenCaptureFrame)null;
			}
			byte[] array = RenderDrawing(null, 1.0, capture.Jpeg, includeButtons: true, asJpeg: true);
			return (array != null) ? capture with
			{
				Jpeg = array
			} : null;
		}, null);
	}

	private byte[]? RenderDrawing(double? atTime, double pixelsPerUnit, byte[]? backgroundJpeg, bool includeButtons, bool asJpeg)
	{
		if (canvasSize.X <= 0f)
		{
			return null;
		}
		CanvasBitmap canvasBitmap = null;
		try
		{
			if (backgroundJpeg != null)
			{
				using IRandomAccessStream stream = new MemoryStream(backgroundJpeg).AsRandomAccessStream();
				canvasBitmap = CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), stream).AsTask().GetAwaiter()
					.GetResult();
			}
			using CanvasRenderTarget canvasRenderTarget = painter.RenderSnapshot(atTime ?? DrawingThread.Now, (float)pixelsPerUnit, canvasBitmap);
			if (includeButtons && overlay.BoundsPx.Width > 0)
			{
				using CanvasDrawingSession session = canvasRenderTarget.CreateDrawingSession();
				double num = pixelsPerUnit * (double)canvasSize.X / (double)overlay.BoundsPx.Width;
				foreach (var window in buttons.Windows)
				{
					RectInt32 item = window.Rect;
					ContainerVisual item2 = window.Root;
					double item3 = window.Scale;
					Matrix3x2 transform = Matrix3x2.CreateScale((float)(item3 * num)) * Matrix3x2.CreateTranslation((float)((double)(item.X - overlay.BoundsPx.X) * num), (float)((double)(item.Y - overlay.BoundsPx.Y) * num));
					painter.DrawForeignVisual(session, item2, transform, new Vector2((float)((double)item.Width / item3), (float)((double)item.Height / item3)), atTime ?? DrawingThread.Now);
				}
			}
			using MemoryStream memoryStream = new MemoryStream();
			if (asJpeg)
			{
				canvasRenderTarget.SaveAsync(memoryStream.AsRandomAccessStream(), CanvasBitmapFileFormat.Jpeg, 0.85f).AsTask().GetAwaiter()
					.GetResult();
			}
			else
			{
				canvasRenderTarget.SaveAsync(memoryStream.AsRandomAccessStream(), CanvasBitmapFileFormat.Png).AsTask().GetAwaiter()
					.GetResult();
			}
			return memoryStream.ToArray();
		}
		catch (Exception value)
		{
			Log.Warn("Drawing", $"drawing picture failed: {value}");
			return null;
		}
		finally
		{
			canvasBitmap?.Dispose();
		}
	}

	private void OnPenStrokes(IReadOnlyList<PenStroke> strokes)
	{
		if (this.PenStrokes == null)
		{
			return;
		}
		DisplayInfo displayInfo = display;
		if ((object)displayInfo == null || canvasSize.X <= 0f)
		{
			return;
		}
		double num = displayInfo.BoundsPx.Width / (double)canvasSize.X;
		List<PenStrokePath> converted = new List<PenStrokePath>(strokes.Count);
		foreach (PenStroke stroke in strokes)
		{
			PointD[] array = new PointD[stroke.Points.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new PointD(displayInfo.BoundsPx.X + (double)stroke.Points[i].X * num, displayInfo.BoundsPx.Y + (double)stroke.Points[i].Y * num);
			}
			converted.Add(new PenStrokePath(array, stroke.BeginTime, stroke.Duration, stroke.Timing.X, stroke.Timing.Y, stroke.Timing.Z, stroke.Timing.W));
		}
		Raise(delegate
		{
			this.PenStrokes?.Invoke(this, converted);
		});
	}

	internal static string WithoutLeadingProse(string code)
	{
		string[] array = code.Split('\n');
		int num = 0;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text = array2[i].Trim();
			bool flag = text.Length > 0 && !text.StartsWith("//") && !text.StartsWith("/*") && text.IndexOfAny("(){}[]=;`".ToCharArray()) < 0;
			if (text.Length > 0 && !flag)
			{
				break;
			}
			num++;
		}
		if (num == 0)
		{
			return code;
		}
		if (num >= array.Length)
		{
			return "";
		}
		return string.Join('\n', array, num, array.Length - num);
	}

	internal static string WithoutWebSearchCitations(string code)
	{
		if (!code.Contains("](http", StringComparison.Ordinal))
		{
			return code;
		}
		List<(int Start, int Length)> stringRanges = StringLiteralRanges(code);
		List<Match> list = (from match in WebSearchCitationPattern.Matches(code)
			where !stringRanges.Any(((int Start, int Length) range) => range.Start < match.Index + match.Length && match.Index < range.Start + range.Length)
			select match).ToList();
		if (list.Count == 0)
		{
			return code;
		}
		StringBuilder stringBuilder = new StringBuilder(code);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			stringBuilder.Remove(list[num].Index, list[num].Length);
		}
		return stringBuilder.ToString();
	}

	internal static List<(int Start, int Length)> StringLiteralRanges(string code)
	{
		List<(int, int)> list = new List<(int, int)>();
		int i = 0;
		while (i < code.Length)
		{
			char c = code[i];
			if (c == '/' && i + 1 < code.Length && code[i + 1] == '/' && (i == 0 || code[i - 1] != ':'))
			{
				for (; i < code.Length && code[i] != '\n'; i++)
				{
				}
			}
			else if (c == '/' && i + 1 < code.Length && code[i + 1] == '*')
			{
				for (i += 2; i + 1 < code.Length && (code[i] != '*' || code[i + 1] != '/'); i++)
				{
				}
				i += 2;
			}
			else if ((c == '"' || c == '\'' || c == '`') ? true : false)
			{
				int num = i++;
				for (; i < code.Length && code[i] != c && (c == '`' || code[i] != '\n'); i += ((code[i] != '\\') ? 1 : 2))
				{
				}
				int num2 = Math.Min(i + 1, code.Length);
				list.Add((num, num2 - num));
				i = num2;
			}
			else
			{
				i++;
			}
		}
		return list;
	}

	internal string? EvaluateForLab(string script)
	{
		return OnDrawingThread(() => engine.Evaluate(script), null);
	}

	internal void SetRemovesFinishedPenMasks(bool removes)
	{
		OnDrawingThread(delegate
		{
			painter.RemovesFinishedPenMasks = removes;
			return true;
		}, fallback: false);
	}

	internal string DumpPainterTree()
	{
		return OnDrawingThread(() => painter.DumpTree(DrawingThread.Now), "");
	}

	internal void SimulateCard(int index, string what, float dx = 0f, float dy = 0f)
	{
		OnDrawingThread(delegate
		{
			cardHandles.Simulate(index, what, new Vector2(dx, dy));
			return true;
		}, fallback: false);
	}

	internal List<RectInt32> ShowCardButtonsForHarness(int? hovered)
	{
		return OnDrawingThread(() => cardHandles.ShowButtonsForHarness(hovered), new List<RectInt32>());
	}

	internal void SimulatePinned(string what)
	{
		OnDrawingThread(delegate
		{
			pinnedCards.IgnoresPointerForHarness = true;
			pinnedCards.Simulate(what);
			return true;
		}, fallback: false);
	}

	internal void RunOnDrawingThread(Action action)
	{
		OnDrawingThread(delegate
		{
			action();
			return true;
		}, fallback: false);
	}

	public void Dispose()
	{
		try
		{
			thread.Invoke(delegate
			{
				StopStaleWatch();
				pinnedCards?.Dispose();
				buttons?.Dispose();
				cardHandles?.Dispose();
				engine?.Dispose();
				overlay?.Dispose();
			});
		}
		catch
		{
		}
		thread.Dispose();
	}
}
