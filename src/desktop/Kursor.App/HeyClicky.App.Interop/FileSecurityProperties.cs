using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HeyClicky.App.Interop;

public static class FileSecurityProperties
{
	private const uint SHOP_FILEPATH = 2u;

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SHObjectProperties(nint hwnd, uint shopObjectType, string pszObjectName, string? pszPropertyPage);

	public static bool Show(string path)
	{
		try
		{
			if (!File.Exists(path) && !Directory.Exists(path))
			{
				return false;
			}
			bool flag = SHObjectProperties(0, 2u, path, "Security");
			Log.Info("PageRead", flag ? "opened the file's Security properties" : "Security properties didn't open");
			return flag;
		}
		catch (Exception ex)
		{
			Log.Warn("PageRead", "Security properties: " + ex.Message);
			return false;
		}
	}
}
