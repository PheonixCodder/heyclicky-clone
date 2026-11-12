using System;
using Windows.UI.ViewManagement;

namespace HeyClicky.App.UI;

public static class MotionPreference
{
	private static readonly UISettings? Settings;

	private static volatile bool reduced;

	private static readonly bool Forced;

	public static bool Reduced
	{
		get
		{
			if (!Forced)
			{
				return reduced;
			}
			return true;
		}
	}

	static MotionPreference()
	{
		Forced = Environment.GetEnvironmentVariable("HEYCLICKY_REDUCE_MOTION") == "1";
		try
		{
			Settings = new UISettings();
			reduced = !Settings.AnimationsEnabled;
			try
			{
				Settings.AnimationsEnabledChanged += delegate
				{
					Refresh();
				};
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Log.Warn("motion", "UISettings unavailable: " + ex.Message);
		}
		if (reduced)
		{
			Log.Info("motion", "Windows animation effects are off: reduced motion");
		}
	}

	public static void Refresh()
	{
		try
		{
			if ((object)Settings != null)
			{
				bool flag = !Settings.AnimationsEnabled;
				if (flag != reduced)
				{
					reduced = flag;
					Log.Info("motion", flag ? "animation effects turned off: reduced motion" : "animation effects turned on");
				}
			}
		}
		catch
		{
		}
	}
}
