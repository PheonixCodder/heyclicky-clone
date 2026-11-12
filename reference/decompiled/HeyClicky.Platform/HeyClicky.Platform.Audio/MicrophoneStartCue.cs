using System;
using System.Threading.Tasks;

namespace HeyClicky.Platform.Audio;

public sealed class MicrophoneStartCue : IDisposable
{
	private sealed record PendingCue(string Name, float? Volume, long Serial, long RequestedTicks);

	public const int MaximumWaitMilliseconds = 250;

	private readonly IMicrophoneCapture microphone;

	private readonly Action<string, float?> play;

	private readonly object gate = new object();

	private PendingCue? pending;

	private long serial;

	public MicrophoneStartCue(IMicrophoneCapture microphone, Action<string, float?> play)
	{
		this.microphone = microphone;
		this.play = play;
		microphone.StreamStarted += OnStreamStarted;
	}

	public void Play(string name, float? volume = null)
	{
		long mine;
		lock (gate)
		{
			mine = ++serial;
			pending = new PendingCue(name, volume, mine, Environment.TickCount64);
		}
		Task.Delay(250).ContinueWith(delegate
		{
			PlayPending(mine, "mic not running yet");
		}, TaskScheduler.Default);
	}

	public void Cancel()
	{
		lock (gate)
		{
			pending = null;
		}
	}

	private void OnStreamStarted()
	{
		Task.Run(delegate
		{
			PlayPending(null, "mic running");
		});
	}

	private void PlayPending(long? onlyIfSerial, string reason)
	{
		PendingCue pendingCue;
		lock (gate)
		{
			if ((object)pending == null)
			{
				return;
			}
			if (onlyIfSerial.HasValue)
			{
				long valueOrDefault = onlyIfSerial.GetValueOrDefault();
				if (valueOrDefault != pending.Serial)
				{
					return;
				}
			}
			pendingCue = pending;
			pending = null;
		}
		Log.Info("Earcon", $"{pendingCue.Name} after {Environment.TickCount64 - pendingCue.RequestedTicks} ms ({reason})");
		try
		{
			play(pendingCue.Name, pendingCue.Volume);
		}
		catch (Exception ex)
		{
			Log.Warn("Earcon", "press chime: " + ex.Message);
		}
	}

	public void Dispose()
	{
		microphone.StreamStarted -= OnStreamStarted;
	}
}
