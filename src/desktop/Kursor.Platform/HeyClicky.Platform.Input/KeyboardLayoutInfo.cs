using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace HeyClicky.Platform.Input;

public static class KeyboardLayoutInfo
{
	private static nint cachedLayout;

	private static bool cachedHasAltGr;

	public static bool ForegroundLayoutHasAltGr()
	{
		HWND foregroundWindow = PInvoke.GetForegroundWindow();
		uint lpdwProcessId;
		nint num = (nint)PInvoke.GetKeyboardLayout((!foregroundWindow.IsNull) ? PInvoke.GetWindowThreadProcessId(foregroundWindow, out lpdwProcessId) : 0u);
		if (num == cachedLayout && num != 0)
		{
			return cachedHasAltGr;
		}
		bool result = LayoutHasAltGr(num);
		cachedLayout = num;
		cachedHasAltGr = result;
		return result;
	}

	public static IReadOnlyList<string> InstalledLayoutLanguageTags()
	{
		try
		{
			int keyboardLayoutList = GetKeyboardLayoutList(0, null);
			if (keyboardLayoutList <= 0)
			{
				return Array.Empty<string>();
			}
			nint[] array = new nint[keyboardLayoutList];
			keyboardLayoutList = GetKeyboardLayoutList(keyboardLayoutList, array);
			return (from tag in (from layout in array.Take(keyboardLayoutList)
					select (int)((long)layout & 0xFFFFL)).Distinct().Select(delegate(int languageId)
				{
					try
					{
						return CultureInfo.GetCultureInfo(languageId).Name;
					}
					catch (CultureNotFoundException)
					{
						return (string)null;
					}
				}).OfType<string>()
				where tag.Length > 0
				select tag).ToList();
		}
		catch
		{
			return Array.Empty<string>();
		}
	}

	[DllImport("user32.dll")]
	private static extern int GetKeyboardLayoutList(int bufferCount, [Out] nint[]? layouts);

	public static bool LayoutHasAltGr(nint layout)
	{
		for (char c = '!'; c <= 'ɏ'; c = (char)(c + 1))
		{
			short num = PInvoke.VkKeyScanEx(c, new HKL(layout));
			if (num != -1 && ((num >> 8) & 0xFF & 6) == 6)
			{
				return true;
			}
		}
		return false;
	}
}
