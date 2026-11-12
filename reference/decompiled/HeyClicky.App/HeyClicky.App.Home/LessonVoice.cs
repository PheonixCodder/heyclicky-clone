using System;
using HeyClicky.App.Wiring;
using HeyClicky.Platform.Audio;

namespace HeyClicky.App.Home;

public sealed class LessonVoice : IDisposable
{
	private readonly IAudioOutput output;

	private readonly WasapiAudioOutput device = new WasapiAudioOutput();

	private byte[] pcm = Array.Empty<byte>();

	private int sampleRate = 24000;

	private double startedAt;

	public double Position => startedAt + output.PlayedSeconds;

	public LessonVoice()
	{
		IAudioOutput audioOutput2;
		if (!(Environment.GetEnvironmentVariable("HEYCLICKY_MUTE_AUDIO") == "1"))
		{
			IAudioOutput audioOutput = device;
			audioOutput2 = audioOutput;
		}
		else
		{
			IAudioOutput audioOutput = new MutedAudioOutput(device);
			audioOutput2 = audioOutput;
		}
		output = audioOutput2;
	}

	public void Play((byte[] Pcm16Mono, int SampleRate) track, double fromSeconds)
	{
		output.Flush();
		(pcm, sampleRate) = track;
		if (sampleRate != output.SampleRate)
		{
			Log.Warn("lessons", $"narration at {sampleRate} Hz, output at {output.SampleRate} Hz: not played");
		}
		else
		{
			int num = Math.Clamp((int)(fromSeconds * (double)sampleRate) * 2, 0, pcm.Length);
			startedAt = fromSeconds;
			if (num < pcm.Length)
			{
				output.Enqueue(pcm.AsSpan(num));
			}
		}
	}

	public void Stop()
	{
		output.Flush();
	}

	public void Dispose()
	{
		output.Flush();
		device.Dispose();
	}
}
