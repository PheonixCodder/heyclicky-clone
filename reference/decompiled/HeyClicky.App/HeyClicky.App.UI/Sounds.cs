using System;
using System.Collections.Generic;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace HeyClicky.App.UI;

public static class Sounds
{
	private static readonly Dictionary<string, MediaPlayer> players = new Dictionary<string, MediaPlayer>();

	private static readonly bool Muted = Environment.GetEnvironmentVariable("HEYCLICKY_MUTE_AUDIO") == "1";

	public static void Play(string name, double volume)
	{
		if (Muted)
		{
			return;
		}
		try
		{
			if (!players.TryGetValue(name, out MediaPlayer value))
			{
				string text = Path.Combine(AppContext.BaseDirectory, "Assets", "Earcons", name + ".wav");
				if (!File.Exists(text))
				{
					return;
				}
				value = new MediaPlayer
				{
					Source = MediaSource.CreateFromUri(new Uri(text)),
					AudioCategory = MediaPlayerAudioCategory.SoundEffects
				};
				players[name] = value;
			}
			value.Volume = volume;
			value.PlaybackSession.Position = TimeSpan.Zero;
			value.Play();
		}
		catch (Exception ex)
		{
			Log.Warn("sounds", name + ": " + ex.Message);
		}
	}

	public static void Stop(string name)
	{
		if (players.TryGetValue(name, out MediaPlayer value))
		{
			value.Pause();
		}
	}
}
