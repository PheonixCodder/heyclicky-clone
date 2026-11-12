using System;
using System.Diagnostics;
using System.Threading;

namespace HeyClicky.Platform.Overlay;

internal sealed class FramePacer
{
	private bool compositorClockAvailable = true;

	private int fastReturns;

	private long fallbackUntilTimestamp;

	private readonly nint highResolutionTimer = NativeComposition.CreateWaitableTimerExW(0, null, 2u, 2031619u);

	public long FallbackWaits;

	public unsafe void WaitForNextFrame()
	{
		long timestamp = Stopwatch.GetTimestamp();
		if (compositorClockAvailable && timestamp >= fallbackUntilTimestamp)
		{
			try
			{
				NativeComposition.DCompositionWaitForCompositorClock(0u, null, 50u);
				if (Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds >= 0.5)
				{
					fastReturns = 0;
					return;
				}
				if (++fastReturns < 3)
				{
					return;
				}
				fastReturns = 0;
				fallbackUntilTimestamp = timestamp + Stopwatch.Frequency * 2;
			}
			catch (EntryPointNotFoundException)
			{
				compositorClockAvailable = false;
			}
		}
		FallbackWaits++;
		if (highResolutionTimer != 0)
		{
			long num = -166667L;
			if (NativeComposition.SetWaitableTimerEx(highResolutionTimer, &num, 0, 0, 0, null, 0u))
			{
				NativeComposition.WaitForSingleObject(highResolutionTimer, 100u);
				return;
			}
		}
		Thread.Sleep(16);
	}
}
