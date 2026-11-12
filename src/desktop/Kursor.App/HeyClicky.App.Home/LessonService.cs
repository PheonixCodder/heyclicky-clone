using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.OneBrain;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Tools;
using HeyClicky.Core.Voice;
using HeyClicky.Drawing;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.App.Home;

public sealed class LessonService
{
	public sealed record Recent(DrawingRecord Record, byte[]? Poster)
	{
		public StoredLesson? Saved { get; set; }

		public string Handle { get; init; } = "";

		public IReadOnlyDictionary<string, byte[]> Photos { get; init; } = new Dictionary<string, byte[]>();
	}

	private readonly RealStack real;

	private readonly DispatcherQueue dq;

	private readonly Func<string> cursorColor;

	private readonly List<Recent> recent = new List<Recent>();

	private readonly bool excludeFromCapture;

	private DrawingSaveOffer? offer;

	private LessonPrinter? printer;

	private OverlayWindow? whiteBoard;

	private CancellationTokenSource? replay;

	private volatile bool replaying;

	private readonly object recentGate = new object();

	private LessonVoice? replayVoice;

	public bool IsReplaying
	{
		get
		{
			if (!replaying)
			{
				return ClickyDrawingWindowViewer.Shared.IsOpen;
			}
			return true;
		}
	}

	public LessonStore Store { get; } = new LessonStore();

	public IReadOnlyList<Recent> RecentDrawings => recent;

	public Func<(int X, int Y)>? NotchPoint { get; set; }

	public bool SavesDrawings => real.Core.Settings.Read((AppSettings s) => s.SaveDrawingsEffective);

	public string CursorColorHex => cursorColor();

	public bool ExcludesFromCapture => excludeFromCapture;

	public event Action? Changed;

	public LessonService(RealStack real, DispatcherQueue dq, bool excludeFromCapture, Func<string> cursorColor)
	{
		LessonService lessonService = this;
		this.real = real;
		this.dq = dq;
		this.excludeFromCapture = excludeFromCapture;
		this.cursorColor = cursorColor;
		real.Companion.DrawingTold += delegate(object? _, DrawingRecord record)
		{
			_ = lessonService.OnToldAsync(record);
		};
		real.Companion.DrawingLeftUp += delegate(object? _, ScreenCaptureFrame frame)
		{
			dq.TryEnqueue(delegate
			{
				if (real.Canvas.IsShowingDrawing && !lessonService.replaying)
				{
					lessonService.ShowOffer(frame.Display, null);
				}
			});
		};
		real.Canvas.Cleared += delegate
		{
			dq.TryEnqueue(delegate
			{
				lessonService.offer?.Hide();
			});
		};
		Store.Changed += delegate
		{
			dq.TryEnqueue(delegate
			{
				lessonService.Changed?.Invoke();
			});
		};
		bool savesDrawings = SavesDrawings;
		real.Core.Settings.Changed += delegate
		{
			if (lessonService.SavesDrawings != savesDrawings)
			{
				savesDrawings = lessonService.SavesDrawings;
				Log.Info("lessons", "save drawings " + (savesDrawings ? "on" : "off"));
				dq.TryEnqueue(delegate
				{
					lessonService.Changed?.Invoke();
				});
			}
		};
		AgentToolHooks.SaveDrawing = SaveFromToolAsync;
	}

	public LessonPlaybackStage NewPlaybackStage(StoredLesson lesson)
	{
		return new LessonPlaybackStage((string query, CancellationToken token) => real.Canvas.PhotoAsync(query, token, lesson.PhotosFolder), real.Core.Settings.Read((AppSettings s) => s.DrawingSoundsEnabled));
	}

	public void PlayInWindow(StoredLesson lesson)
	{
		ClickyDrawingWindowViewer.Shared.Present(lesson, this);
	}

	private void ShowOffer(DisplayInfo display, Recent? entry)
	{
		if (!DrawnOnboardingTour.IsRunning && !real.Canvas.IsDrawingFadedAway)
		{
			if (offer == null)
			{
				offer = new DrawingSaveOffer(excludeFromCapture);
			}
			offer.Show(display, ((object)entry == null) ? null : ((Action)delegate
			{
				SaveAsync(entry);
			}), delegate
			{
				Log.Info("lessons", "drawing cleared with the Clear button");
				real.Canvas.PlayClearSoundIfShowingDrawing();
				real.Canvas.Clear();
			});
		}
	}

	private async Task OnToldAsync(DrawingRecord record)
	{
		byte[] poster = await Task.Run(delegate
		{
			try
			{
				return real.Canvas.RenderDrawingPng(null);
			}
			catch
			{
				return (byte[])null;
			}
		});
		string handle;
		lock (recentGate)
		{
			do
			{
				handle = Random.Shared.Next(65536).ToString("x4");
			}
			while (recent.Any((Recent r) => r.Handle == handle));
		}
		Recent entry = new Recent(record, poster)
		{
			Handle = handle,
			Photos = real.Canvas.KnownPhotos(record.Program)
		};
		if (record.KeepsInChat)
		{
			lock (recentGate)
			{
				recent.Add(entry);
				if (recent.Count > 5)
				{
					recent.RemoveAt(0);
				}
			}
			StoredLesson stored = await Store.SaveAsync(record, poster, entry.Photos);
			if (stored == null)
			{
				return;
			}
			entry.Saved = stored;
			dq.TryEnqueue(delegate
			{
				this.Changed?.Invoke();
				NotchChat.HandOffs?.NoteDrawingKept(stored.Data.Id, stored.Data.Title);
				if (real.Canvas.IsShowing)
				{
					ShowOffer(record.Screen.Display, null);
				}
			});
			return;
		}
		real.Core.BrainSession.NoteForNextTurn($"[clicky drawing] \"{record.Title}\" can be saved to the user's Drawings (drawing id: {handle}) if they ask.");
		dq.TryEnqueue(delegate
		{
			lock (recentGate)
			{
				recent.Add(entry);
				if (recent.Count > 5)
				{
					recent.RemoveAt(0);
				}
			}
			this.Changed?.Invoke();
			if (real.Canvas.IsShowing)
			{
				ShowOffer(record.Screen.Display, entry);
			}
		});
	}

	private async Task<string> SaveFromToolAsync(string? handle)
	{
		string wanted = handle?.Trim().ToLowerInvariant() ?? "";
		int attempt = 0;
		Recent entry;
		while (true)
		{
			lock (recentGate)
			{
				entry = ((wanted.Length == 0) ? recent.LastOrDefault() : recent.LastOrDefault((Recent r) => r.Handle == wanted));
			}
			if ((object)entry != null || attempt >= 10)
			{
				break;
			}
			await Task.Delay(100);
			attempt++;
		}
		if ((object)entry == null)
		{
			Log.Info("lessons", "save_drawing: no recent drawing" + ((wanted.Length > 0) ? (" with id " + wanted) : ""));
			return Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "not_found",
				["message"] = "There is no recent drawing to save: only explanations drawn in this session can be saved."
			});
		}
		StoredLesson saved = entry.Saved;
		if (saved != null)
		{
			return Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "already_saved",
				["title"] = saved.Data.Title,
				["message"] = "That drawing is already in the user's Drawings."
			});
		}
		TaskCompletionSource<StoredLesson?> completion = new TaskCompletionSource<StoredLesson>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!dq.TryEnqueue(async delegate
		{
			try
			{
				TaskCompletionSource<StoredLesson?> taskCompletionSource = completion;
				taskCompletionSource.TrySetResult(await SaveAsync(entry));
			}
			catch (Exception exception)
			{
				completion.TrySetException(exception);
			}
		}))
		{
			return Json(new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "failed",
				["message"] = "The drawing couldn't be saved."
			});
		}
		StoredLesson storedLesson = await completion.Task;
		Log.Info("lessons", $"save_drawing: {((storedLesson == null) ? "failed" : ("saved \"" + storedLesson.Data.Title + "\""))} (id {entry.Handle})");
		return (storedLesson == null) ? Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "failed",
			["message"] = "The drawing couldn't be saved."
		}) : Json(new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "saved",
			["title"] = storedLesson.Data.Title,
			["message"] = "Saved to the user's Drawings in Home."
		});
		static string Json(JsonObject o)
		{
			return o.ToJsonString();
		}
	}

	public async Task<StoredLesson?> SaveAsync(Recent entry)
	{
		StoredLesson saved = entry.Saved;
		if (saved != null)
		{
			return saved;
		}
		offer?.Hide();
		StoredLesson stored = await Store.SaveAsync(entry.Record, entry.Poster, entry.Photos);
		if (stored == null)
		{
			return null;
		}
		entry.Saved = stored;
		dq.TryEnqueue(delegate
		{
			this.Changed?.Invoke();
			if (real.Canvas.IsShowing)
			{
				real.Canvas.Clear();
			}
			if (printer == null)
			{
				printer = new LessonPrinter(excludeFromCapture);
			}
			printer.Print(entry.Poster, stored.Data.Title, entry.Record.Screen.Display, NotchPoint?.Invoke());
		});
		return stored;
	}

	public async Task PlayAsync(StoredLesson lesson, bool withVoice = true, bool onWhiteBoard = false)
	{
		replay?.Cancel();
		CancellationTokenSource cts = (replay = new CancellationTokenSource());
		StoredLesson.Meta d = lesson.Data;
		string program;
		byte[] jpeg;
		try
		{
			program = await File.ReadAllTextAsync(lesson.ProgramPath);
			jpeg = await File.ReadAllBytesAsync(lesson.ScreenPath);
		}
		catch (Exception ex)
		{
			Log.Warn("lessons", "replay read: " + ex.Message);
			return;
		}
		(byte[], int)? tuple = ((!withVoice) ? (((byte[], int)?)null) : (await Task.Run((Func<(byte[], int)?>)lesson.ReadNarration)));
		(byte[] Pcm16Mono, int SampleRate)? narration = tuple;
		if (cts.IsCancellationRequested)
		{
			return;
		}
		IReadOnlyList<DisplayInfo> displays = real.Capture.GetDisplays();
		DisplayInfo displayInfo = displays.FirstOrDefault((DisplayInfo x) => x.DeviceName == d.DisplayDevice) ?? displays.FirstOrDefault((DisplayInfo x) => x.IsPrimary) ?? displays.FirstOrDefault();
		if ((object)displayInfo == null)
		{
			return;
		}
		byte[] jpeg2 = (onWhiteBoard ? (WhiteJpeg(d.CanvasWidth, d.CanvasHeight) ?? jpeg) : jpeg);
		ScreenCaptureFrame frame = new ScreenCaptureFrame(displayInfo, jpeg2, d.CanvasWidth, d.CanvasHeight, ContainsCursor: false, new PointD(0.0, 0.0));
		Log.Info("lessons", $"replaying \"{d.Title}\" ({d.Lines.Count} lines, {d.Duration:0.0} s{(narration.HasValue ? ", with its recorded voice" : ", no voice")}{(onWhiteBoard ? ", on a white board" : "")})");
		real.Canvas.Clear(animated: false);
		ShowBackdrop(displayInfo, onWhiteBoard ? null : lesson.ScreenPath);
		bool subtitlesSetting = real.Core.Settings.Read((AppSettings x) => x.DrawingSubtitlesEffective);
		replaying = true;
		try
		{
			await ReplayAsync();
		}
		finally
		{
			if (replay == cts)
			{
				replaying = false;
				real.Canvas.ReplayPhotosFolder = null;
			}
		}
		async Task ReplayAsync()
		{
			real.Canvas.ShowsSubtitles = !narration.HasValue || subtitlesSetting;
			real.Canvas.ReplayPhotosFolder = lesson.PhotosFolder;
			real.Canvas.BeginProgram(frame, d.CursorColorHex);
			real.Canvas.IsStreamingNarration = true;
			real.Canvas.Feed(program, done: true);
			try
			{
				await Task.Delay(300, cts.Token);
			}
			catch
			{
				return;
			}
			EndReplayWhenCleared(cts);
			double shift = lesson.ReplayShift;
			DateTimeOffset started = DateTimeOffset.Now;
			if (narration.HasValue)
			{
				(byte[], int) valueOrDefault = narration.GetValueOrDefault();
				if (replayVoice == null)
				{
					replayVoice = new LessonVoice();
				}
				replayVoice.Play(valueOrDefault, shift);
			}
			for (int i = 0; i < d.Lines.Count; i++)
			{
				if (cts.IsCancellationRequested)
				{
					break;
				}
				StoredLesson.LineData line = d.Lines[i];
				TimeSpan timeSpan = TimeSpan.FromSeconds(line.Start - shift) - (DateTimeOffset.Now - started);
				if (timeSpan > TimeSpan.Zero)
				{
					try
					{
						await Task.Delay(timeSpan, cts.Token);
					}
					catch
					{
						break;
					}
				}
				if (cts.IsCancellationRequested)
				{
					break;
				}
				double? length = line.Length;
				if (length.HasValue)
				{
					double valueOrDefault2 = length.GetValueOrDefault();
					real.Canvas.SetLineLength(i, valueOrDefault2);
				}
				real.Canvas.ReleaseLine(i);
				Log.Info("lessons", $"replay line {i} at {(DateTimeOffset.Now - started).TotalSeconds:0.00} s{(narration.HasValue ? $", voice at {replayVoice.Position - shift:0.00} s" : "")}");
			}
			if (cts.IsCancellationRequested)
			{
				real.Canvas.ShowsSubtitles = subtitlesSetting;
			}
			else
			{
				TimeSpan timeSpan2 = TimeSpan.FromSeconds(d.Duration - shift) - (DateTimeOffset.Now - started);
				if (timeSpan2 > TimeSpan.Zero)
				{
					try
					{
						await Task.Delay(timeSpan2, cts.Token);
					}
					catch
					{
					}
				}
				if (cts.IsCancellationRequested)
				{
					real.Canvas.ShowsSubtitles = subtitlesSetting;
				}
				else
				{
					real.Canvas.Finish();
					real.Canvas.IsStreamingNarration = false;
					real.Canvas.ShowsSubtitles = subtitlesSetting;
					try
					{
						await Task.Delay(3500, cts.Token);
					}
					catch
					{
						return;
					}
					Log.Info("lessons", "replay ended");
					StopReplay();
					real.Canvas.Clear();
				}
			}
		}
	}

	public void StopReplay()
	{
		replaying = false;
		replay?.Cancel();
		replayVoice?.Stop();
		real.Canvas.ReplayPhotosFolder = null;
		whiteBoard?.HideWindow();
	}

	public void StopReplayForEscape()
	{
		if (ClickyDrawingWindowViewer.Shared.IsOpen)
		{
			Log.Info("lessons", "drawing window closed (Escape)");
			ClickyDrawingWindowViewer.Shared.Dismiss(animated: true);
			return;
		}
		Log.Info("lessons", "replay stopped (Escape)");
		StopReplay();
		real.Companion.Cancel();
		real.Canvas.IsStreamingNarration = false;
		real.Canvas.ShowsSubtitles = false;
		real.Canvas.Clear();
	}

	private void ShowBackdrop(DisplayInfo display, string? screenPath)
	{
		if ((object)whiteBoard == null)
		{
			whiteBoard = new OverlayWindow("HeyClicky board", excludeFromCapture);
			whiteBoard.Root.PointerPressed += delegate(object _, PointerRoutedEventArgs e)
			{
				e.Handled = true;
				if (replaying)
				{
					Log.Info("lessons", "replay stopped (a click)");
					StopReplay();
					real.Canvas.IsStreamingNarration = false;
					real.Canvas.Clear();
				}
			};
		}
		whiteBoard.Root.Background = ((screenPath == null) ? ((Brush)Ui.Brush(Colors.White)) : ((Brush)new ImageBrush
		{
			ImageSource = new BitmapImage(new Uri(screenPath)),
			Stretch = Stretch.UniformToFill,
			AlignmentY = AlignmentY.Top
		}));
		whiteBoard.PlacePx((int)display.BoundsPx.X, (int)display.BoundsPx.Y, (int)display.BoundsPx.Width, (int)display.BoundsPx.Height);
		whiteBoard.ShowNoActivate();
		nint num = FindWindowW(null, "Clicky drawing");
		if (num != 0)
		{
			SetWindowPos(whiteBoard.Hwnd, num, 0, 0, 0, 0, 19u);
		}
	}

	private void EndReplayWhenCleared(CancellationTokenSource cts)
	{
		real.Canvas.Cleared += OnCleared;
		cts.Token.Register(delegate
		{
			dq.TryEnqueue(delegate
			{
				real.Canvas.Cleared -= OnCleared;
			});
		});
		void OnCleared(object? sender, EventArgs e)
		{
			dq.TryEnqueue(delegate
			{
				real.Canvas.Cleared -= OnCleared;
				if (replay == cts)
				{
					if (replaying)
					{
						Log.Info("lessons", "replay stopped: the drawing was cleared");
					}
					StopReplay();
				}
			});
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint FindWindowW(string? cls, string title);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint h, nint after, int x, int y, int w, int ht, uint flags);

	private static byte[]? WhiteJpeg(int w, int h)
	{
		try
		{
			return Task.Run(async delegate
			{
				byte[] pixels = new byte[w * h * 4];
				Array.Fill(pixels, byte.MaxValue);
				using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
				BitmapEncoder obj = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
				obj.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)w, (uint)h, 96.0, 96.0, pixels);
				await obj.FlushAsync();
				byte[] bytes = new byte[stream.Size];
				stream.Seek(0uL);
				using DataReader reader = new DataReader(stream.GetInputStreamAt(0uL));
				await reader.LoadAsync((uint)stream.Size);
				reader.ReadBytes(bytes);
				return bytes;
			}).GetAwaiter().GetResult();
		}
		catch
		{
			return null;
		}
	}
}
