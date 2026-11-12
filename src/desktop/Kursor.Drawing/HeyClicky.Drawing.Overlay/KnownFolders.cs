using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HeyClicky.Drawing.Overlay;

internal static class KnownFolders
{
	private static readonly Guid DownloadsId = new Guid("374DE290-123F-4565-9164-39C4925E467B");

	public static string Downloads()
	{
		try
		{
			if (SHGetKnownFolderPath(DownloadsId, 0u, IntPtr.Zero, out var path) == 0 && path != IntPtr.Zero)
			{
				try
				{
					return Marshal.PtrToStringUni(path) ?? Fallback();
				}
				finally
				{
					Marshal.FreeCoTaskMem(path);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "Downloads folder lookup failed: " + ex.Message);
		}
		return Fallback();
	}

	private static string Fallback()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
	}

	[DllImport("shell32.dll")]
	private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint flags, nint token, out nint path);
}
