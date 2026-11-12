using System;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Voice;

namespace HeyClicky.App.Proactive;

public sealed class TapVoiceCapture
{
	private readonly RealStack real;

	private readonly object gate = new object();

	private DeepgramTranscriber? ears;

	private Action<string>? onTranscript;

	public bool IsActive
	{
		get
		{
			lock (gate)
			{
				return ears != null;
			}
		}
	}

	public event Action<bool>? ActiveChanged;

	public TapVoiceCapture(RealStack real)
	{
		this.real = real;
	}

	public bool Start(Action<string> onFinalTranscript)
	{
		if (!real.Core.Auth.IsSignedIn)
		{
			return false;
		}
		lock (gate)
		{
			if (ears != null)
			{
				return true;
			}
			AppSettings current = real.Core.Settings.Current;
			ears = new DeepgramTranscriber(real.Core.Http, (bool force, CancellationToken ct) => real.Core.Auth.GetValidAccessTokenAsync(force, ct), current.DictationLanguageCode, Array.Empty<string>(), "companion", real.Microphone.SampleRate);
			ears.Start();
			onTranscript = onFinalTranscript;
		}
		real.Microphone.Frame += OnFrame;
		real.MicrophoneStartCue.Play("textOpen", null);
		try
		{
			real.Microphone.Start();
		}
		catch (Exception ex)
		{
			Log.Warn("voice", "tap capture microphone: " + ex.Message);
			real.MicrophoneStartCue.Cancel();
			Cancel();
			return false;
		}
		this.ActiveChanged?.Invoke(obj: true);
		return true;
	}

	private void OnFrame(ReadOnlyMemory<byte> frame)
	{
		DeepgramTranscriber deepgramTranscriber;
		lock (gate)
		{
			deepgramTranscriber = ears;
		}
		deepgramTranscriber?.PushAudio(frame.Span);
	}

	public void Stop()
	{
		DeepgramTranscriber e;
		Action<string> done;
		lock (gate)
		{
			e = ears;
			done = onTranscript;
			ears = null;
			onTranscript = null;
		}
		if (e == null)
		{
			return;
		}
		StopMicrophone();
		this.ActiveChanged?.Invoke(obj: false);
		Task.Run(async delegate
		{
			string transcript = null;
			try
			{
				transcript = (await e.FinishAsync(null).ConfigureAwait(continueOnCapturedContext: false))?.Trim();
			}
			catch (Exception ex)
			{
				Log.Warn("voice", "tap capture transcription: " + ex.Message);
			}
			finally
			{
				await e.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (string.IsNullOrWhiteSpace(transcript))
			{
				Log.Info("voice", "tap capture: heard nothing");
			}
			else
			{
				real.Audio.PlayEarcon("textSend");
				done?.Invoke(transcript);
			}
		});
	}

	public bool Cancel()
	{
		DeepgramTranscriber deepgramTranscriber;
		lock (gate)
		{
			deepgramTranscriber = ears;
			ears = null;
			onTranscript = null;
		}
		if (deepgramTranscriber == null)
		{
			return false;
		}
		StopMicrophone();
		deepgramTranscriber.DisposeAsync().AsTask();
		this.ActiveChanged?.Invoke(obj: false);
		return true;
	}

	private void StopMicrophone()
	{
		real.Microphone.Frame -= OnFrame;
		try
		{
			real.Microphone.Stop();
		}
		catch
		{
		}
	}
}
