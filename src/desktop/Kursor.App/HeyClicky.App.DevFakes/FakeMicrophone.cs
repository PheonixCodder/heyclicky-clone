using System;

namespace HeyClicky.App.DevFakes;

public sealed class FakeMicrophone : IMicrophoneCapture, IDisposable
{
	private readonly long start = Environment.TickCount64;

	public int SampleRate => 24000;

	public bool IsRunning { get; private set; }

	public float Level
	{
		get
		{
			double num = (double)(Environment.TickCount64 - start) / 1000.0;
			return (float)Math.Clamp(0.45 + 0.35 * Math.Sin(num * 5.3) * Math.Sin(num * 1.7), 0.0, 1.0);
		}
	}

	public event Action<ReadOnlyMemory<byte>>? Frame;

	public void Start()
	{
		IsRunning = true;
	}

	public void Stop()
	{
		IsRunning = false;
	}

	public void Dispose()
	{
	}
}
