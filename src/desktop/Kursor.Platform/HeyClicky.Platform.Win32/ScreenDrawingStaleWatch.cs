using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.Platform.Input;

namespace HeyClicky.Platform.Win32;

public sealed class ScreenDrawingStaleWatch : IDisposable, ILowLevelInputSink, IThreadPoolWorkItem
{
	private struct NativeRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;

		public readonly RectD ToRectD()
		{
			return new RectD(Left, Top, Right - Left, Bottom - Top);
		}
	}

	private struct NativePoint
	{
		public int X;

		public int Y;
	}

	private delegate bool EnumWindowsCallback(nint window, nint parameter);

	private const double FrontWindowMoveTolerancePoints = 4.0;

	private const int PollIntervalMilliseconds = 250;

	private const int WindowPollEveryNthTick = 2;

	private const double SmallestWatchedWidthPoints = 160.0;

	private const double SmallestWatchedHeightPoints = 120.0;

	private const double CoveringWindowAreaShare = 0.5;

	private readonly RectD displayBoundsPx;

	private readonly double displayScale;

	private readonly Action<string> onChanged;

	private readonly Timer pollTimer;

	private readonly int ownProcessId = Environment.ProcessId;

	private int baselineProcessId;

	private nint watchedWindow;

	private RectD watchedWindowBounds;

	private int pollTicks;

	private int hasFired;

	private int disposed;

	private long pendingPointerEvent;

	private int pointerWorkQueued;

	private const int GwlExStyle = -20;

	private const long WsExToolWindow = 128L;

	private const uint GaRoot = 2u;

	private const int DwmwaCloaked = 14;

	private static readonly HashSet<string> ScreenCaptureProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ScreenClippingHost", "SnippingTool", "ScreenSketch" };

	private readonly object captureProcessGate = new object();

	private int lastCheckedCaptureProcessId;

	private bool lastCheckedCaptureProcessIsCapture;

	public ScreenDrawingStaleWatch(DisplayInfo display, Action<string> onChanged)
	{
		displayBoundsPx = display.BoundsPx;
		displayScale = Math.Max(0.5, display.Scale);
		this.onChanged = onChanged;
		int num = ProcessOf(GetForegroundWindow());
		if (num == ownProcessId)
		{
			(nint, RectD)? tuple = FrontWindowOf((int processId) => processId != ownProcessId);
			num = (tuple.HasValue ? ProcessOf(tuple.GetValueOrDefault().Item1) : 0);
		}
		if (num != 0)
		{
			AdoptBaseline(num);
		}
		LowLevelInputHub.Shared.AddSink(this);
		LowLevelInputHub.Shared.Acquire();
		pollTimer = new Timer(delegate
		{
			Poll();
		}, null, 250, 250);
	}

	private void AdoptBaseline(int processId)
	{
		baselineProcessId = processId;
		(nint, RectD)? tuple = FrontWindowOf(processId);
		if (tuple.HasValue)
		{
			(watchedWindow, watchedWindowBounds) = tuple.GetValueOrDefault();
		}
	}

	private void Fire(string reason)
	{
		if (ClickyDriveState.IsDrivingNow || Interlocked.Exchange(ref hasFired, 1) == 1)
		{
			return;
		}
		Dispose();
		try
		{
			onChanged(reason);
		}
		catch (Exception ex)
		{
			Log.Warn("StaleWatch", "handler failed: " + ex.Message);
		}
	}

	private void Poll()
	{
		if (Volatile.Read(in hasFired) == 1 || Volatile.Read(in disposed) == 1)
		{
			return;
		}
		try
		{
			int num = ProcessOf(GetForegroundWindow());
			if (num != 0 && num != ownProcessId && !IsScreenCaptureProcess(num))
			{
				if (baselineProcessId == 0)
				{
					AdoptBaseline(num);
				}
				else if (num != baselineProcessId)
				{
					Log.Info("StaleWatch", $"another app came to the front (pid {num})");
					Fire("app switch");
					return;
				}
			}
			if (++pollTicks % 2 == 0)
			{
				string text = WatchedWindowChangeReason();
				if (text != null)
				{
					Fire(text);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("StaleWatch", "poll failed: " + ex.Message);
		}
	}

	private string? WatchedWindowChangeReason()
	{
		if (watchedWindow == 0)
		{
			return null;
		}
		if (!IsWindow(watchedWindow) || !IsWindowVisible(watchedWindow) || IsCloaked(watchedWindow))
		{
			return "window closed";
		}
		if (!GetWindowRect(watchedWindow, out var rect))
		{
			return null;
		}
		RectD rectD = rect.ToRectD();
		double num = 4.0 * displayScale;
		if (Math.Abs(rectD.X - watchedWindowBounds.X) > num || Math.Abs(rectD.Y - watchedWindowBounds.Y) > num || Math.Abs(rectD.Width - watchedWindowBounds.Width) > num || Math.Abs(rectD.Height - watchedWindowBounds.Height) > num)
		{
			return "window moved";
		}
		(nint, RectD)? tuple = FrontWindowOf(baselineProcessId);
		if (tuple.HasValue)
		{
			(nint, RectD) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item1 != watchedWindow)
			{
				double num2 = watchedWindowBounds.Width * watchedWindowBounds.Height;
				double num3 = OverlapArea(valueOrDefault.Item2, watchedWindowBounds);
				if (valueOrDefault.Item2.Width * valueOrDefault.Item2.Height >= num2 * 0.5 && num3 >= num2 * 0.5)
				{
					return "another window in front";
				}
			}
		}
		return null;
	}

	private (nint Handle, RectD Bounds)? FrontWindowOf(int processId)
	{
		if (processId != 0)
		{
			return FrontWindowOf((int candidate) => candidate == processId);
		}
		return null;
	}

	private (nint Handle, RectD Bounds)? FrontWindowOf(Func<int, bool> isWantedProcess)
	{
		(nint, RectD)? found = null;
		double minimumWidth = 160.0 * displayScale;
		double minimumHeight = 120.0 * displayScale;
		EnumWindows(delegate(nint window, nint _)
		{
			if (!isWantedProcess(ProcessOf(window)) || !IsWindowVisible(window) || IsCloaked(window))
			{
				return true;
			}
			if ((GetWindowLongPtr(window, -20) & 0x80) != 0L)
			{
				return true;
			}
			if (!GetWindowRect(window, out var rect))
			{
				return true;
			}
			RectD rectD = rect.ToRectD();
			if (rectD.Width < minimumWidth || rectD.Height < minimumHeight)
			{
				return true;
			}
			if (OverlapArea(rectD, displayBoundsPx) < rectD.Width * rectD.Height * 0.5)
			{
				return true;
			}
			found = (window, rectD);
			return false;
		}, 0);
		return found;
	}

	private static double OverlapArea(RectD first, RectD second)
	{
		double num = Math.Min(first.Right, second.Right) - Math.Max(first.X, second.X);
		double num2 = Math.Min(first.Bottom, second.Bottom) - Math.Max(first.Y, second.Y);
		if (!(num > 0.0) || !(num2 > 0.0))
		{
			return 0.0;
		}
		return num * num2;
	}

	void ILowLevelInputSink.OnKey(in LowLevelKeyEvent keyEvent)
	{
	}

	void ILowLevelInputSink.OnMouseButton(LowLevelMouseButtonKind kind, int x, int y, uint timeMs)
	{
		if (Volatile.Read(in hasFired) != 1 && !((double)x < displayBoundsPx.X) && !((double)x >= displayBoundsPx.Right) && !((double)y < displayBoundsPx.Y) && !((double)y >= displayBoundsPx.Bottom))
		{
			long num = x - (long)displayBoundsPx.X;
			long num2 = y - (long)displayBoundsPx.Y;
			long value = ((long)((kind != LowLevelMouseButtonKind.Wheel) ? 1 : 2) << 62) | (num << 31) | num2;
			Interlocked.Exchange(ref pendingPointerEvent, value);
			if (Interlocked.Exchange(ref pointerWorkQueued, 1) == 0)
			{
				ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
			}
		}
	}

	void IThreadPoolWorkItem.Execute()
	{
		Volatile.Write(ref pointerWorkQueued, 0);
		long num = Interlocked.Exchange(ref pendingPointerEvent, 0L);
		if (num != 0L && Volatile.Read(in hasFired) != 1)
		{
			bool flag = num >> 62 == 2;
			int x = (int)(((num >> 31) & 0x7FFFFFFF) + (long)displayBoundsPx.X);
			int y = (int)((num & 0x7FFFFFFF) + (long)displayBoundsPx.Y);
			NativePoint point = default(NativePoint);
			point.X = x;
			point.Y = y;
			nint ancestor = GetAncestor(WindowFromPoint(point), 2u);
			int num2 = ((ancestor != 0) ? ProcessOf(ancestor) : 0);
			if ((num2 != ownProcessId || ancestor == 0) && (num2 == 0 || !IsScreenCaptureProcess(num2)))
			{
				Fire(flag ? "scroll" : "click");
			}
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) != 1)
		{
			pollTimer?.Dispose();
			LowLevelInputHub.Shared.RemoveSink(this);
			LowLevelInputHub.Shared.Release();
		}
	}

	private bool IsScreenCaptureProcess(int processId)
	{
		lock (captureProcessGate)
		{
			if (processId == lastCheckedCaptureProcessId)
			{
				return lastCheckedCaptureProcessIsCapture;
			}
			bool result;
			try
			{
				using Process process = Process.GetProcessById(processId);
				result = ScreenCaptureProcessNames.Contains(process.ProcessName);
			}
			catch
			{
				result = false;
			}
			lastCheckedCaptureProcessId = processId;
			lastCheckedCaptureProcessIsCapture = result;
			return result;
		}
	}

	private static int ProcessOf(nint window)
	{
		if (window == 0)
		{
			return 0;
		}
		GetWindowThreadProcessId(window, out var processId);
		return (int)processId;
	}

	private static bool IsCloaked(nint window)
	{
		if (DwmGetWindowAttribute(window, 14, out var value, 4) == 0)
		{
			return value != 0;
		}
		return false;
	}

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsCallback callback, nint parameter);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(nint window);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint window);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint window, out NativeRect rect);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern long GetWindowLongPtr(nint window, int index);

	[DllImport("user32.dll")]
	private static extern nint WindowFromPoint(NativePoint point);

	[DllImport("user32.dll")]
	private static extern nint GetAncestor(nint window, uint flags);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}
