using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Services;

public static class MainThreadStallMonitor
{
	private sealed record StallWork(object? Work, int RunningMs, long CallbacksSincePing);

	private const int PingIntervalMs = 50;

	private const int StallThresholdMs = 100;

	private const int InProgressReportMs = 1000;

	private const int HangReportMs = 2000;

	private const int UnendedHangReportMs = 15000;

	private const int AlwaysLoggedMs = 500;

	private static Thread? thread;

	private static DispatcherQueue? queue;

	private static long outstandingSentTicks;

	private static bool reportedOutstanding;

	private static bool reportedHang;

	private static StallWork? stallWork;

	private static long callbacksAtPing;

	public static int StallCount;

	public static int LongestStallMs;

	[DllImport("kernel32.dll")]
	private static extern bool QueryUnbiasedInterruptTime(out long unbiasedTime);

	private static long UnbiasedNow()
	{
		if (!QueryUnbiasedInterruptTime(out var unbiasedTime))
		{
			return Stopwatch.GetTimestamp() * 10000000 / Stopwatch.Frequency;
		}
		return unbiasedTime;
	}

	private static int MillisecondsSince(long unbiasedTicks)
	{
		return (int)Math.Min(2147483647L, (UnbiasedNow() - unbiasedTicks) / 10000);
	}

	private static void ReportHang(int milliseconds, bool ended, StallWork? work)
	{
		if (!Debugger.IsAttached)
		{
			string value = ((!ended) ? "unended" : ((milliseconds < 5000) ? "2-5s" : ((milliseconds < 15000) ? "5-15s" : "15s+")));
			string text = (((object)work == null) ? "unknown" : UiWork.Describe(work.Work));
			string value2 = ((work?.Work == null) ? "" : $", running {work.RunningMs} ms at the 1 s mark");
			string value3 = (((object)work == null) ? "" : $", {work.CallbacksSincePing} UI callbacks started during the stall");
			ClickyAnalytics.ReportInternalError("main_thread", "app_hanging", null, $"UI thread blocked {(ended ? "" : "for at least ")}{milliseconds} ms ({value}), {Program.MillisecondsSinceProcessStart() / 1000.0:0} s after launch; UI work: {text}{value2}{value3}", CrashReportLevel.Warning, text, new Dictionary<string, string>
			{
				["hang_duration"] = value,
				["ui_work"] = text
			});
		}
	}

	public static void Start(DispatcherQueue uiQueue)
	{
		if (thread == null)
		{
			queue = uiQueue;
			thread = new Thread(Run)
			{
				IsBackground = true,
				Name = "HeyClicky UI stall watchdog",
				Priority = ThreadPriority.BelowNormal
			};
			thread.Start();
			Log.Info("stall", $"UI-thread stall watchdog armed (threshold {100} ms, hang report at {2000} ms)");
		}
	}

	private static void Run()
	{
		while (true)
		{
			Thread.Sleep(50);
			try
			{
				Tick();
			}
			catch (Exception ex)
			{
				Log.Warn("stall", "watchdog tick: " + ex.Message);
			}
		}
	}

	private static void Tick()
	{
		long num = Interlocked.Read(in outstandingSentTicks);
		if (num != 0L)
		{
			int num2 = MillisecondsSince(num);
			if (num2 >= 1000 && !reportedOutstanding)
			{
				reportedOutstanding = true;
				(object? Work, int RunningMs) tuple = UiWork.Snapshot();
				object item = tuple.Work;
				int item2 = tuple.RunningMs;
				StallWork stallWork = new StallWork(item, item2, UiWork.Started - Interlocked.Read(in callbacksAtPing));
				Volatile.Write(ref MainThreadStallMonitor.stallWork, stallWork);
				Log.Info("stall", $"[stall] UI thread unresponsive for {num2} ms and counting (ping sent {Stamp(DateTime.Now.AddMilliseconds(-num2))}; running {UiWork.Describe(item)}{((item == null) ? "" : $" for {item2} ms")}, {stallWork.CallbacksSincePing} UI callbacks since the ping)");
			}
			if (num2 >= 15000 && !Volatile.Read(in reportedHang))
			{
				Volatile.Write(ref reportedHang, value: true);
				ReportHang(num2, ended: false, Volatile.Read(in MainThreadStallMonitor.stallWork));
			}
		}
		else
		{
			long sentAt = UnbiasedNow();
			DateTime sentWall = DateTime.Now;
			Interlocked.Exchange(ref outstandingSentTicks, sentAt);
			reportedOutstanding = false;
			Volatile.Write(ref reportedHang, value: false);
			Volatile.Write(ref MainThreadStallMonitor.stallWork, null);
			Interlocked.Exchange(ref callbacksAtPing, UiWork.Started);
			DispatcherQueue? dispatcherQueue = queue;
			if ((object)dispatcherQueue == null || !dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, delegate
			{
				int num3 = MillisecondsSince(sentAt);
				StallWork work = Volatile.Read(in MainThreadStallMonitor.stallWork);
				Interlocked.Exchange(ref outstandingSentTicks, 0L);
				if (num3 >= 100)
				{
					if (num3 >= 2000 && !Volatile.Read(in reportedHang))
					{
						Volatile.Write(ref reportedHang, value: true);
						ReportHang(num3, ended: true, work);
					}
					Interlocked.Increment(ref StallCount);
					if (num3 > LongestStallMs)
					{
						LongestStallMs = num3;
					}
					if (num3 >= 500)
					{
						Log.Info("stall", $"[stall] UI thread blocked {num3} ms ({Stamp(sentWall)} → {Stamp(DateTime.Now)})");
					}
				}
			}))
			{
				Interlocked.Exchange(ref outstandingSentTicks, 0L);
			}
		}
	}

	private static string Stamp(DateTime time)
	{
		return time.ToString("HH:mm:ss.fff");
	}
}
