using System;
using System.Runtime.InteropServices;

namespace HeyClicky.Drawing.Interop;

internal static class NativeMethods
{
	public delegate nint WndProc(nint hwnd, uint message, nint wParam, nint lParam);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct WNDCLASSEXW
	{
		public int cbSize;

		public uint style;

		public nint lpfnWndProc;

		public int cbClsExtra;

		public int cbWndExtra;

		public nint hInstance;

		public nint hIcon;

		public nint hCursor;

		public nint hbrBackground;

		public nint lpszMenuName;

		public nint lpszClassName;

		public nint hIconSm;
	}

	public struct MSG
	{
		public nint hwnd;

		public uint message;

		public nint wParam;

		public nint lParam;

		public uint time;

		public int ptX;

		public int ptY;

		public uint lPrivate;
	}

	public struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public struct POINT
	{
		public int X;

		public int Y;
	}

	public struct TRACKMOUSEEVENT
	{
		public int cbSize;

		public uint dwFlags;

		public nint hwndTrack;

		public uint dwHoverTime;
	}

	public const int WS_POPUP = int.MinValue;

	public const int WS_CLIPCHILDREN = 33554432;

	public const int WS_EX_TOPMOST = 8;

	public const int WS_EX_TRANSPARENT = 32;

	public const int WS_EX_TOOLWINDOW = 128;

	public const int WS_EX_LAYERED = 524288;

	public const int WS_EX_NOREDIRECTIONBITMAP = 2097152;

	public const int WS_EX_NOACTIVATE = 134217728;

	public const int SW_HIDE = 0;

	public const int SW_SHOWNOACTIVATE = 4;

	public const int SW_SHOWNA = 8;

	public const uint SWP_NOSIZE = 1u;

	public const uint SWP_NOMOVE = 2u;

	public const uint SWP_NOZORDER = 4u;

	public const uint SWP_NOACTIVATE = 16u;

	public const uint SWP_SHOWWINDOW = 64u;

	public const uint SWP_NOOWNERZORDER = 512u;

	public static readonly nint HWND_TOPMOST = new IntPtr(-1);

	public static readonly nint HWND_NOTTOPMOST = new IntPtr(-2);

	public const uint LWA_ALPHA = 2u;

	public const uint WDA_NONE = 0u;

	public const uint WDA_EXCLUDEFROMCAPTURE = 17u;

	public const uint WM_DESTROY = 2u;

	public const uint WM_CLOSE = 16u;

	public const uint WM_QUIT = 18u;

	public const uint WM_MOUSEACTIVATE = 33u;

	public const uint WM_NCHITTEST = 132u;

	public const uint WM_MOUSEMOVE = 512u;

	public const uint WM_LBUTTONDOWN = 513u;

	public const uint WM_LBUTTONUP = 514u;

	public const uint WM_MOUSELEAVE = 675u;

	public const uint WM_SETCURSOR = 32u;

	public const uint WM_DPICHANGED = 736u;

	public const uint WM_DISPLAYCHANGE = 126u;

	public const int MA_NOACTIVATE = 3;

	public const int HTCLIENT = 1;

	public const int HTTRANSPARENT = -1;

	public const uint QS_ALLINPUT = 1279u;

	public const uint MWMO_INPUTAVAILABLE = 4u;

	public const uint WAIT_OBJECT_0 = 0u;

	public const uint INFINITE = uint.MaxValue;

	public const uint PM_REMOVE = 1u;

	public const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 2u;

	public const uint TIMER_ALL_ACCESS = 2031619u;

	public const uint TME_LEAVE = 2u;

	public static readonly nint IDC_HAND = new IntPtr(32649);

	public static readonly nint IDC_ARROW = new IntPtr(32512);

	public static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern ushort RegisterClassExW(ref WNDCLASSEXW windowClass);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern nint CreateWindowExW(int exStyle, string className, string windowName, int style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

	[DllImport("user32.dll")]
	public static extern bool DestroyWindow(nint hwnd);

	[DllImport("user32.dll")]
	public static extern nint DefWindowProcW(nint hwnd, uint message, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	public static extern bool ShowWindow(nint hwnd, int command);

	[DllImport("user32.dll")]
	public static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

	[DllImport("user32.dll")]
	public static extern bool SetLayeredWindowAttributes(nint hwnd, uint colorKey, byte alpha, uint flags);

	[DllImport("user32.dll")]
	public static extern bool SetWindowDisplayAffinity(nint hwnd, uint affinity);

	[DllImport("user32.dll")]
	public static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	public static extern int GetWindowLongW(nint hwnd, int index);

	[DllImport("user32.dll")]
	public static extern int SetWindowLongW(nint hwnd, int index, int value);

	[DllImport("user32.dll")]
	public static extern bool GetCursorPos(out POINT point);

	[DllImport("user32.dll")]
	public static extern nint WindowFromPoint(POINT point);

	[DllImport("user32.dll")]
	public static extern nint GetAncestor(nint hwnd, uint flags);

	[DllImport("user32.dll")]
	public static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT track);

	[DllImport("user32.dll")]
	public static extern nint SetCursor(nint cursor);

	[DllImport("user32.dll")]
	public static extern nint LoadCursorW(nint instance, nint name);

	[DllImport("user32.dll")]
	public static extern nint SetCapture(nint hwnd);

	[DllImport("user32.dll")]
	public static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	public static extern uint GetDpiForWindow(nint hwnd);

	[DllImport("user32.dll")]
	public static extern bool PeekMessageW(out MSG message, nint hwnd, uint filterMin, uint filterMax, uint remove);

	[DllImport("user32.dll")]
	public static extern bool TranslateMessage(ref MSG message);

	[DllImport("user32.dll")]
	public static extern nint DispatchMessageW(ref MSG message);

	[DllImport("user32.dll")]
	public static extern void PostQuitMessage(int exitCode);

	[DllImport("user32.dll")]
	public static extern bool PostThreadMessageW(uint threadId, uint message, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	public unsafe static extern uint MsgWaitForMultipleObjectsEx(uint count, nint* handles, uint milliseconds, uint wakeMask, uint flags);

	[DllImport("kernel32.dll")]
	public static extern nint GetModuleHandleW(nint name);

	[DllImport("kernel32.dll")]
	public static extern uint GetCurrentThreadId();

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern nint CreateWaitableTimerExW(nint attributes, nint name, uint flags, uint access);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetWaitableTimerEx(nint timer, ref long dueTime, int period, nint completion, nint completionArg, nint wakeContext, uint tolerableDelay);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool CancelWaitableTimer(nint timer);

	[DllImport("kernel32.dll")]
	public static extern bool CloseHandle(nint handle);

	[DllImport("user32.dll")]
	public static extern nint SetThreadDpiAwarenessContext(nint context);

	public static int LowWord(nint value)
	{
		return (short)((long)value & 0xFFFFL);
	}

	public static int HighWord(nint value)
	{
		return (short)(((long)value >> 16) & 0xFFFF);
	}
}
