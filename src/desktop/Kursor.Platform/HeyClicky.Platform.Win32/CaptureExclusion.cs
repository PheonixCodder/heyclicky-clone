using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Win32;

public static class CaptureExclusion
{
	private static readonly object cursorChromeGate = new object();

	private static readonly List<(nint Window, Action<Action> OnItsThread)> cursorChrome = new List<(nint, Action<Action>)>();

	private static bool showInScreenRecordings;

	private static int hiddenFromOwnCaptureCount;

	public static bool Enabled { get; set; } = true;

	public static Func<bool, Task<bool>>? SetHiddenFromOwnCapture { get; set; }

	private static bool CursorChromeExcluded
	{
		get
		{
			if (Enabled)
			{
				if (showInScreenRecordings)
				{
					return hiddenFromOwnCaptureCount > 0;
				}
				return true;
			}
			return false;
		}
	}

	public static bool ShowInScreenRecordings
	{
		get
		{
			lock (cursorChromeGate)
			{
				return showInScreenRecordings;
			}
		}
		set
		{
			lock (cursorChromeGate)
			{
				if (showInScreenRecordings == value)
				{
					return;
				}
				showInScreenRecordings = value;
			}
			ApplyToCursorChrome();
		}
	}

	public static bool Apply(nint hwnd)
	{
		if (!Enabled)
		{
			return true;
		}
		HWND hWnd = new HWND(hwnd);
		if ((bool)PInvoke.SetWindowDisplayAffinity(hWnd, WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE))
		{
			return true;
		}
		Log.Warn("CaptureExclusion", $"WDA_EXCLUDEFROMCAPTURE failed for 0x{hwnd:X} (error {Marshal.GetLastWin32Error()}); falling back to WDA_MONITOR");
		PInvoke.SetWindowDisplayAffinity(hWnd, WINDOW_DISPLAY_AFFINITY.WDA_MONITOR);
		return false;
	}

	public static void Remove(nint hwnd)
	{
		PInvoke.SetWindowDisplayAffinity(new HWND(hwnd), WINDOW_DISPLAY_AFFINITY.WDA_NONE);
	}

	public static void RegisterCursorChrome(nint hwnd, Action<Action> onItsThread)
	{
		lock (cursorChromeGate)
		{
			cursorChrome.Add((hwnd, onItsThread));
		}
		ApplyCursorChrome(hwnd);
	}

	public static void UnregisterCursorChrome(nint hwnd)
	{
		lock (cursorChromeGate)
		{
			cursorChrome.RemoveAll(((nint Window, Action<Action> OnItsThread) w) => w.Window == hwnd);
		}
	}

	internal static bool SetCursorChromeHiddenFromOwnCapture(bool hidden)
	{
		bool flag;
		lock (cursorChromeGate)
		{
			bool cursorChromeExcluded = CursorChromeExcluded;
			hiddenFromOwnCaptureCount = Math.Max(0, hiddenFromOwnCaptureCount + (hidden ? 1 : (-1)));
			flag = cursorChromeExcluded != CursorChromeExcluded && cursorChrome.Count > 0;
		}
		if (flag)
		{
			ApplyToCursorChrome();
		}
		return flag;
	}

	private static void ApplyToCursorChrome()
	{
		List<(nint, Action<Action>)> list;
		lock (cursorChromeGate)
		{
			list = cursorChrome.ToList();
		}
		foreach (var item in list)
		{
			var (window, _) = item;
			item.Item2(delegate
			{
				ApplyCursorChrome(window);
			});
		}
	}

	private static void ApplyCursorChrome(nint hwnd)
	{
		bool cursorChromeExcluded;
		lock (cursorChromeGate)
		{
			if (!cursorChrome.Any<(nint, Action<Action>)>(((nint Window, Action<Action> OnItsThread) w) => w.Window == hwnd))
			{
				return;
			}
			cursorChromeExcluded = CursorChromeExcluded;
		}
		if (cursorChromeExcluded)
		{
			Apply(hwnd);
		}
		else
		{
			Remove(hwnd);
		}
	}
}
