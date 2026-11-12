using System.Collections.Generic;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App;

public static class TimerRoots
{
	private static readonly HashSet<DispatcherQueueTimer> live = new HashSet<DispatcherQueueTimer>();

	public static DispatcherQueueTimer Rooted(this DispatcherQueueTimer timer)
	{
		lock (live)
		{
			live.Add(timer);
		}
		timer.Tick += delegate(DispatcherQueueTimer t, object _)
		{
			if (!t.IsRepeating)
			{
				lock (live)
				{
					live.Remove(t);
				}
			}
		};
		return timer;
	}

	public static void Release(DispatcherQueueTimer timer)
	{
		timer.Stop();
		lock (live)
		{
			live.Remove(timer);
		}
	}
}
