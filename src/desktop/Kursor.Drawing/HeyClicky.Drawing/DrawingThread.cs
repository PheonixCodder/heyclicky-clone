using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.Drawing.Interop;
using Windows.System;

namespace HeyClicky.Drawing;

internal sealed class DrawingThread : IDisposable
{
	private sealed class PlaybackClock
	{
		public bool IsPaused = true;

		public double Origin;

		public double Held;

		public double Now
		{
			get
			{
				if (!IsPaused)
				{
					return RealNow - Origin;
				}
				return Held;
			}
		}
	}

	private struct DispatcherQueueOptions
	{
		public int Size;

		public int ThreadType;

		public int ApartmentType;
	}

	private static readonly double SecondsPerTick = 1.0 / (double)Stopwatch.Frequency;

	[ThreadStatic]
	private static PlaybackClock? playbackClock;

	private readonly PlaybackClock? clock;

	private readonly Thread thread;

	private readonly ManualResetEventSlim started = new ManualResetEventSlim(initialState: false);

	private DispatcherQueueController? controller;

	private uint threadId;

	private nint waitableTimer;

	private volatile bool quitting;

	private readonly PriorityQueue<int, double> timerQueue = new PriorityQueue<int, double>();

	private readonly Dictionary<int, (double Due, Action Callback)> timers = new Dictionary<int, (double, Action)>();

	private int nextTimerToken = 1;

	private double armedDueTime = double.PositiveInfinity;

	public long TimerFires;

	public static double Now => playbackClock?.Now ?? RealNow;

	public static double RealNow => (double)Stopwatch.GetTimestamp() * SecondsPerTick;

	public static double PlaybackClockShift
	{
		get
		{
			PlaybackClock playbackClock = DrawingThread.playbackClock;
			if (playbackClock == null)
			{
				return 0.0;
			}
			return RealNow - playbackClock.Now;
		}
	}

	public DispatcherQueue Queue { get; private set; }

	public bool IsCurrent => Environment.CurrentManagedThreadId == thread.ManagedThreadId;

	public bool IsClockPaused => clock?.IsPaused ?? false;

	public DrawingThread(string name, bool playback = false)
	{
		if (playback)
		{
			clock = new PlaybackClock();
		}
		thread = new Thread(Run)
		{
			Name = name,
			IsBackground = true
		};
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		started.Wait();
	}

	private void Run()
	{
		NativeMethods.SetThreadDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
		playbackClock = clock;
		threadId = NativeMethods.GetCurrentThreadId();
		controller = CreateSystemDispatcherQueue();
		Queue = controller.DispatcherQueue;
		waitableTimer = NativeMethods.CreateWaitableTimerExW(IntPtr.Zero, IntPtr.Zero, 2u, 2031619u);
		if (waitableTimer == IntPtr.Zero)
		{
			waitableTimer = NativeMethods.CreateWaitableTimerExW(IntPtr.Zero, IntPtr.Zero, 0u, 2031619u);
		}
		started.Set();
		Pump();
		try
		{
			controller.ShutdownQueueAsync();
		}
		catch
		{
		}
		NativeMethods.CloseHandle(waitableTimer);
	}

	private static DispatcherQueueController CreateSystemDispatcherQueue()
	{
		DispatcherQueueOptions options = default(DispatcherQueueOptions);
		options.Size = Marshal.SizeOf<DispatcherQueueOptions>();
		options.ThreadType = 2;
		options.ApartmentType = 0;
		Marshal.ThrowExceptionForHR(CreateDispatcherQueueController(options, out var num));
		try
		{
			return DispatcherQueueController.FromAbi(num);
		}
		finally
		{
			Marshal.Release(num);
		}
	}

	[DllImport("CoreMessaging.dll")]
	private static extern int CreateDispatcherQueueController(DispatcherQueueOptions options, out nint controller);

	private unsafe void Pump()
	{
		nint num = waitableTimer;
		while (!quitting)
		{
			if (NativeMethods.MsgWaitForMultipleObjectsEx(1u, &num, uint.MaxValue, 1279u, 4u) == 0)
			{
				armedDueTime = double.PositiveInfinity;
				RunDueTimers();
			}
			NativeMethods.MSG message;
			while (NativeMethods.PeekMessageW(out message, IntPtr.Zero, 0u, 0u, 1u))
			{
				if (message.message == 18)
				{
					quitting = true;
					break;
				}
				NativeMethods.TranslateMessage(ref message);
				NativeMethods.DispatchMessageW(ref message);
			}
		}
	}

	public void Post(Action action)
	{
		if (!Queue.TryEnqueue(DispatcherQueuePriority.Normal, delegate
		{
			Guard(action);
		}))
		{
			Log.Warn("Drawing", "drawing thread queue refused work");
		}
	}

	public T Invoke<T>(Func<T> function)
	{
		if (IsCurrent)
		{
			return function();
		}
		T result = default(T);
		ExceptionDispatchInfo failure = null;
		ManualResetEventSlim done = new ManualResetEventSlim(initialState: false);
		try
		{
			if (!Queue.TryEnqueue(DispatcherQueuePriority.Normal, delegate
			{
				try
				{
					result = function();
				}
				catch (Exception source)
				{
					failure = ExceptionDispatchInfo.Capture(source);
				}
				finally
				{
					done.Set();
				}
			}))
			{
				throw new InvalidOperationException("the drawing thread has stopped");
			}
			if (!done.Wait(5000))
		{
			throw new TimeoutException("drawing thread invoke timed out");
		}
			failure?.Throw();
			return result;
		}
		finally
		{
			if (done != null)
			{
				((IDisposable)done).Dispose();
			}
		}
	}

	public void Invoke(Action action)
	{
		Invoke(delegate
		{
			action();
			return true;
		});
	}

	private static void Guard(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Log.Error("Drawing", "drawing thread work failed", ex);
		}
	}

	public int ScheduleTimer(double dueTime, Action callback)
	{
		int num = nextTimerToken++;
		timers[num] = (dueTime, callback);
		timerQueue.Enqueue(num, dueTime);
		ArmWaitableTimer();
		return num;
	}

	public void CancelTimer(int token)
	{
		timers.Remove(token);
	}

	public void CancelAllTimers()
	{
		timers.Clear();
		timerQueue.Clear();
		armedDueTime = double.PositiveInfinity;
		NativeMethods.CancelWaitableTimer(waitableTimer);
	}

	private void RunDueTimers()
	{
		PlaybackClock playbackClock = clock;
		if (playbackClock != null && playbackClock.IsPaused)
		{
			return;
		}
		int element;
		double priority;
		while (timerQueue.TryPeek(out element, out priority))
		{
			if (!timers.TryGetValue(element, out (double, Action) value) || value.Item1 != priority)
			{
				timerQueue.Dequeue();
				continue;
			}
			if (priority > Now + 0.0005)
			{
				break;
			}
			timerQueue.Dequeue();
			timers.Remove(element);
			TimerFires++;
			Guard(value.Item2);
		}
		ArmWaitableTimer();
	}

	private void ArmWaitableTimer()
	{
		PlaybackClock playbackClock = clock;
		if (playbackClock != null && playbackClock.IsPaused)
		{
			if (!double.IsPositiveInfinity(armedDueTime))
			{
				NativeMethods.CancelWaitableTimer(waitableTimer);
				armedDueTime = double.PositiveInfinity;
			}
			return;
		}
		int element;
		double priority;
		while (timerQueue.TryPeek(out element, out priority) && !timers.ContainsKey(element))
		{
			timerQueue.Dequeue();
		}
		if (!timerQueue.TryPeek(out var _, out var priority2))
		{
			if (!double.IsPositiveInfinity(armedDueTime))
			{
				NativeMethods.CancelWaitableTimer(waitableTimer);
				armedDueTime = double.PositiveInfinity;
			}
		}
		else if (!(priority2 >= armedDueTime - 0.0001) || double.IsPositiveInfinity(armedDueTime))
		{
			armedDueTime = priority2;
			long dueTime = -Math.Max(1L, (long)((priority2 - Now) * 10000000.0));
			NativeMethods.SetWaitableTimerEx(waitableTimer, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0u);
		}
	}

	public void PauseClock()
	{
		if (clock != null && !clock.IsPaused)
		{
			clock.Held = clock.Now;
			clock.IsPaused = true;
			ArmWaitableTimer();
		}
	}

	public void ResumeClock()
	{
		if (clock != null && clock.IsPaused)
		{
			clock.Origin = RealNow - clock.Held;
			clock.IsPaused = false;
			ArmWaitableTimer();
		}
	}

	public void JumpClock(double time)
	{
		if (clock == null)
		{
			return;
		}
		bool isPaused = clock.IsPaused;
		clock.Held = clock.Now;
		clock.IsPaused = true;
		int element;
		double priority;
		while (timerQueue.TryPeek(out element, out priority))
		{
			if (!timers.TryGetValue(element, out (double, Action) value) || value.Item1 != priority)
			{
				timerQueue.Dequeue();
				continue;
			}
			if (priority > time)
			{
				break;
			}
			timerQueue.Dequeue();
			timers.Remove(element);
			clock.Held = Math.Max(clock.Held, priority);
			TimerFires++;
			Guard(value.Item2);
		}
		clock.Held = time;
		if (!isPaused)
		{
			clock.Origin = RealNow - time;
			clock.IsPaused = false;
		}
		ArmWaitableTimer();
	}

	public void ResetClock()
	{
		CancelAllTimers();
		if (clock != null)
		{
			clock.Held = 0.0;
			clock.IsPaused = true;
		}
	}

	public void Dispose()
	{
		if (!quitting)
		{
			quitting = true;
			NativeMethods.PostThreadMessageW(threadId, 18u, IntPtr.Zero, IntPtr.Zero);
			thread.Join(2000);
		}
	}
}
