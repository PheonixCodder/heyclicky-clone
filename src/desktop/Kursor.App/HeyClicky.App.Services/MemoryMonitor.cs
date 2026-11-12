using System;
using System.Threading;
using HeyClicky.Drawing3D;
using HeyClicky.Platform.Input;

namespace HeyClicky.App.Services;

public static class MemoryMonitor
{
	private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1L);

	private static Timer? timer;

	private static int hookReinstallsAtLastSample;

	public static void Start()
	{
		if (timer == null)
		{
			timer = new Timer(delegate
			{
				Sample();
			}, null, Interval, Interval);
			Sample();
		}
	}

	private static void Sample()
	{
		try
		{
			(int Stages, int Renderers) liveCounts = ThreeStage.LiveCounts;
			int item = liveCounts.Stages;
			int item2 = liveCounts.Renderers;
			int hookReinstallCount = ShortcutMonitor.HookReinstallCount;
			int value = hookReinstallCount - hookReinstallsAtLastSample;
			hookReinstallsAtLastSample = hookReinstallCount;
			Log.Info("memory", $"{MemorySample.Take()}, 3D stages {item} (renderers {item2}), keyboard hook reinstalls {value}/min");
		}
		catch (Exception ex)
		{
			Log.Warn("memory", "sample failed: " + ex.Message);
		}
	}
}
