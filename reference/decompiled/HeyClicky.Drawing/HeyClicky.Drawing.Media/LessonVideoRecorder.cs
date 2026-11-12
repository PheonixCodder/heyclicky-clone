using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Drawing.Overlay;

namespace HeyClicky.Drawing.Media;

public sealed class LessonVideoRecorder : IDisposable
{
	private readonly string temporaryVideoPath;

	private readonly Mp4VideoWriter writer;

	private readonly BlockingCollection<(byte[] Pixels, double Seconds)> queue = new BlockingCollection<(byte[], double)>(8);

	private readonly Thread thread;

	private volatile bool playbackStarted;

	private double? firstFramePosition;

	private byte[]? lastFrame;

	private int framesWritten;

	private int framesDropped;

	private bool cancelled;

	public int Width => writer.Width;

	public int Height => writer.Height;

	private LessonVideoRecorder(string temporaryVideoPath, Mp4VideoWriter writer)
	{
		this.temporaryVideoPath = temporaryVideoPath;
		this.writer = writer;
		thread = new Thread(WriteLoop)
		{
			Name = "drawing video",
			IsBackground = true
		};
		thread.Start();
	}

	public static LessonVideoRecorder Start(int width, int height)
	{
		(int Width, int Height) tuple = LessonVideoFiles.VideoSize(width, height);
		int item = tuple.Width;
		int item2 = tuple.Height;
		string path = Path.Combine(Path.GetTempPath(), $"clicky-drawing-{Guid.NewGuid():N}.mp4");
		Mp4VideoWriter mp4VideoWriter = new Mp4VideoWriter(path, item, item2);
		Log.Info("Drawing", $"drawing video: recording {item}x{item2} at 30 fps");
		return new LessonVideoRecorder(path, mp4VideoWriter);
	}

	public void MarkPlaybackStarted()
	{
		playbackStarted = true;
	}

	public void Add(LessonPlaybackFrame frame)
	{
		if (playbackStarted && !cancelled && !queue.IsAddingCompleted)
		{
			double? num = firstFramePosition;
			bool num2 = !num.HasValue;
			double valueOrDefault = firstFramePosition.GetValueOrDefault();
			if (!firstFramePosition.HasValue)
			{
				valueOrDefault = frame.Position;
				firstFramePosition = valueOrDefault;
			}
			double item = (num2 ? 0.0 : Math.Max(0.0, frame.Position));
			byte[] item2 = Fit(frame.Pixels, frame.Width, frame.Height, writer.Width, writer.Height);
			if (!queue.TryAdd((item2, item)))
			{
				Interlocked.Increment(ref framesDropped);
			}
		}
	}

	private static byte[] Fit(byte[] pixels, int width, int height, int videoWidth, int videoHeight)
	{
		if (width == videoWidth && height == videoHeight)
		{
			return pixels;
		}
		byte[] array = new byte[videoWidth * videoHeight * 4];
		int count = Math.Min(width, videoWidth) * 4;
		for (int i = 0; i < Math.Min(height, videoHeight); i++)
		{
			Buffer.BlockCopy(pixels, i * width * 4, array, i * videoWidth * 4, count);
		}
		return array;
	}

	private void WriteLoop()
	{
		foreach (var (array, seconds) in queue.GetConsumingEnumerable())
		{
			if (cancelled)
			{
				continue;
			}
			try
			{
				if (writer.WriteFrame(array, seconds))
				{
					lastFrame = array;
					framesWritten++;
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "drawing video: a frame failed: " + ex.Message);
			}
		}
	}

	public Task<string?> FinishAsync(double duration)
	{
		return Task.Run(delegate
		{
			queue.CompleteAdding();
			thread.Join();
			if (cancelled || lastFrame == null)
			{
				Discard();
				return (string)null;
			}
			double num = duration - 1.0 / 30.0;
			if (num > writer.LastFrameSeconds)
			{
				try
				{
					writer.WriteFrame(lastFrame, num);
				}
				catch (Exception ex)
				{
					Log.Warn("Drawing", "drawing video: the last frame failed: " + ex.Message);
				}
			}
			bool flag = writer.Finish();
			writer.Dispose();
			Log.Info("Drawing", $"drawing video: {framesWritten} frames written, {framesDropped} dropped, {duration:0.0} s{(flag ? "" : ", writing failed")}");
			if (!flag)
			{
				TryDelete(temporaryVideoPath);
				return (string)null;
			}
			return temporaryVideoPath;
		});
	}

	public void Cancel()
	{
		cancelled = true;
		queue.CompleteAdding();
		Task.Run(delegate
		{
			thread.Join();
			Discard();
		});
	}

	private void Discard()
	{
		writer.Dispose();
		TryDelete(temporaryVideoPath);
	}

	public void Dispose()
	{
		if (!queue.IsAddingCompleted)
		{
			Cancel();
		}
	}

	public static string? Save(string videoPath, short[]? soundtrack, string title, string folder)
	{
		try
		{
			Directory.CreateDirectory(folder);
			string text = LessonVideoFiles.UniquePath(folder, LessonVideoFiles.SafeFileName(title), "mp4");
			if (soundtrack == null)
			{
				File.Copy(videoPath, text);
			}
			else
			{
				Mp4Soundtrack.Mux(videoPath, soundtrack, text);
			}
			Log.Info("Drawing", "drawing video: saved " + text);
			return text;
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "drawing video: couldn't save it: " + ex.Message);
			return null;
		}
		finally
		{
			TryDelete(videoPath);
		}
	}

	public static string DownloadsFolder()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_DOWNLOADS_DIR");
		if (environmentVariable == null || environmentVariable.Length <= 0)
		{
			if (!AppPaths.IsOverridden)
			{
				return KnownFolders.Downloads();
			}
			return Directory.CreateDirectory(AppPaths.Combine("Downloads")).FullName;
		}
		return environmentVariable;
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}
}
