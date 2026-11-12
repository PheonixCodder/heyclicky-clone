using System;
using System.Runtime.InteropServices;
using HeyClicky.App.Interop;

namespace HeyClicky.App.Shell;

public static class Displays
{
	public readonly record struct Monitor(nint Handle, Native.RECT Bounds, Native.RECT Work, double Scale);

	public static Monitor UnderCursor()
	{
		Native.GetCursorPos(out var p);
		return From(Native.MonitorFromPoint(p, 1));
	}

	public static Monitor Primary()
	{
		Native.POINT p = default(Native.POINT);
		p.X = 0;
		p.Y = 0;
		return From(Native.MonitorFromPoint(p, 1));
	}

	public static Monitor ForWindow(nint hwnd)
	{
		return From(Native.MonitorFromWindow(hwnd, 2));
	}

	public static Monitor ForWindow(OverlayWindow window)
	{
		if (window.IsShown)
		{
			return ForWindow(window.Hwnd);
		}
		var (num, num2, num3, num4) = window.RectPx;
		return At(num + num3 / 2, num2 + num4 / 2);
	}

	public static Monitor At(int x, int y)
	{
		Native.POINT p = default(Native.POINT);
		p.X = x;
		p.Y = y;
		return From(Native.MonitorFromPoint(p, 2));
	}

	public static double DpiAt(int x, int y)
	{
		return At(x, y).Scale * 96.0;
	}

	public static Monitor From(nint mon)
	{
		Native.MONITORINFO mONITORINFO = default(Native.MONITORINFO);
		mONITORINFO.cbSize = Marshal.SizeOf<Native.MONITORINFO>();
		Native.MONITORINFO info = mONITORINFO;
		Native.GetMonitorInfo(mon, ref info);
		Native.GetDpiForMonitor(mon, 0, out var dpiX, out var _);
		return new Monitor(mon, info.rcMonitor, info.rcWork, (dpiX == 0) ? 1.0 : ((double)dpiX / 96.0));
	}

	public static (int x, int y, int w, int h) CenteredInWork(Monitor m, double wDip, double hDip)
	{
		int num = (int)Math.Round(wDip * m.Scale);
		int num2 = (int)Math.Round(hDip * m.Scale);
		int item = m.Work.Left + (m.Work.Width - num) / 2;
		int item2 = m.Work.Top + (m.Work.Height - num2) / 2;
		return (x: item, y: item2, w: num, h: num2);
	}
}
