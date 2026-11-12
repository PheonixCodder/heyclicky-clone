using System;

namespace HeyClicky;

public interface IAudioOutput : IDisposable
{
	int SampleRate { get; }

	TimeSpan Buffered { get; }

	double PlayedSeconds { get; }

	float Level { get; }

	void Enqueue(ReadOnlySpan<byte> pcm16Mono);

	void Flush();

	void PlayEarcon(string name);

	void PrepareForUpcomingAudio()
	{
	}
}
