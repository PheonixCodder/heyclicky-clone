using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Drawing.Audio;
using HeyClicky.Drawing.Engine;
using HeyClicky.Drawing.Painter;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Graphics.DirectX;
using Windows.UI.Composition;

namespace HeyClicky.Drawing;

public sealed class LessonPlaybackStage : IDisposable
{
	private const double FramesPerSecond = 30.0;

	private readonly DrawingThread thread;

	private readonly Func<string, CancellationToken, Task<byte[]?>> photoProvider;

	private readonly bool playsSounds;

	private ScreenPainter? painter;

	private NativeEngine? engine;

	private LessonPlaybackScript? script;

	private (byte[] Pixels, int Width, int Height)? decodedScreen;

	private float pixelsPerUnit = 0.5f;

	private bool isPlaying;

	private bool hasEnded;

	private int currentBeat = -1;

	private Timer? frameTimer;

	private int renderInFlight;

	private bool disposed;

	private CanvasBitmap? screenBitmap;

	private CanvasGeometry? penTriangle;

	private List<SoundItem>? capturedSounds;

	public double PointWidth => script?.PointWidth ?? 0.0;

	public double PointHeight
	{
		get
		{
			LessonPlaybackScript lessonPlaybackScript = script;
			if ((object)lessonPlaybackScript == null || lessonPlaybackScript.CanvasWidth <= 0)
			{
				return 0.0;
			}
			return lessonPlaybackScript.PointWidth * (double)lessonPlaybackScript.CanvasHeight / (double)lessonPlaybackScript.CanvasWidth;
		}
	}

	public bool IsPlaying => isPlaying;

	public bool HasEnded => hasEnded;

	public double Duration => script?.Duration ?? 0.0;

	public bool ComposesOverScreen { get; set; }

	public Func<double, LessonPenPose?>? PenPose { get; set; }

	public event Action<LessonPlaybackFrame>? FrameReady;

	public event Action<int>? BeatChanged;

	public event Action? Ended;

	public event Action<IReadOnlyList<PenStrokePath>, double>? PenStrokes;

	public event Action? PenReset;

	public LessonPlaybackStage(Func<string, CancellationToken, Task<byte[]?>> photoProvider, bool playsSounds)
	{
		this.photoProvider = photoProvider;
		this.playsSounds = playsSounds;
		thread = new DrawingThread("Clicky drawing playback", playback: true);
	}

	public Task<bool> LoadAsync(LessonPlaybackScript lessonScript, int stageWidthInPixels)
	{
		TaskCompletionSource<bool> loaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		thread.Post(delegate
		{
			try
			{
				double realNow = DrawingThread.RealNow;
				script = lessonScript;
				pixelsPerUnit = Math.Clamp((float)stageWidthInPixels / (float)Math.Max(1, lessonScript.CanvasWidth), 0.15f, 2f);
				Compositor compositor = new Compositor();
				CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
				string assetsDirectory = ScreenDrawingCanvas.AssetsDirectory;
				painter = new ScreenPainter(compositor, thread, sharedDevice, new TextShaper(sharedDevice, Path.Combine(assetsDirectory, "Fonts")))
				{
					RemovesFinishedPenMasks = false,
					ShowsOnScreen = false,
					PhotoProvider = photoProvider
				};
				painter.PhotoShown += delegate
				{
					if (!isPlaying)
					{
						RenderFrame();
					}
				};
				painter.PenStrokesReady += OnPenStrokes;
				engine = new NativeEngine(thread, painter, Path.Combine(assetsDirectory, "Engine"), playsSounds ? SoundPlayer.Shared : null)
				{
					PlaysPenSounds = false
				};
				if (!engine.Load())
				{
					loaded.TrySetResult(result: false);
				}
				else
				{
					decodedScreen = ScreenshotSampler.DecodeAsync(lessonScript.ScreenJpeg, lessonScript.CanvasWidth, lessonScript.CanvasHeight).GetAwaiter().GetResult();
					if (ComposesOverScreen)
					{
						(byte[], int, int)? tuple = decodedScreen;
						if (tuple.HasValue)
						{
							(byte[], int, int) valueOrDefault = tuple.GetValueOrDefault();
							screenBitmap = CanvasBitmap.CreateFromBytes(sharedDevice, valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3, DirectXPixelFormat.R8G8B8A8UIntNormalized);
						}
					}
					Rebuild(0.0);
					RenderFrame();
					Log.Info("Drawing", $"playback stage ready in {(DrawingThread.RealNow - realNow) * 1000.0:F0} ms: {lessonScript.Lines.Count} beats, {lessonScript.Duration:0.0} s, {pixelsPerUnit:0.00} px per unit");
					loaded.TrySetResult(result: true);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Drawing", "the playback stage failed to load", ex);
				loaded.TrySetResult(result: false);
			}
		});
		return loaded.Task;
	}

	public void Play()
	{
		Post(delegate
		{
			if (!isPlaying)
			{
				if (hasEnded)
				{
					Rebuild(0.0);
				}
				thread.ResumeClock();
				isPlaying = true;
				StartFrames();
			}
		});
	}

	public void Pause()
	{
		Post(delegate
		{
			if (isPlaying)
			{
				thread.PauseClock();
				engine.StopSounds();
				isPlaying = false;
				StopFrames();
				RenderFrame();
			}
		});
	}

	public void Seek(double position)
	{
		Post(delegate
		{
			bool num = isPlaying;
			if (num)
			{
				thread.PauseClock();
			}
			Rebuild(Math.Clamp(position, 0.0, script.Duration));
			if (num)
			{
				thread.ResumeClock();
			}
			RenderFrame();
		});
	}

	public void BeginCapturingSounds()
	{
		Post(delegate
		{
			capturedSounds = new List<SoundItem>();
			engine.SoundsScheduled = delegate(List<SoundItem> items)
			{
				capturedSounds?.AddRange(items);
			};
		});
	}

	public Task<LessonPlaybackSounds> EndCapturingSoundsAsync()
	{
		TaskCompletionSource<LessonPlaybackSounds> done = new TaskCompletionSource<LessonPlaybackSounds>(TaskCreationOptions.RunContinuationsAsynchronously);
		thread.Post(delegate
		{
			if (engine != null)
			{
				engine.SoundsScheduled = null;
			}
			done.TrySetResult(new LessonPlaybackSounds(capturedSounds ?? new List<SoundItem>()));
			capturedSounds = null;
		});
		return done.Task;
	}

	private void OnPenStrokes(IReadOnlyList<PenStroke> strokes)
	{
		if (this.PenStrokes == null || painter == null)
		{
			return;
		}
		double pointsPerUnit = painter.PointsPerUnit;
		List<PenStrokePath> list = new List<PenStrokePath>(strokes.Count);
		foreach (PenStroke stroke in strokes)
		{
			PointD[] array = new PointD[stroke.Points.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new PointD((double)stroke.Points[i].X * pointsPerUnit, (double)stroke.Points[i].Y * pointsPerUnit);
			}
			list.Add(new PenStrokePath(array, stroke.BeginTime, stroke.Duration, stroke.Timing.X, stroke.Timing.Y, stroke.Timing.Z, stroke.Timing.W));
		}
		try
		{
			this.PenStrokes?.Invoke(list, DrawingThread.Now);
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "playback pen handler: " + ex.Message);
		}
	}

	private void Post(Action action)
	{
		thread.Post(delegate
		{
			if (!disposed && engine != null && painter != null && (object)script != null)
			{
				action();
			}
		});
	}

	private void Rebuild(double position)
	{
		LessonPlaybackScript lessonPlaybackScript = script;
		engine.Clear();
		thread.ResetClock();
		hasEnded = false;
		SetBeat(-1);
		Raise(this.PenReset);
		engine.PlaysSounds = false;
		painter.Configure(new Vector2(lessonPlaybackScript.CanvasWidth, lessonPlaybackScript.CanvasHeight), lessonPlaybackScript.PointWidth, lessonPlaybackScript.DisplayScale);
		painter.StageVisual.Offset = Vector3.Zero;
		engine.Screenshot.Set(decodedScreen);
		NativeEngine.ClickyInkHex = (string.IsNullOrWhiteSpace(lessonPlaybackScript.InkHex) ? "#3380FF" : lessonPlaybackScript.InkHex.ToUpperInvariant());
		if (!engine.BeginStream(lessonPlaybackScript.CanvasWidth, lessonPlaybackScript.CanvasHeight, lessonPlaybackScript.PointWidth, subtitles: false))
		{
			Log.Warn("Drawing", "playback: beginStream failed");
			return;
		}
		engine.Feed(ScreenDrawingCanvas.WithoutLeadingProse(ScreenDrawingCanvas.WithoutWebSearchCitations(lessonPlaybackScript.Program)), done: true);
		for (int i = 0; i < lessonPlaybackScript.Lines.Count; i++)
		{
			int beat = i;
			(double Start, double? Length) line = lessonPlaybackScript.Lines[i];
			thread.ScheduleTimer(line.Start, delegate
			{
				engine.Line(beat);
				double? item = line.Length;
				if (item.HasValue)
				{
					double valueOrDefault = item.GetValueOrDefault();
					if (valueOrDefault > 0.0)
					{
						engine.LineLength(beat, valueOrDefault);
					}
				}
				SetBeat(beat);
			});
		}
		thread.ScheduleTimer(lessonPlaybackScript.Duration, ReachEnd);
		thread.JumpClock(position);
		engine.StopSounds();
		engine.PlaysSounds = playsSounds;
	}

	private void ReachEnd()
	{
		engine.Finish();
		hasEnded = true;
		thread.PauseClock();
		isPlaying = false;
		StopFrames();
		RenderFrame();
		Raise(this.Ended);
	}

	private void SetBeat(int beat)
	{
		if (beat == currentBeat)
		{
			return;
		}
		currentBeat = beat;
		try
		{
			this.BeatChanged?.Invoke(beat);
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "playback beat handler: " + ex.Message);
		}
	}

	private static void Raise(Action? handler)
	{
		try
		{
			handler?.Invoke();
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "playback handler: " + ex.Message);
		}
	}

	private void StartFrames()
	{
		StopFrames();
		frameTimer = new Timer(delegate
		{
			if (Interlocked.CompareExchange(ref renderInFlight, 1, 0) == 0)
			{
				thread.Post(delegate
				{
					try
					{
						if (!disposed && isPlaying)
						{
							RenderFrame();
						}
					}
					finally
					{
						Volatile.Write(ref renderInFlight, 0);
					}
				});
			}
		}, null, TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 30.0));
	}

	private void StopFrames()
	{
		frameTimer?.Dispose();
		frameTimer = null;
	}

	private void RenderFrame()
	{
		if (painter == null || this.FrameReady == null)
		{
			return;
		}
		try
		{
			double now = DrawingThread.Now;
			using CanvasRenderTarget canvasRenderTarget = painter.RenderSnapshot(now, pixelsPerUnit, screenBitmap);
			LessonPenPose? lessonPenPose = PenPose?.Invoke(now);
			if (lessonPenPose.HasValue)
			{
				LessonPenPose valueOrDefault = lessonPenPose.GetValueOrDefault();
				DrawPen(canvasRenderTarget, valueOrDefault);
			}
			byte[] pixelBytes = canvasRenderTarget.GetPixelBytes();
			this.FrameReady?.Invoke(new LessonPlaybackFrame(pixelBytes, (int)canvasRenderTarget.SizeInPixels.Width, (int)canvasRenderTarget.SizeInPixels.Height, now));
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "playback frame failed: " + ex.Message);
		}
	}

	private void DrawPen(CanvasRenderTarget picture, LessonPenPose pen)
	{
		if ((object)script == null || script.PointWidth <= 0.0)
		{
			return;
		}
		CanvasDevice device = picture.Device;
		if ((object)penTriangle == null)
		{
			penTriangle = RoundedTriangle(device);
		}
		float num = (float)picture.SizeInPixels.Width / (float)script.PointWidth;
		Matrix3x2 transform = Matrix3x2.CreateScale((float)pen.Scale) * Matrix3x2.CreateRotation((float)((-35.0 + pen.TurnDegrees) * Math.PI / 180.0)) * Matrix3x2.CreateScale(num) * Matrix3x2.CreateTranslation((float)(pen.X * (double)num), (float)(pen.Y * (double)num));
		using CanvasCommandList canvasCommandList = new CanvasCommandList(device);
		using (CanvasDrawingSession canvasDrawingSession = canvasCommandList.CreateDrawingSession())
		{
			canvasDrawingSession.Transform = transform;
			canvasDrawingSession.FillGeometry(penTriangle, pen.Tint);
		}
		using ShadowEffect image = new ShadowEffect
		{
			Source = canvasCommandList,
			ShadowColor = pen.Tint,
			BlurAmount = (float)(8.0 * pen.Scale * (double)num / 2.0)
		};
		using CanvasDrawingSession canvasDrawingSession2 = picture.CreateDrawingSession();
		canvasDrawingSession2.DrawImage(image);
		canvasDrawingSession2.Transform = transform;
		canvasDrawingSession2.FillGeometry(penTriangle, pen.Tint);
	}

	private static CanvasGeometry RoundedTriangle(ICanvasResourceCreator device)
	{
		float num = 16f * MathF.Sqrt(3f) / 2f;
		Vector2[] array = new Vector2[3]
		{
			new Vector2(0f, (0f - num) / 1.5f),
			new Vector2(-8f, num / 3f),
			new Vector2(8f, num / 3f)
		};
		float num2 = 1.44f;
		float num3 = num2 * MathF.Sqrt(3f);
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(device);
		canvasPathBuilder.BeginFigure((array[0] + array[1]) / 2f);
		for (int i = 1; i <= 3; i++)
		{
			Vector2 vector = array[i % 3];
			Vector2 vector2 = array[(i - 1) % 3];
			Vector2 vector3 = array[(i + 1) % 3];
			canvasPathBuilder.AddLine(vector + Vector2.Normalize(vector2 - vector) * num3);
			canvasPathBuilder.AddArc(vector + Vector2.Normalize(vector3 - vector) * num3, num2, num2, 0f, CanvasSweepDirection.CounterClockwise, CanvasArcSize.Small);
		}
		canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
		return CanvasGeometry.CreatePath(canvasPathBuilder);
	}

	public void Refresh()
	{
		Post(RenderFrame);
	}

	public void Dispose()
	{
		try
		{
			thread.Invoke(delegate
			{
				disposed = true;
				StopFrames();
				engine?.StopSounds();
				engine?.Dispose();
				painter?.ClearAll();
				screenBitmap?.Dispose();
				penTriangle?.Dispose();
			});
		}
		catch
		{
		}
		thread.Dispose();
	}
}
