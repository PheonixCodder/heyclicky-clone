using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Dock;

internal static class DockTimer
{
	private static readonly HashSet<DispatcherQueueTimer> live = new HashSet<DispatcherQueueTimer>();

	public static DispatcherQueueTimer After(double seconds, Action action)
	{
		DispatcherQueueTimer t = DispatcherQueue.GetForCurrentThread().CreateTimer();
		t.Interval = TimeSpan.FromSeconds(Math.Max(0.0, seconds));
		t.IsRepeating = false;
		t.Tick += delegate
		{
			live.Remove(t);
			action();
		};
		live.Add(t);
		t.Start();
		return t;
	}
}
