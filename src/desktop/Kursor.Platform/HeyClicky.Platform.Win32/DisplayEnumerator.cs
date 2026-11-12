using System;
using System.Collections.Generic;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace HeyClicky.Platform.Win32;

public static class DisplayEnumerator
{
	public unsafe static IReadOnlyList<DisplayInfo> GetDisplays()
	{
		using (DpiAwarenessScope.PerMonitorV2())
		{
			List<(string DeviceName, RECT Bounds, RECT WorkArea, double Scale, bool IsPrimary)> rawMonitors = new List<(string, RECT, RECT, double, bool)>(4);
			PInvoke.EnumDisplayMonitors(HDC.Null, (RECT?)null, (MONITORENUMPROC)delegate(HMONITOR hMonitor, HDC _, RECT* _, LPARAM _)
			{
				MONITORINFOEXW mONITORINFOEXW = default(MONITORINFOEXW);
				mONITORINFOEXW.monitorInfo.cbSize = (uint)sizeof(MONITORINFOEXW);
				if (!PInvoke.GetMonitorInfo(hMonitor, (MONITORINFO*)(&mONITORINFOEXW)))
				{
					return true;
				}
				double item = 1.0;
				if (PInvoke.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var _).Succeeded && dpiX != 0)
				{
					item = (double)dpiX / 96.0;
				}
				string item2 = mONITORINFOEXW.szDevice.ToString();
				bool item3 = (mONITORINFOEXW.monitorInfo.dwFlags & 1) != 0;
				rawMonitors.Add((item2, mONITORINFOEXW.monitorInfo.rcMonitor, mONITORINFOEXW.monitorInfo.rcWork, item, item3));
				return true;
			}, default(LPARAM));
			rawMonitors.Sort(delegate((string DeviceName, RECT Bounds, RECT WorkArea, double Scale, bool IsPrimary) a, (string DeviceName, RECT Bounds, RECT WorkArea, double Scale, bool IsPrimary) b)
			{
				if (a.IsPrimary != b.IsPrimary)
				{
					if (!a.IsPrimary)
					{
						return 1;
					}
					return -1;
				}
				int num = a.Bounds.left.CompareTo(b.Bounds.left);
				return (num == 0) ? a.Bounds.top.CompareTo(b.Bounds.top) : num;
			});
			DisplayInfo[] array = new DisplayInfo[rawMonitors.Count];
			for (int i = 0; i < rawMonitors.Count; i++)
			{
				(string, RECT, RECT, double, bool) tuple = rawMonitors[i];
				array[i] = new DisplayInfo(i, tuple.Item1, ToRect(tuple.Item2), ToRect(tuple.Item3), tuple.Item4, tuple.Item5);
			}
			return array;
		}
	}

	public static DisplayInfo DisplayContaining(PointD pointPx, IReadOnlyList<DisplayInfo>? displays = null)
	{
		if (displays == null)
		{
			displays = GetDisplays();
		}
		DisplayInfo displayInfo = null;
		double num = double.MaxValue;
		foreach (DisplayInfo display in displays)
		{
			if (display.BoundsPx.Contains(pointPx))
			{
				return display;
			}
			double num2 = Math.Max(Math.Max(display.BoundsPx.X - pointPx.X, 0.0), pointPx.X - display.BoundsPx.Right);
			double num3 = Math.Max(Math.Max(display.BoundsPx.Y - pointPx.Y, 0.0), pointPx.Y - display.BoundsPx.Bottom);
			double num4 = num2 * num2 + num3 * num3;
			if (num4 < num)
			{
				num = num4;
				displayInfo = display;
			}
		}
		return displayInfo ?? displays[0];
	}

	public static RectD VirtualScreenPx(IReadOnlyList<DisplayInfo>? displays = null)
	{
		if (displays == null)
		{
			displays = GetDisplays();
		}
		double num = double.MaxValue;
		double num2 = double.MaxValue;
		double num3 = double.MinValue;
		double num4 = double.MinValue;
		foreach (DisplayInfo display in displays)
		{
			num = Math.Min(num, display.BoundsPx.X);
			num2 = Math.Min(num2, display.BoundsPx.Y);
			num3 = Math.Max(num3, display.BoundsPx.Right);
			num4 = Math.Max(num4, display.BoundsPx.Bottom);
		}
		return new RectD(num, num2, num3 - num, num4 - num2);
	}

	public static PointD CursorPositionPx()
	{
		using (DpiAwarenessScope.PerMonitorV2())
		{
			PInvoke.GetCursorPos(out var lpPoint);
			return new PointD(lpPoint.X, lpPoint.Y);
		}
	}

	private static RectD ToRect(RECT rect)
	{
		return new RectD(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
	}
}
