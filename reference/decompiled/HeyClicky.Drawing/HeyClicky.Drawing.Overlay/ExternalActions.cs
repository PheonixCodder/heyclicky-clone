using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace HeyClicky.Drawing.Overlay;

internal static class ExternalActions
{
	public static void OpenPage(Uri page)
	{
		if (page.Scheme != Uri.UriSchemeHttps && page.Scheme != Uri.UriSchemeHttp)
		{
			return;
		}
		try
		{
			ShellOpen.Start(page.AbsoluteUri);
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "page did not open: " + ex.Message);
		}
	}

	public static void CopyText(string text)
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			for (int i = 0; i < 5; i++)
			{
				if (TrySetClipboardText(text))
				{
					return;
				}
				Thread.Sleep(30);
			}
			Log.Warn("Drawing", "copy failed: the clipboard is busy");
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
	}

	private static bool TrySetClipboardText(string text)
	{
		if (!OpenClipboard(IntPtr.Zero))
		{
			return false;
		}
		try
		{
			EmptyClipboard();
			int num = (text.Length + 1) * 2;
			nint num2 = GlobalAlloc(2u, (nuint)num);
			if (num2 == IntPtr.Zero)
			{
				return false;
			}
			nint num3 = GlobalLock(num2);
			Marshal.Copy(text.ToCharArray(), 0, num3, text.Length);
			Marshal.WriteInt16(num3, text.Length * 2, 0);
			GlobalUnlock(num2);
			if (SetClipboardData(13u, num2) == IntPtr.Zero)
			{
				GlobalFree(num2);
				return false;
			}
			return true;
		}
		finally
		{
			CloseClipboard();
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool OpenClipboard(nint owner);

	[DllImport("user32.dll")]
	private static extern bool CloseClipboard();

	[DllImport("user32.dll")]
	private static extern bool EmptyClipboard();

	[DllImport("user32.dll")]
	private static extern nint SetClipboardData(uint format, nint data);

	[DllImport("kernel32.dll")]
	private static extern nint GlobalAlloc(uint flags, nuint bytes);

	[DllImport("kernel32.dll")]
	private static extern nint GlobalLock(nint memory);

	[DllImport("kernel32.dll")]
	private static extern bool GlobalUnlock(nint memory);

	[DllImport("kernel32.dll")]
	private static extern nint GlobalFree(nint memory);
}
