using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32.Media.Audio;

namespace HeyClicky.Platform.Audio;

internal sealed class VoiceRenderStream : WasapiRenderStream
{
	private const int BytesPerSample = 2;

	private const int JitterPrebufferMs = 60;

	private const int UnderrunRefillFrames = 960;

	private readonly object queueGate = new object();

	private byte[] queue = new byte[480000];

	private int queueReadOffset;

	private int queueCount;

	private bool waitingForRefillAfterUnderrun;

	private readonly long[] writeEndStreamFrame = new long[128];

	private readonly long[] realFramesCumulativeAtWriteEnd = new long[128];

	private readonly int[] realFramesInWrite = new int[128];

	private int writeRecordCount;

	private int writeRecordNewest = -1;

	private long realFramesWrittenThisRun;

	private long realFramesPlayedBeforeThisRun;

	private long realFramesPlayedThisRun;

	private long lastClockUpdateTimestamp;

	private bool lastClockUpdateWasPlayingRealAudio;

	private long flushGeneration;

	private float smoothedLevel;

	private bool drainReported;

	protected override TimeSpan PrebufferWait
	{
		get
		{
			lock (queueGate)
			{
				int num = queueCount / 2 * 1000 / SampleRate;
				return (num >= 60) ? TimeSpan.Zero : TimeSpan.FromMilliseconds(60 - num, 0L);
			}
		}
	}

	protected override bool HasPendingAudio
	{
		get
		{
			lock (queueGate)
			{
				return queueCount > 0;
			}
		}
	}

	public TimeSpan Buffered
	{
		get
		{
			long num;
			lock (queueGate)
			{
				num = queueCount / 2 + (realFramesWrittenThisRun - realFramesPlayedThisRun);
			}
			return TimeSpan.FromSeconds((double)num / (double)SampleRate);
		}
	}

	public double PlayedSeconds
	{
		get
		{
			lock (queueGate)
			{
				double num = (double)(realFramesPlayedBeforeThisRun + realFramesPlayedThisRun) / (double)SampleRate;
				if (lastClockUpdateWasPlayingRealAudio)
				{
					double totalSeconds = Stopwatch.GetElapsedTime(lastClockUpdateTimestamp).TotalSeconds;
					double val = (double)(realFramesWrittenThisRun - realFramesPlayedThisRun) / (double)SampleRate;
					num += Math.Min(totalSeconds, Math.Min(val, 0.05));
				}
				return num;
			}
		}
	}

	public float Level => Volatile.Read(in smoothedLevel);

	public event Action? Drained;

	public VoiceRenderStream()
		: base("voice", 24000, 2, 40)
	{
	}

	protected unsafe override void InitializeClient(IAudioClient client, long bufferDurationHns)
	{
		WAVEFORMATEX wAVEFORMATEX = CoreAudio.Pcm16(SampleRate, 1);
		client.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, 2281963520u, bufferDurationHns, 0L, &wAVEFORMATEX, null);
	}

	public void Enqueue(ReadOnlySpan<byte> pcm16Mono)
	{
		if (!pcm16Mono.IsEmpty)
		{
			lock (queueGate)
			{
				EnsureQueueCapacity(queueCount + pcm16Mono.Length);
				int num = (queueReadOffset + queueCount) % queue.Length;
				int num2 = Math.Min(pcm16Mono.Length, queue.Length - num);
				pcm16Mono.Slice(0, num2).CopyTo(queue.AsSpan(num));
				int num3 = num2;
				pcm16Mono.Slice(num3, pcm16Mono.Length - num3).CopyTo(queue.AsSpan(0));
				queueCount += pcm16Mono.Length;
			}
			Wake();
		}
	}

	public void Flush()
	{
		lock (queueGate)
		{
			queueReadOffset = 0;
			queueCount = 0;
			waitingForRefillAfterUnderrun = false;
			realFramesPlayedBeforeThisRun = 0L;
			realFramesPlayedThisRun = 0L;
			realFramesWrittenThisRun = 0L;
			writeRecordCount = 0;
			writeRecordNewest = -1;
			lastClockUpdateWasPlayingRealAudio = false;
			flushGeneration++;
		}
		Volatile.Write(ref smoothedLevel, 0f);
		RequestDeviceFlush();
	}

	private void EnsureQueueCapacity(int requiredBytes)
	{
		if (requiredBytes > queue.Length)
		{
			int num;
			for (num = queue.Length; num < requiredBytes; num *= 2)
			{
			}
			byte[] array = new byte[num];
			int num2 = Math.Min(queueCount, queue.Length - queueReadOffset);
			queue.AsSpan(queueReadOffset, num2).CopyTo(array);
			queue.AsSpan(0, queueCount - num2).CopyTo(array.AsSpan(num2));
			queue = array;
			queueReadOffset = 0;
		}
	}

	protected override int Fill(Span<byte> destination, int frameCount)
	{
		int num2;
		lock (queueGate)
		{
			int num = queueCount / 2;
			if (waitingForRefillAfterUnderrun && num < 960)
			{
				return 0;
			}
			waitingForRefillAfterUnderrun = false;
			num2 = Math.Min(frameCount, num);
			int num3 = num2 * 2;
			int num4 = Math.Min(num3, queue.Length - queueReadOffset);
			queue.AsSpan(queueReadOffset, num4).CopyTo(destination);
			Span<byte> span = queue.AsSpan(0, num3 - num4);
			int num5 = num4;
			span.CopyTo(destination.Slice(num5, destination.Length - num5));
			queueReadOffset = (queueReadOffset + num3) % queue.Length;
			queueCount -= num3;
			if (queueCount == 0)
			{
				queueReadOffset = 0;
			}
			if (num2 < frameCount && num2 > 0)
			{
				waitingForRefillAfterUnderrun = true;
			}
		}
		UpdateLevel(destination.Slice(0, num2 * 2));
		return num2;
	}

	private void UpdateLevel(ReadOnlySpan<byte> pcm)
	{
		float val = 0f;
		if (!pcm.IsEmpty)
		{
			ReadOnlySpan<short> readOnlySpan = MemoryMarshal.Cast<byte, short>(pcm);
			double num = 0.0;
			ReadOnlySpan<short> readOnlySpan2 = readOnlySpan;
			for (int i = 0; i < readOnlySpan2.Length; i++)
			{
				double num2 = (double)readOnlySpan2[i] / 32768.0;
				num += num2 * num2;
			}
			val = Math.Min((float)Math.Sqrt(num / (double)readOnlySpan.Length) * 10.2f, 1f);
		}
		float num3 = Volatile.Read(in smoothedLevel);
		Volatile.Write(ref smoothedLevel, Math.Max(val, num3 * 0.72f));
	}

	protected override void OnFramesWritten(long streamFramesAfterWrite, int realFrames, int totalFrames)
	{
		lock (queueGate)
		{
			realFramesWrittenThisRun += realFrames;
			writeRecordNewest = (writeRecordNewest + 1) % writeEndStreamFrame.Length;
			writeEndStreamFrame[writeRecordNewest] = streamFramesAfterWrite;
			realFramesCumulativeAtWriteEnd[writeRecordNewest] = realFramesWrittenThisRun;
			realFramesInWrite[writeRecordNewest] = realFrames;
			writeRecordCount = Math.Min(writeRecordCount + 1, writeEndStreamFrame.Length);
		}
	}

	protected override void OnClockPosition(long playedStreamFrames)
	{
		bool flag = false;
		lock (queueGate)
		{
			long num = realFramesWrittenThisRun;
			bool flag2 = false;
			int num2 = writeRecordNewest;
			for (int i = 0; i < writeRecordCount; i++)
			{
				int num3 = (num2 - 1 + writeEndStreamFrame.Length) % writeEndStreamFrame.Length;
				long num4 = ((i + 1 < writeRecordCount) ? writeEndStreamFrame[num3] : 0);
				long num5 = ((i + 1 < writeRecordCount) ? realFramesCumulativeAtWriteEnd[num3] : (realFramesCumulativeAtWriteEnd[num2] - realFramesInWrite[num2]));
				if (playedStreamFrames >= num4)
				{
					long num6 = playedStreamFrames - num4;
					num = num5 + Math.Min(num6, realFramesInWrite[num2]);
					flag2 = num6 < realFramesInWrite[num2];
					break;
				}
				num2 = num3;
			}
			if (num > realFramesWrittenThisRun)
			{
				num = realFramesWrittenThisRun;
			}
			if (num > realFramesPlayedThisRun)
			{
				realFramesPlayedThisRun = num;
			}
			lastClockUpdateTimestamp = Stopwatch.GetTimestamp();
			lastClockUpdateWasPlayingRealAudio = flag2;
			flag = !flag2 && queueCount == 0 && realFramesPlayedThisRun == realFramesWrittenThisRun && realFramesWrittenThisRun > 0 && !drainReported;
			if (flag)
			{
				drainReported = true;
			}
			if (queueCount > 0)
			{
				drainReported = false;
			}
		}
		if (flag)
		{
			Volatile.Write(ref smoothedLevel, 0f);
			this.Drained?.Invoke();
		}
	}

	protected override void OnStreamReset(bool causedByFlush)
	{
		lock (queueGate)
		{
			if (!causedByFlush)
			{
				realFramesPlayedBeforeThisRun += realFramesWrittenThisRun;
			}
			realFramesWrittenThisRun = 0L;
			realFramesPlayedThisRun = 0L;
			writeRecordCount = 0;
			writeRecordNewest = -1;
			lastClockUpdateWasPlayingRealAudio = false;
		}
		Volatile.Write(ref smoothedLevel, 0f);
	}
}
