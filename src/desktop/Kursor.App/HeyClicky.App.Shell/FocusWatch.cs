using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace HeyClicky.App.Shell;

internal static class FocusWatch
{
	private delegate void WinEventProc(nint hook, uint eventType, nint hwnd, int idObject, int idChild, uint thread, uint time);

	private const uint EVENT_SYSTEM_FOREGROUND = 3u;

	private const uint EVENT_SYSTEM_MINIMIZESTART = 22u;

	private const uint WINEVENT_OUTOFCONTEXT = 0u;

	private const int OBJID_WINDOW = 0;

	private static WinEventProc? callback;

	private static nint foregroundHook;

	private static nint minimizeHook;

	private static nint lastForeground;

	private static nint lastGame;

	public static event Action<bool, bool>? ForegroundChanged;

	public static void Start()
	{
		if (callback == null)
		{
			callback = OnEvent;
			foregroundHook = SetWinEventHook(3u, 3u, 0, callback, 0u, 0u, 0u);
			minimizeHook = SetWinEventHook(22u, 22u, 0, callback, 0u, 0u, 0u);
			lastForeground = GetForegroundWindow();
			lastGame = FullscreenGuard.GameWindow();
			if (foregroundHook == 0 || minimizeHook == 0)
			{
				Log.Warn("focus", "the foreground watch didn't start");
			}
		}
	}

	private static void OnEvent(nint hook, uint eventType, nint hwnd, int idObject, int idChild, uint thread, uint time)
	{
		if (idObject != 0 || hwnd == 0)
		{
			return;
		}
		try
		{
			if (eventType == 22)
			{
				if (hwnd == lastGame)
				{
					Log.Warn("focus", $"the fullscreen game {FullscreenGuard.Describe(hwnd)} is being minimized ({FullscreenGuard.NotificationState()})");
				}
				return;
			}
			nint num = lastForeground;
			lastForeground = hwnd;
			GetWindowThreadProcessId(hwnd, out var pid);
			if (num != hwnd)
			{
				uint pid2;
				bool arg = num != 0 && GetWindowThreadProcessId(num, out pid2) != 0 && pid2 == Environment.ProcessId;
				FocusWatch.ForegroundChanged?.Invoke(arg, pid == Environment.ProcessId);
			}
			bool flag = num != 0 && num == lastGame;
			if (pid == Environment.ProcessId)
			{
				Log.Warn("focus", "HeyClicky's \"" + Title(hwnd) + "\" took the foreground from " + ((num == 0) ? "nothing" : FullscreenGuard.Describe(num)) + (flag ? " (a fullscreen game: it may leave fullscreen)" : ""));
			}
			else if (flag)
			{
				Log.Info("focus", "the fullscreen game " + FullscreenGuard.Describe(num) + " lost the foreground to " + FullscreenGuard.Describe(hwnd));
			}
			nint num2 = FullscreenGuard.GameWindow();
			if (num2 != 0)
			{
				lastGame = num2;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("focus", "foreground watch: " + ex.Message);
		}
	}

	public static void NoteActivated(string window)
	{
		IEnumerable<string> source = from m in (from f in new StackTrace(2, fNeedFileInfo: false).GetFrames()
				select f.GetMethod() into m
				where (object)m != null && m.DeclaringType?.Namespace?.StartsWith("HeyClicky", StringComparison.Ordinal) == true
				select m).Take(6)
			select m.DeclaringType.Name + "." + m.Name;
		Log.Info("focus", "\"" + window + "\" activated, from " + string.Join(" ← ", source.DefaultIfEmpty("outside HeyClicky's code (a click or the system)")));
	}

	private static string Title(nint hwnd)
	{
		StringBuilder stringBuilder = new StringBuilder(96);
		GetWindowText(hwnd, stringBuilder, stringBuilder.Capacity);
		return stringBuilder.ToString();
	}

	[DllImport("user32.dll")]
	private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint module, WinEventProc callback, uint process, uint thread, uint flags);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowText(nint hwnd, StringBuilder text, int max);
}
