using System;

namespace HeyClicky.Platform.Audio;

public static class AudioOutputRoute
{
	public static event Action<AudioOutputRouteChange>? Changed;

	public static event Action<string, string, string>? OutputUnavailable;

	internal static void Raise(AudioOutputRouteChange change)
	{
		Log.Info("AudioOut", $"{change.StreamName}: route {change.FromDevice} ({change.FromTransport}) -> {change.ToDevice} ({change.ToTransport}), dropped {change.DroppedBuffers}, was running {change.EngineWasRunning}, reopened {change.RestartSucceeded}");
		try
		{
			AudioOutputRoute.Changed?.Invoke(change);
		}
		catch (Exception ex)
		{
			Log.Warn("AudioOut", "route observer threw: " + ex.Message);
		}
	}

	internal static void RaiseOutputUnavailable(string streamName, string transport, string device)
	{
		try
		{
			AudioOutputRoute.OutputUnavailable?.Invoke(streamName, transport, device);
		}
		catch (Exception ex)
		{
			Log.Warn("AudioOut", "route observer threw: " + ex.Message);
		}
	}
}
