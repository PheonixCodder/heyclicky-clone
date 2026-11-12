using System.Runtime.InteropServices;

namespace HeyClicky.Platform.Overlay;

internal static class PointerFreedom
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct GUITHREADINFO
	{
		public int cbSize;

		public uint flags;

		public nint hwndActive;

		public nint hwndFocus;

		public nint hwndCapture;

		public nint hwndMenuOwner;

		public nint hwndMoveSize;

		public nint hwndCaret;

		public RECT rcCaret;
	}

	public static nint ForegroundWindow()
	{
		return GetForegroundWindow();
	}

	public static bool MovesFreely(RectD virtualScreenPx)
	{
		if ((GetAsyncKeyState(1) & 0x8000) != 0 || (GetAsyncKeyState(2) & 0x8000) != 0 || (GetAsyncKeyState(4) & 0x8000) != 0)
		{
			return false;
		}
		if (GetClipCursor(out var rect) && ((double)rect.Left > virtualScreenPx.X + 1.0 || (double)rect.Top > virtualScreenPx.Y + 1.0 || (double)rect.Right < virtualScreenPx.Right - 1.0 || (double)rect.Bottom < virtualScreenPx.Bottom - 1.0))
		{
			return false;
		}
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == 0)
		{
			return true;
		}
		uint processId;
		uint windowThreadProcessId = GetWindowThreadProcessId(foregroundWindow, out processId);
		GUITHREADINFO info = new GUITHREADINFO
		{
			cbSize = Marshal.SizeOf<GUITHREADINFO>()
		};
		if (GetGUIThreadInfo(windowThreadProcessId, ref info))
		{
			return info.hwndCapture == 0;
		}
		return true;
	}

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int key);

	[DllImport("user32.dll")]
	private static extern bool GetClipCursor(out RECT rect);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

	[DllImport("user32.dll")]
	private static extern bool GetGUIThreadInfo(uint thread, ref GUITHREADINFO info);
}
