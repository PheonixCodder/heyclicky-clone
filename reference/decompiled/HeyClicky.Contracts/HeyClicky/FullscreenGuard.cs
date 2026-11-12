using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace HeyClicky;

public static class FullscreenGuard
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	private static readonly nint Topmost = -1;

	private static nint lastGame;

	private const int GWL_EXSTYLE = -20;

	private const long WS_EX_TOPMOST = 8L;

	public static nint TestGameWindow { get; set; }

	public static nint GameWindow()
	{
		nint num = Find();
		if (Interlocked.Exchange(ref lastGame, num) != num)
		{
			Log.Info("fullscreen", (num != 0) ? (Describe(num) + " owns its screen (" + NotificationState() + "): HeyClicky's windows stay under it so it keeps fullscreen; the voice still works") : "no fullscreen game in front: HeyClicky's windows go back on top");
		}
		return num;
	}

	public static nint TopmostInsertAfter()
	{
		nint num = GameWindow();
		if (num == 0)
		{
			return Topmost;
		}
		return num;
	}

	private static nint Find()
	{
		nint testGameWindow = TestGameWindow;
		if (testGameWindow != 0)
		{
			if (!IsWindowVisible(testGameWindow))
			{
				return 0;
			}
			return testGameWindow;
		}
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == 0 || !IsWindowVisible(foregroundWindow) || IsIconic(foregroundWindow))
		{
			return 0;
		}
		GetWindowThreadProcessId(foregroundWindow, out var pid);
		if (pid == Environment.ProcessId)
		{
			return 0;
		}
		if ((GetWindowLongPtr(foregroundWindow, -20) & 8) == 0L)
		{
			return 0;
		}
		if (!GetWindowRect(foregroundWindow, out var rect))
		{
			return 0;
		}
		MONITORINFO mONITORINFO = default(MONITORINFO);
		mONITORINFO.cbSize = Marshal.SizeOf<MONITORINFO>();
		MONITORINFO info = mONITORINFO;
		if (!GetMonitorInfo(MonitorFromWindow(foregroundWindow, 2u), ref info))
		{
			return 0;
		}
		RECT rcMonitor = info.rcMonitor;
		if (rect.Left > rcMonitor.Left || rect.Top > rcMonitor.Top || rect.Right < rcMonitor.Right || rect.Bottom < rcMonitor.Bottom)
		{
			return 0;
		}
		return foregroundWindow;
	}

	public static string Describe(nint hwnd)
	{
		char[] array = new char[96];
		int className = GetClassName(hwnd, array, array.Length);
		return ProcessName(hwnd) + " [" + new string(array, 0, Math.Max(0, className)) + "]";
	}

	public static string ProcessName(nint hwnd)
	{
		GetWindowThreadProcessId(hwnd, out var pid);
		try
		{
			return Process.GetProcessById((int)pid).ProcessName;
		}
		catch
		{
			return pid.ToString();
		}
	}

	public static string NotificationState()
	{
		int state = 0;
		try
		{
			if (SHQueryUserNotificationState(out state) != 0)
			{
				state = 0;
			}
		}
		catch
		{
		}
		return state switch
		{
			2 => "shell: busy", 
			3 => "shell: D3D fullscreen", 
			4 => "shell: presentation", 
			5 => "shell: accepts notifications", 
			6 => "shell: quiet time", 
			7 => "shell: immersive app", 
			_ => "shell: unknown", 
		};
	}

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint hwnd);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern long GetWindowLongPtr(nint hwnd, int index);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hwnd, out RECT rect);

	[DllImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint hwnd, uint flags);

	[DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
	private static extern bool GetMonitorInfo(nint monitor, ref MONITORINFO info);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(nint hwnd, char[] name, int max);

	[DllImport("shell32.dll")]
	private static extern int SHQueryUserNotificationState(out int state);
}
