using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Win32;

public static class ForegroundTracker
{
	private static Thread? thread;

	private static nint lastExternalWindow;

	private static readonly object Gate = new object();

	private static readonly WINEVENTPROC Callback = OnForeground;

	public static nint LastExternalWindow => Interlocked.CompareExchange(ref lastExternalWindow, 0, 0);

	public static event Action<nint>? ExternalWindowCameForward;

	public unsafe static void Start()
	{
		lock (Gate)
		{
			if (thread == null)
			{
				HWND foregroundWindow = PInvoke.GetForegroundWindow();
				if (!foregroundWindow.IsNull && !IsOwn(foregroundWindow))
				{
					lastExternalWindow = (nint)foregroundWindow.Value;
				}
				thread = new Thread(Run)
				{
					IsBackground = true,
					Name = "HeyClicky foreground tracker"
				};
				thread.Start();
			}
		}
	}

	private unsafe static bool IsOwn(HWND window)
	{
		uint num = default(uint);
		PInvoke.GetWindowThreadProcessId(window, &num);
		return num == (uint)Environment.ProcessId;
	}

	private static void Run()
	{
		using UnhookWinEventSafeHandle unhookWinEventSafeHandle = PInvoke.SetWinEventHook(3u, 3u, null, Callback, 0u, 0u, 2u);
		if (unhookWinEventSafeHandle.IsInvalid)
		{
			Log.Warn("Foreground", $"SetWinEventHook failed ({Marshal.GetLastPInvokeError()})");
		}
		else
		{
			MSG lpMsg;
			while ((bool)PInvoke.GetMessage(out lpMsg, HWND.Null, 0u, 0u))
			{
				PInvoke.TranslateMessage(in lpMsg);
				PInvoke.DispatchMessage(in lpMsg);
			}
		}
	}

	private unsafe static void OnForeground(HWINEVENTHOOK hook, uint @event, HWND window, int idObject, int idChild, uint thread, uint time)
	{
		if (window.IsNull)
		{
			return;
		}
		try
		{
			if (!IsOwn(window))
			{
				Interlocked.Exchange(ref lastExternalWindow, (nint)window.Value);
				ForegroundTracker.ExternalWindowCameForward?.Invoke((nint)window.Value);
			}
		}
		catch
		{
		}
	}
}
