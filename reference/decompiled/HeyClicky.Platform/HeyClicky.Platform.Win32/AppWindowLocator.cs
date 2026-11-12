using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeyClicky.Platform.Win32;

public static class AppWindowLocator
{
	private struct NativeRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private delegate bool EnumWindowsProc(nint window, nint parameter);

	public static RectD? FrontWindowFrame(string processName)
	{
		if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			string text = processName;
			processName = text.Substring(0, text.Length - 4);
		}
		HashSet<int> hashSet = new HashSet<int>();
		Process[] processesByName = Process.GetProcessesByName(processName);
		foreach (Process process in processesByName)
		{
			hashSet.Add(process.Id);
			process.Dispose();
		}
		return FrontWindowFrame(hashSet);
	}

	public static RectD? FrontWindowFrame(IReadOnlySet<int> processIds)
	{
		if (processIds.Count == 0)
		{
			return null;
		}
		RectD? found = null;
		EnumWindows(delegate(nint window, nint _)
		{
			GetWindowThreadProcessId(window, out var processId);
			if (!processIds.Contains((int)processId) || !IsNormalVisibleWindow(window))
			{
				return true;
			}
			RectD? rectD = FrameOf(window);
			if (rectD.HasValue)
			{
				RectD valueOrDefault = rectD.GetValueOrDefault();
				if (!(valueOrDefault.Width < 300.0) && !(valueOrDefault.Height < 200.0))
				{
					found = valueOrDefault;
					return false;
				}
			}
			return true;
		}, 0);
		return found;
	}

	public static DisplayInfo? FrontmostAppDisplay(IReadOnlyList<DisplayInfo>? displays = null)
	{
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == 0)
		{
			return null;
		}
		GetWindowThreadProcessId(foregroundWindow, out var processId);
		if (processId == (uint)Environment.ProcessId)
		{
			return null;
		}
		RectD? rectD = FrontWindowFrame(new HashSet<int> { (int)processId });
		if (rectD.HasValue)
		{
			RectD valueOrDefault = rectD.GetValueOrDefault();
			return Display(valueOrDefault, displays);
		}
		return null;
	}

	public static DisplayInfo? Display(RectD mostlyShowing, IReadOnlyList<DisplayInfo>? displays = null)
	{
		if (displays == null)
		{
			displays = DisplayEnumerator.GetDisplays();
		}
		int? num = IndexOfFrame(mostlyShowing, displays.Select((DisplayInfo d) => d.BoundsPx).ToList());
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			return displays[valueOrDefault];
		}
		return null;
	}

	public static int? IndexOfFrame(RectD window, IReadOnlyList<RectD> frames)
	{
		(int, double)? tuple = null;
		for (int i = 0; i < frames.Count; i++)
		{
			RectD rectD = frames[i];
			double num = Math.Min(rectD.Right, window.Right) - Math.Max(rectD.X, window.X);
			double num2 = Math.Min(rectD.Bottom, window.Bottom) - Math.Max(rectD.Y, window.Y);
			double num3 = ((num > 0.0 && num2 > 0.0) ? (num * num2) : 0.0);
			if (num3 > 0.0 && num3 > (tuple?.Item2 ?? 0.0))
			{
				tuple = (i, num3);
			}
		}
		return tuple?.Item1;
	}

	private static bool IsNormalVisibleWindow(nint window)
	{
		if (!IsWindowVisible(window) || IsIconic(window))
		{
			return false;
		}
		long windowLongPtrW = GetWindowLongPtrW(window, -20);
		if ((windowLongPtrW & 0x80) != 0L || (windowLongPtrW & 8) != 0L)
		{
			return false;
		}
		if (DwmGetWindowAttribute(window, 14, out int value, 4) == 0 && value != 0)
		{
			return false;
		}
		if ((windowLongPtrW & 0x80000) != 0L && GetLayeredWindowAttributes(window, out var _, out var alpha, out var flags) && (flags & 2) != 0 && alpha <= 13)
		{
			return false;
		}
		return true;
	}

	private static RectD? FrameOf(nint window)
	{
		if (DwmGetWindowAttribute(window, 9, out NativeRect value, Marshal.SizeOf<NativeRect>()) != 0 && !GetWindowRect(window, out value))
		{
			return null;
		}
		return new RectD(value.Left, value.Top, value.Right - value.Left, value.Bottom - value.Top);
	}

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint window);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint window);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern long GetWindowLongPtrW(nint window, int index);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint window, out NativeRect rect);

	[DllImport("user32.dll")]
	private static extern bool GetLayeredWindowAttributes(nint window, out uint key, out byte alpha, out uint flags);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint window, int attribute, out NativeRect value, int size);
}
