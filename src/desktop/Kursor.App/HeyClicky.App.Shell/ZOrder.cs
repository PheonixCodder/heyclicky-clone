using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using HeyClicky.App.Interop;

namespace HeyClicky.App.Shell;

public static class ZOrder
{
	private const uint GW_HWNDPREV = 3u;

	private const int DWMWA_CLOAKED = 14;

	[DllImport("user32.dll")]
	private static extern nint GetWindow(nint hwnd, uint cmd);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint hwnd);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(nint hwnd, StringBuilder text, int max);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowText(nint hwnd, StringBuilder text, int max);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);

	public static nint ForeignWindowAbove(nint hwnd, (int X, int Y, int W, int H) rectPx)
	{
		if (rectPx.W <= 0 || rectPx.H <= 0)
		{
			return 0;
		}
		uint processId = (uint)Environment.ProcessId;
		int num = 0;
		nint window = GetWindow(hwnd, 3u);
		while (window != 0 && num++ < 2000)
		{
			if (IsWindowVisible(window) && !IsIconic(window))
			{
				Native.GetWindowThreadProcessId(window, out var pid);
				if (pid != processId || window == FullscreenGuard.TestGameWindow)
				{
					uint num2 = (uint)Native.GetWindowLongPtr(window, -20);
					if (((num2 & 0x20) == 0 || (num2 & 0x80000) == 0) && (DwmGetWindowAttribute(window, 14, out var value, 4) != 0 || value == 0))
					{
						string text = ClassName(window);
						bool flag = ((text == "Shell_TrayWnd" || text == "Shell_SecondaryTrayWnd") ? true : false);
						if (!flag && Native.GetWindowRectPublic(window, out var rc) && rc.Width > 0 && rc.Height > 0 && rc.Left < rectPx.X + rectPx.W && rectPx.X < rc.Right && rc.Top < rectPx.Y + rectPx.H && rectPx.Y < rc.Bottom)
						{
							return window;
						}
					}
				}
			}
			window = GetWindow(window, 3u);
		}
		return 0;
	}

	public static string Describe(nint hwnd)
	{
		StringBuilder stringBuilder = new StringBuilder(96);
		GetWindowText(hwnd, stringBuilder, 96);
		Native.GetWindowThreadProcessId(hwnd, out var pid);
		string value;
		try
		{
			value = Process.GetProcessById((int)pid).ProcessName;
		}
		catch
		{
			value = pid.ToString();
		}
		return $"{value} [{ClassName(hwnd)}]{((stringBuilder.Length > 0) ? " (titled)" : "")}";
	}

	private static string ClassName(nint hwnd)
	{
		StringBuilder stringBuilder = new StringBuilder(96);
		GetClassName(hwnd, stringBuilder, 96);
		return stringBuilder.ToString();
	}

	public static void RaiseToTop(nint hwnd)
	{
		Native.SetWindowPos(hwnd, FullscreenGuard.TopmostInsertAfter(), 0, 0, 0, 0, 531u);
	}
}
