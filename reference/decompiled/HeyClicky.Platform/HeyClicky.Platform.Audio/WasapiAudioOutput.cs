using System;
using System.Collections.Generic;

namespace HeyClicky.Platform.Audio;

public sealed class WasapiAudioOutput : IAudioOutput, IDisposable
{
	public const int VoiceSampleRate = 24000;

	private readonly VoiceRenderStream voiceStream = new VoiceRenderStream();

	private readonly EarconPlayer earconPlayer = new EarconPlayer();

	public int SampleRate => 24000;

	public TimeSpan Buffered => voiceStream.Buffered;

	public double PlayedSeconds => voiceStream.PlayedSeconds;

	public float Level => voiceStream.Level;

	public static IReadOnlyCollection<string> EarconNames => EarconPlayer.KnownNames;

	public event Action? Drained
	{
		add
		{
			voiceStream.Drained += value;
		}
		remove
		{
			voiceStream.Drained -= value;
		}
	}

	public void Enqueue(ReadOnlySpan<byte> pcm16Mono)
	{
		voiceStream.Enqueue(pcm16Mono);
	}

	public void Flush()
	{
		voiceStream.Flush();
	}

	public void PrepareForUpcomingAudio()
	{
		voiceStream.StartWithSilence();
	}

	public void PlayEarcon(string name)
	{
		earconPlayer.Play(name, null);
	}

	public void PlayEarcon(string name, float volume)
	{
		earconPlayer.Play(name, volume);
	}

	public void PreloadEarcons()
	{
		earconPlayer.PreloadAll();
	}

	public void Dispose()
	{
		voiceStream.Dispose();
		earconPlayer.Dispose();
	}
}
