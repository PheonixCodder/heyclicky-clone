using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HeyClicky.App.Home;
using HeyClicky.Drawing;
using HeyClicky.Platform.Overlay;
using Microsoft.UI.Dispatching;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed class ClickyLessonInlinePlayer : IDisposable
{
	private readonly LessonPlaybackStage stage;

	private readonly LessonPlaybackScript? script;

	private readonly (byte[] Pcm16Mono, int SampleRate)? narration;

	private readonly LessonVoice? voice;

	private readonly DispatcherQueue dq;

	private readonly Color penTint;

	private PenRideTrajectory? penRide;

	private bool isLoaded;

	private bool disposed;

	public StoredLesson Stored { get; }

	public StoredLesson.Meta Lesson => Stored.Data;

	public bool IsPlaying { get; private set; }

	public bool HasEnded { get; private set; }

	public int? CurrentBeat { get; private set; }

	public double Position { get; private set; }

	public double Duration => script?.Duration ?? Math.Max(1.0, Lesson.Duration - Stored.ReplayShift);

	public (int Width, int Height) FrameSize { get; private set; }

	public (byte[] Pcm16Mono, int SampleRate)? Narration => narration;

	public event Action? Changed;

	public event Action<double>? ProgressChanged;

	public event Action<LessonPlaybackFrame>? FrameReady;

	public ClickyLessonInlinePlayer(StoredLesson stored, LessonService service)
	{
		Stored = stored;
		dq = DispatcherQueue.GetForCurrentThread();
		script = stored.PlaybackScript();
		narration = stored.ReadNarration();
		(byte[], int)? tuple = narration;
		if (tuple.HasValue)
		{
			voice = new LessonVoice();
		}
		penTint = ParseHex(service.CursorColorHex) ?? Color.FromArgb(byte.MaxValue, 51, 128, byte.MaxValue);
		stage = service.NewPlaybackStage(stored);
		stage.ComposesOverScreen = true;
		stage.FrameReady += delegate(LessonPlaybackFrame frame)
		{
			FrameSize = (Width: frame.Width, Height: frame.Height);
			this.FrameReady?.Invoke(frame);
			dq.TryEnqueue(delegate
			{
				if (!disposed)
				{
					Position = Math.Min(frame.Position, Duration);
					this.ProgressChanged?.Invoke(Position);
				}
			});
		};
		stage.BeatChanged += delegate(int beat)
		{
			dq.TryEnqueue(delegate
			{
				if (!disposed)
				{
					CurrentBeat = ((beat < 0) ? ((int?)null) : new int?(beat));
					this.Changed?.Invoke();
				}
			});
		};
		stage.Ended += delegate
		{
			dq.TryEnqueue(ReachEnd);
		};
		stage.PenReset += delegate
		{
			penRide = null;
		};
		stage.PenStrokes += delegate(IReadOnlyList<PenStrokePath> strokes, double now)
		{
			double pointWidth = stage.PointWidth;
			double pointHeight = stage.PointHeight;
			if (!(pointWidth <= 0.0) && strokes.Count != 0)
			{
				if (penRide == null)
				{
					PointD? pointD = strokes.FirstOrDefault((PenStrokePath s) => s.PointsPx.Count > 0)?.PointsPx[0];
					if (!pointD.HasValue)
					{
						return;
					}
					PointD valueOrDefault = pointD.GetValueOrDefault();
					penRide = new PenRideTrajectory();
					penRide.Begin(new PointD(Math.Max(20.0, Math.Min(valueOrDefault.X + 8.0, pointWidth - 20.0)), Math.Max(20.0, Math.Min(valueOrDefault.Y + 12.0, pointHeight - 20.0))), now, pointWidth, pointHeight);
				}
				penRide.Add(strokes.Select((PenStrokePath s) => (s.PointsPx.ToArray(), BeginTime: s.BeginTime, Duration: s.Duration, X1: s.X1, Y1: s.Y1, X2: s.X2, Y2: s.Y2)), now);
			}
		};
		stage.PenPose = delegate(double time)
		{
			PenRideTrajectory penRideTrajectory = penRide;
			if (penRideTrajectory == null || penRideTrajectory.IsEmpty)
			{
				return (LessonPenPose?)null;
			}
			(PointD, PointD, double, double) tuple2 = penRideTrajectory.Sample(time);
			return new LessonPenPose(tuple2.Item1.X, tuple2.Item1.Y, tuple2.Item3, tuple2.Item4, penTint);
		};
	}

	public async Task<bool> LoadAsync(int stageWidthInPixels)
	{
		if (isLoaded)
		{
			return true;
		}
		if ((object)script == null)
		{
			return false;
		}
		isLoaded = await stage.LoadAsync(script, stageWidthInPixels);
		return isLoaded;
	}

	public void Play()
	{
		if (isLoaded && !disposed)
		{
			if (HasEnded)
			{
				Position = 0.0;
				HasEnded = false;
			}
			stage.Play();
			(byte[], int)? tuple = narration;
			if (tuple.HasValue)
			{
				(byte[], int) valueOrDefault = tuple.GetValueOrDefault();
				voice.Play(valueOrDefault, Position + Stored.ReplayShift);
			}
			IsPlaying = true;
			this.Changed?.Invoke();
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 2);
			defaultInterpolatedStringHandler.AppendLiteral("drawing window: playing from ");
			defaultInterpolatedStringHandler.AppendFormatted(Position, "0.00");
			defaultInterpolatedStringHandler.AppendLiteral(" s");
			tuple = narration;
			defaultInterpolatedStringHandler.AppendFormatted((!tuple.HasValue) ? " (no recorded voice)" : "");
			Log.Info("lessons", defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}

	public void Pause()
	{
		if (IsPlaying)
		{
			stage.Pause();
			voice?.Stop();
			IsPlaying = false;
			this.Changed?.Invoke();
		}
	}

	public void TogglePlay()
	{
		if (IsPlaying)
		{
			Pause();
		}
		else
		{
			Play();
		}
	}

	public void SeekToBeat(int index)
	{
		IReadOnlyList<(double, double?)> readOnlyList = script?.Lines;
		if (readOnlyList != null && index >= 0 && index < readOnlyList.Count)
		{
			Seek(readOnlyList[index].Item1);
		}
	}

	public void Seek(double position)
	{
		if (!isLoaded || disposed)
		{
			return;
		}
		Position = Math.Clamp(position, 0.0, Duration);
		HasEnded = false;
		stage.Seek(Position);
		if (IsPlaying)
		{
			(byte[], int)? tuple = narration;
			if (tuple.HasValue)
			{
				(byte[], int) valueOrDefault = tuple.GetValueOrDefault();
				voice.Play(valueOrDefault, Position + Stored.ReplayShift);
			}
		}
		this.ProgressChanged?.Invoke(Position);
		this.Changed?.Invoke();
	}

	public void BeginCapturingSounds()
	{
		stage.BeginCapturingSounds();
	}

	public Task<LessonPlaybackSounds> EndCapturingSoundsAsync()
	{
		return stage.EndCapturingSoundsAsync();
	}

	private void ReachEnd()
	{
		if (!disposed)
		{
			HasEnded = true;
			IsPlaying = false;
			voice?.Stop();
			Position = Duration;
			this.ProgressChanged?.Invoke(Position);
			this.Changed?.Invoke();
		}
	}

	private static Color? ParseHex(string? hex)
	{
		string text = (hex ?? "").TrimStart('#');
		if (text.Length != 6 || !uint.TryParse(text, NumberStyles.HexNumber, null, out var result))
		{
			return null;
		}
		return Color.FromArgb(byte.MaxValue, (byte)(result >> 16), (byte)(result >> 8), (byte)result);
	}

	public void Dispose()
	{
		if (!disposed)
		{
			Pause();
			disposed = true;
			voice?.Dispose();
			Task.Run((Action)stage.Dispose);
		}
	}
}
