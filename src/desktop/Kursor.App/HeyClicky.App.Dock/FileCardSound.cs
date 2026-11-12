using System;
using HeyClicky.Drawing.Audio;

namespace HeyClicky.App.Dock;

public static class FileCardSound
{
	private static readonly object owner = new object();

	private static DateTimeOffset lastPlayedAt = DateTimeOffset.MinValue;

	private static DateTimeOffset lastFocusChangeAt = DateTimeOffset.MinValue;

	public static Func<bool> PlaysSounds { get; set; } = () => true;

	public static void NoteFocusChanged()
	{
		lastFocusChangeAt = DateTimeOffset.Now;
	}

	public static void PlayLift()
	{
		if (!PlaysSounds())
		{
			return;
		}
		DateTimeOffset now = DateTimeOffset.Now;
		if ((now - lastFocusChangeAt).TotalSeconds <= 0.45 || (now - lastPlayedAt).TotalSeconds <= 0.12)
		{
			return;
		}
		lastPlayedAt = now;
		try
		{
			ScreenDrawingSounds.Play("paper-place", 0.14f, 0.02, owner);
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "paper sound: " + ex.Message);
		}
	}
}
