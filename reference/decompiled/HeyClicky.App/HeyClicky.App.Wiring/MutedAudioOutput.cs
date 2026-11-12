using System;
using System.Buffers;

namespace HeyClicky.App.Wiring;

public sealed class MutedAudioOutput(IAudioOutput inner) : IAudioOutput, IDisposable
{
	public int SampleRate => inner.SampleRate;

	public TimeSpan Buffered => inner.Buffered;

	public double PlayedSeconds => inner.PlayedSeconds;

	public float Level => 0f;

	public void Enqueue(ReadOnlySpan<byte> pcm16Mono)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(pcm16Mono.Length);
		try
		{
			Array.Clear(array, 0, pcm16Mono.Length);
			inner.Enqueue(array.AsSpan(0, pcm16Mono.Length));
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public void Flush()
	{
		inner.Flush();
	}

	public void PlayEarcon(string name)
	{
	}

	public void Dispose()
	{
	}
}
