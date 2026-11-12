using System;

namespace HeyClicky.App.DevFakes;

public sealed class FakeAudioOutput : IAudioOutput, IDisposable
{
	public int SampleRate => 24000;

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
