using System.Runtime.InteropServices;

namespace HeyClicky;

public static class NonRudeWindow
{
	public static void Mark(nint hwnd)
	{
		if (hwnd != 0 && !SetPropW(hwnd, "NonRudeHWND", 1))
		{
			Log.Warn("NonRudeWindow", $"SetProp NonRudeHWND failed for 0x{hwnd:X} ({Marshal.GetLastWin32Error()})");
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool SetPropW(nint hwnd, string name, nint data);
}
