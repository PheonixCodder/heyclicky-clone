using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HeyClicky.App.Shell;

namespace HeyClicky.App.Interop;

public static class Native
{
	private struct LASTINPUTINFO
	{
		public uint cbSize;

		public uint dwTime;
	}

	public delegate bool EnumWindowsProc(nint hwnd, nint data);

	public struct DWM_BLURBEHIND
	{
		public uint dwFlags;

		public int fEnable;

		public nint hRgnBlur;

		public int fTransitionOnMaximized;
	}

	public struct POINT
	{
		public int X;

		public int Y;
	}

	public struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;

		public int Width => Right - Left;

		public int Height => Bottom - Top;
	}

	public struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	public delegate bool MonitorEnumProc(nint mon, nint hdc, ref RECT rc, nint data);

	public struct MARGINS
	{
		public int L;

		public int R;

		public int T;

		public int B;
	}

	public struct DispatcherQueueOptions
	{
		public int dwSize;

		public int threadType;

		public int apartmentType;
	}

	public const int GWL_STYLE = -16;

	public const int GWL_EXSTYLE = -20;

	public const int GWLP_WNDPROC = -4;

	public const int GCLP_HBRBACKGROUND = -10;

	public const int BLACK_BRUSH = 4;

	public const uint WS_POPUP = 2147483648u;

	public const uint WS_VISIBLE = 268435456u;

	public const uint WS_CLIPCHILDREN = 33554432u;

	public const uint WS_CAPTION = 12582912u;

	public const uint WS_THICKFRAME = 262144u;

	public const uint WS_SYSMENU = 524288u;

	public const uint WS_MINIMIZEBOX = 131072u;

	public const uint WS_MAXIMIZEBOX = 65536u;

	public const uint WS_EX_TOPMOST = 8u;

	public const uint WS_EX_TRANSPARENT = 32u;

	public const uint WS_EX_TOOLWINDOW = 128u;

	public const uint WS_EX_APPWINDOW = 262144u;

	public const uint WS_EX_LAYERED = 524288u;

	public const uint WS_EX_NOACTIVATE = 134217728u;

	public const uint SWP_NOSIZE = 1u;

	public const uint SWP_NOMOVE = 2u;

	public const uint SWP_NOZORDER = 4u;

	public const uint SWP_NOACTIVATE = 16u;

	public const uint SWP_FRAMECHANGED = 32u;

	public const uint SWP_SHOWWINDOW = 64u;

	public const uint SWP_HIDEWINDOW = 128u;

	public static readonly nint HWND_TOPMOST = -1;

	public const int SW_HIDE = 0;

	public const int SW_SHOWNOACTIVATE = 4;

	public const int SW_SHOWNA = 8;

	public const int SW_SHOW = 5;

	public const uint WDA_NONE = 0u;

	public const uint WDA_EXCLUDEFROMCAPTURE = 17u;

	public const uint WM_MOUSEACTIVATE = 33u;

	public const int MA_NOACTIVATE = 3;

	public const uint WM_DPICHANGED = 736u;

	public const uint WM_DISPLAYCHANGE = 126u;

	public const uint WM_SETTINGCHANGE = 26u;

	public const uint WM_ACTIVATE = 6u;

	public const uint WM_NCACTIVATE = 134u;

	public const uint WM_CLOSE = 16u;

	public const uint WM_DESTROY = 2u;

	public const uint WM_USER = 1024u;

	public const uint WM_APP = 32768u;

	public const uint WM_COMMAND = 273u;

	public const uint WM_LBUTTONUP = 514u;

	public const uint WM_RBUTTONUP = 517u;

	public const uint WM_LBUTTONDBLCLK = 515u;

	public const uint WM_CONTEXTMENU = 123u;

	public const uint WM_COPYDATA = 74u;

	public const int MONITOR_DEFAULTTONEAREST = 2;

	public const int MONITOR_DEFAULTTOPRIMARY = 1;

	public const int RGN_COPY = 5;

	public const uint GA_ROOT = 2u;

	private const byte VK_MENU = 18;

	private const byte VK_MASK_KEY = byte.MaxValue;

	private const uint KEYEVENTF_KEYUP = 2u;

	public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

	public const int DWMWA_BORDER_COLOR = 34;

	public const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;

	public const int DWMWCP_DONOTROUND = 1;

	public const int DWMWCP_ROUND = 2;

	public const int DWMWA_COLOR_NONE = -2;

	public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

	[DllImport("user32.dll")]
	private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

	[DllImport("user32.dll")]
	public static extern bool IsIconic(nint hwnd);

	public static long MillisecondsSinceLastInput()
	{
		LASTINPUTINFO lASTINPUTINFO = default(LASTINPUTINFO);
		lASTINPUTINFO.cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>();
		LASTINPUTINFO info = lASTINPUTINFO;
		if (!GetLastInputInfo(ref info))
		{
			return 0L;
		}
		return (uint)(Environment.TickCount - (int)info.dwTime);
	}

	[DllImport("user32.dll", EntryPoint = "SetClassLongPtrW")]
	public static extern nint SetClassLongPtr(nint hwnd, int index, nint value);

	[DllImport("gdi32.dll")]
	public static extern nint GetStockObject(int index);

	[DllImport("user32.dll")]
	public static extern bool SetLayeredWindowAttributes(nint hwnd, uint key, byte alpha, uint flags);

	[DllImport("user32.dll")]
	public static extern bool GetClientRect(nint hwnd, out RECT rc);

	[DllImport("user32.dll", EntryPoint = "GetWindowRect")]
	public static extern bool GetWindowRectPublic(nint hwnd, out RECT rc);

	[DllImport("user32.dll")]
	public static extern int FillRect(nint hdc, ref RECT rc, nint brush);

	[DllImport("user32.dll")]
	public static extern bool EnumChildWindows(nint parent, EnumWindowsProc proc, nint data);

	[DllImport("dwmapi.dll")]
	public static extern int DwmEnableBlurBehindWindow(nint hwnd, ref DWM_BLURBEHIND bb);

	public static void EnableAlphaComposition(nint hwnd)
	{
		nint num = CreateRectRgn(0, 0, -1, -1);
		DWM_BLURBEHIND dWM_BLURBEHIND = default(DWM_BLURBEHIND);
		dWM_BLURBEHIND.dwFlags = 3u;
		dWM_BLURBEHIND.fEnable = 1;
		dWM_BLURBEHIND.hRgnBlur = num;
		DWM_BLURBEHIND bb = dWM_BLURBEHIND;
		DwmEnableBlurBehindWindow(hwnd, ref bb);
		DeleteObject(num);
	}

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	public static extern nint GetWindowLongPtr(nint hwnd, int index);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	public static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);

	[DllImport("user32.dll")]
	public static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int cx, int cy, uint flags);

	[DllImport("user32.dll")]
	public static extern bool ShowWindow(nint hwnd, int cmd);

	[DllImport("user32.dll")]
	public static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	public static extern bool IsWindow(nint hwnd);

	[DllImport("user32.dll")]
	public static extern int SetWindowRgn(nint hwnd, nint hrgn, bool redraw);

	[DllImport("gdi32.dll")]
	public static extern nint CreateRectRgn(int l, int t, int r, int b);

	[DllImport("gdi32.dll")]
	public static extern nint CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

	[DllImport("gdi32.dll")]
	public static extern int CombineRgn(nint dst, nint a, nint b, int mode);

	[DllImport("gdi32.dll")]
	public static extern bool DeleteObject(nint obj);

	[DllImport("gdi32.dll")]
	public static extern int OffsetRgn(nint rgn, int x, int y);

	[DllImport("gdi32.dll")]
	public static extern bool EqualRgn(nint a, nint b);

	[DllImport("user32.dll")]
	public static extern int GetWindowRgn(nint hwnd, nint rgn);

	[DllImport("user32.dll")]
	private static extern nint GetWindow(nint hwnd, uint cmd);

	public static List<nint> DirectChildren(nint hwnd)
	{
		List<nint> list = new List<nint>();
		for (nint window = GetWindow(hwnd, 5u); window != 0; window = GetWindow(window, 2u))
		{
			list.Add(window);
		}
		return list;
	}

	[DllImport("user32.dll")]
	public static extern bool SetWindowDisplayAffinity(nint hwnd, uint affinity);

	[DllImport("user32.dll")]
	public static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	public static extern bool SetForegroundWindow(nint hwnd);

	[DllImport("user32.dll")]
	public static extern nint SetFocus(nint hwnd);

	[DllImport("user32.dll")]
	public static extern nint SetActiveWindow(nint hwnd);

	[DllImport("user32.dll")]
	public static extern bool BringWindowToTop(nint hwnd);

	[DllImport("user32.dll")]
	public static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);

	[DllImport("user32.dll")]
	public static extern nint WindowFromPoint(POINT point);

	[DllImport("user32.dll")]
	public static extern nint GetAncestor(nint hwnd, uint flags);

	public static bool IsOwnWindowAt(int x, int y)
	{
		POINT point = default(POINT);
		point.X = x;
		point.Y = y;
		return IsOwnWindow(WindowFromPoint(point));
	}

	public static bool IsOwnWindow(nint hwnd)
	{
		if (hwnd == 0)
		{
			return false;
		}
		GetWindowThreadProcessId(hwnd, out var pid);
		return pid == (uint)Environment.ProcessId;
	}

	public static string DescribeForeground(nint hwnd)
	{
		if (hwnd != 0)
		{
			if (!IsOwnWindow(hwnd))
			{
				return FullscreenGuard.Describe(hwnd);
			}
			return $"HeyClicky's own window {hwnd}";
		}
		return "nothing";
	}

	[DllImport("kernel32.dll")]
	public static extern uint GetCurrentThreadId();

	[DllImport("user32.dll")]
	public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

	[DllImport("user32.dll")]
	public static extern bool AllowSetForegroundWindow(int pid);

	[DllImport("user32.dll")]
	public static extern void keybd_event(byte vk, byte scan, uint flags, nuint extra);

	[DllImport("user32.dll")]
	public static extern bool GetCursorPos(out POINT p);

	[DllImport("user32.dll")]
	public static extern nint MonitorFromPoint(POINT p, int flags);

	[DllImport("user32.dll")]
	public static extern nint MonitorFromWindow(nint hwnd, int flags);

	[DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
	public static extern bool GetMonitorInfo(nint mon, ref MONITORINFO info);

	[DllImport("shcore.dll")]
	public static extern int GetDpiForMonitor(nint mon, int type, out uint dpiX, out uint dpiY);

	[DllImport("user32.dll")]
	public static extern uint GetDpiForWindow(nint hwnd);

	[DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
	public static extern nint CallWindowProc(nint prev, nint hwnd, uint msg, nint w, nint l);

	[DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
	public static extern nint DefWindowProc(nint hwnd, uint msg, nint w, nint l);

	[DllImport("user32.dll", EntryPoint = "SendMessageW")]
	public static extern nint SendMessage(nint hwnd, uint msg, nint w, nint l);

	[DllImport("user32.dll")]
	public static extern bool ReleaseCapture();

	[DllImport("user32.dll", EntryPoint = "PostMessageW")]
	public static extern bool PostMessage(nint hwnd, uint msg, nint w, nint l);

	[DllImport("user32.dll")]
	public static extern bool DestroyWindow(nint hwnd);

	[DllImport("user32.dll")]
	public static extern short GetAsyncKeyState(int vk);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);

	public static bool IsPrimaryButtonDown()
	{
		return (GetAsyncKeyState((GetSystemMetrics(23) == 0) ? 1 : 2) & 0x8000) != 0;
	}

	[DllImport("user32.dll")]
	public static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc proc, nint data);

	[DllImport("dwmapi.dll")]
	public static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int value, int size);

	[DllImport("dwmapi.dll")]
	public static extern int DwmExtendFrameIntoClientArea(nint hwnd, ref MARGINS m);

	[DllImport("CoreMessaging.dll")]
	public static extern int CreateDispatcherQueueController(DispatcherQueueOptions options, out nint controller);

	public static void AddExStyle(nint hwnd, uint add, uint remove = 0u)
	{
		uint num = (uint)GetWindowLongPtr(hwnd, -20);
		num = (num | add) & ~remove;
		SetWindowLongPtr(hwnd, -20, (nint)num);
	}

	public static void SetStyle(nint hwnd, uint add, uint remove)
	{
		uint num = (uint)GetWindowLongPtr(hwnd, -16);
		num = (num | add) & ~remove;
		SetWindowLongPtr(hwnd, -16, (nint)num);
	}

	public static void ForceForeground(nint hwnd)
	{
		if (OverlayWindow.Offscreen)
		{
			return;
		}
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == hwnd)
		{
			return;
		}
		uint pid;
		uint num = ((foregroundWindow != 0) ? GetWindowThreadProcessId(foregroundWindow, out pid) : 0u);
		uint currentThreadId = GetCurrentThreadId();
		bool flag = num != 0 && num != currentThreadId && AttachThreadInput(currentThreadId, num, attach: true);
		try
		{
			BringWindowToTop(hwnd);
			if (!SetForegroundWindow(hwnd))
			{
				keybd_event(18, 0, 0u, 0u);
				keybd_event(byte.MaxValue, 0, 0u, 0u);
				keybd_event(byte.MaxValue, 0, 2u, 0u);
				keybd_event(18, 0, 2u, 0u);
				if (!SetForegroundWindow(hwnd))
				{
					Log.Warn("window", $"couldn't bring window {hwnd} to the foreground (in front: {DescribeForeground(GetForegroundWindow())}, input attached: {flag})");
				}
			}
		}
		finally
		{
			if (flag)
			{
				AttachThreadInput(currentThreadId, num, attach: false);
			}
		}
		nint foregroundWindow2 = GetForegroundWindow();
		if (foregroundWindow2 != hwnd)
		{
			Log.Warn("window", $"window {hwnd} still isn't in front after ForceForeground (in front: {DescribeForeground(foregroundWindow2)}, was: {DescribeForeground(foregroundWindow)}, input attached: {flag})");
		}
	}
}
