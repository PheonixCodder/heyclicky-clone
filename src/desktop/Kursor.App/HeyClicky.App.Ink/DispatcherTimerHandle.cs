using System;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Ink;

internal sealed class DispatcherTimerHandle
{
	private DispatcherQueueTimer? timer;

	public static DispatcherTimerHandle After(DispatcherQueue queue, double seconds, Action action)
	{
		DispatcherTimerHandle handle = new DispatcherTimerHandle();
		DispatcherQueueTimer? dispatcherQueueTimer = (handle.timer = queue.CreateTimer());
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(seconds);
		dispatcherQueueTimer.Tick += delegate
		{
			handle.timer = null;
			action();
		};
		dispatcherQueueTimer.Start();
		return handle;
	}

	public void Cancel()
	{
		timer?.Stop();
		timer = null;
	}
}
