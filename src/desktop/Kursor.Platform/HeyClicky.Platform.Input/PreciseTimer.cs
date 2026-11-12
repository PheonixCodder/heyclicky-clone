using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Platform.Input;

internal sealed class PreciseTimer : IDisposable
{
	private readonly nint handle = CreateWaitableTimerExW(0, null, 2u, 2031619u);

	public static Task DelayAsync(int milliseconds)
	{
		if (milliseconds <= 0)
		{
			return Task.CompletedTask;
		}
		if (milliseconds >= 40)
		{
			return Task.Delay(milliseconds);
		}
		return Task.Run(delegate
		{
			using PreciseTimer preciseTimer = new PreciseTimer();
			preciseTimer.Sleep(milliseconds);
		});
	}

	public void Sleep(double milliseconds)
	{
		if (milliseconds <= 0.0)
		{
			return;
		}
		if (handle == 0)
		{
			Thread.Sleep((int)Math.Ceiling(milliseconds));
			return;
		}
		long dueTime = -(long)(milliseconds * 10000.0);
		if (SetWaitableTimerEx(handle, ref dueTime, 0, 0, 0, 0, 0u))
		{
			WaitForSingleObject(handle, uint.MaxValue);
		}
		else
		{
			Thread.Sleep((int)Math.Ceiling(milliseconds));
		}
	}

	public void WaitUntil(Stopwatch clock, double seconds)
	{
		double num = seconds * 1000.0 - clock.Elapsed.TotalMilliseconds;
		if (num > 0.2)
		{
			Sleep(num);
		}
	}

	public void Dispose()
	{
		if (handle != 0)
		{
			CloseHandle(handle);
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern nint CreateWaitableTimerExW(nint attributes, string? name, uint flags, uint access);

	[DllImport("kernel32.dll")]
	private static extern bool SetWaitableTimerEx(nint timer, ref long dueTime, int period, nint completion, nint argument, nint wakeContext, uint tolerableDelay);

	[DllImport("kernel32.dll")]
	private static extern uint WaitForSingleObject(nint handle, uint milliseconds);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(nint handle);
}
