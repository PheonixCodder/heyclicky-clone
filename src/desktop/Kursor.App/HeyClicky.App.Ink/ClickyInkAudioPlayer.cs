using System;
using Microsoft.UI.Dispatching;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkAudioPlayer
{
	private MediaPlayer? player;

	private readonly DispatcherQueue? dispatcher = DispatcherQueue.GetForCurrentThread();

	public static ClickyInkAudioPlayer Shared { get; } = new ClickyInkAudioPlayer();

	public string? PlayingPath { get; private set; }

	public event Action? Changed;

	public void Toggle(string path)
	{
		if (PlayingPath == path)
		{
			Stop();
			return;
		}
		Stop();
		try
		{
			MediaPlayer mediaPlayer = new MediaPlayer
			{
				Source = MediaSource.CreateFromUri(new Uri(path))
			};
			mediaPlayer.MediaEnded += delegate
			{
				dispatcher?.TryEnqueue(Stop);
			};
			mediaPlayer.MediaFailed += delegate(MediaPlayer _, MediaPlayerFailedEventArgs e)
			{
				Log.Warn("ink", "note gallery sound failed: " + e.ErrorMessage);
				dispatcher?.TryEnqueue(Stop);
			};
			player = mediaPlayer;
			PlayingPath = path;
			this.Changed?.Invoke();
			mediaPlayer.Play();
		}
		catch (Exception ex)
		{
			Log.Warn("ink", "note gallery sound: " + ex.Message);
			player = null;
			PlayingPath = null;
		}
	}

	public void Stop()
	{
		MediaPlayer mediaPlayer = player;
		if ((object)mediaPlayer != null)
		{
			try
			{
				mediaPlayer.Pause();
				mediaPlayer.Dispose();
			}
			catch
			{
			}
		}
		player = null;
		if (PlayingPath != null)
		{
			PlayingPath = null;
			this.Changed?.Invoke();
		}
	}
}
