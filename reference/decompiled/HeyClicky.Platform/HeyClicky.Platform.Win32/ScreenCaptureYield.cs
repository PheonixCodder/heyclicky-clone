using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HeyClicky.Platform.Win32;

public static class ScreenCaptureYield
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

	private const int PollMilliseconds = 100;

	private const int AppearanceTimeoutMilliseconds = 3000;

	private const int ClosedGraceMilliseconds = 300;

	private const int MaximumWatchMilliseconds = 600000;

	private const int DWMWA_CLOAK = 13;

	private static readonly string[] CaptureUiProcesses = new string[3] { "SnippingTool", "ScreenClippingHost", "ScreenSketch" };

	private static readonly object Gate = new object();

	private static readonly HashSet<nint> Registered = new HashSet<nint>();

	private static readonly HashSet<nint> Cloaked = new HashSet<nint>();

	private static Task? watch;

	private static long watchStartedTicks;

	private static bool yielding;

	private static readonly Dictionary<uint, string?> ProcessNames = new Dictionary<uint, string>();

	public static bool IsYielding
	{
		get
		{
			lock (Gate)
			{
				return Cloaked.Count > 0 || yielding;
			}
		}
	}

	public static (bool SawCaptureUi, int YieldedMilliseconds) LastWatch { get; private set; }

	public static event Action<bool>? Changed;

	public static void Register(nint hwnd)
	{
		if (hwnd == 0)
		{
			return;
		}
		lock (Gate)
		{
			Registered.Add(hwnd);
			if (yielding)
			{
				Cloak(hwnd, cloak: true);
			}
		}
	}

	public static void Unregister(nint hwnd)
	{
		lock (Gate)
		{
			Registered.Remove(hwnd);
			Cloaked.Remove(hwnd);
		}
	}

	public static void NoteCaptureUiMayBeOpening(string reason)
	{
		lock (Gate)
		{
			watchStartedTicks = Environment.TickCount64;
			if (!yielding)
			{
				yielding = true;
				Log.Info("CaptureYield", "stepping HeyClicky's windows aside (" + reason + ")");
				foreach (nint item in Registered)
				{
					Cloak(item, cloak: true);
				}
			}
			Task task = watch;
			if (task != null && !task.IsCompleted)
			{
				return;
			}
			watch = Task.Run(async delegate
			{
				try
				{
					await WatchUntilTheCaptureUiClosesAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception ex)
				{
					Log.Error("CaptureYield", "the watch failed", ex);
				}
				finally
				{
					lock (Gate)
					{
						if (yielding)
						{
							yielding = false;
							foreach (nint item2 in Cloaked.ToList())
							{
								Cloak(item2, cloak: false);
							}
							Cloaked.Clear();
						}
					}
				}
			});
		}
		try
		{
			ScreenCaptureYield.Changed?.Invoke(obj: true);
		}
		catch (Exception ex2)
		{
			Log.Warn("CaptureYield", "observer threw: " + ex2.Message);
		}
	}

	public static void NoteForegroundWindow(nint hwnd)
	{
		if (hwnd != 0 && IsCaptureUiProcess(ProcessNameOf(hwnd)))
		{
			NoteCaptureUiMayBeOpening("the snip UI came to the front");
		}
	}

	private static async Task WatchUntilTheCaptureUiClosesAsync()
	{
		long? lastSeen = null;
		bool sawCaptureUi = false;
		long started = Environment.TickCount64;
		while (true)
		{
			long tickCount = Environment.TickCount64;
			bool flag;
			try
			{
				flag = CaptureUiIsOnScreen();
			}
			catch (Exception ex)
			{
				Log.Warn("CaptureYield", "snip UI check failed: " + ex.Message);
				flag = false;
			}
			if (flag && !sawCaptureUi)
			{
				Log.Info("CaptureYield", $"the snip UI is up ({tickCount - started} ms after the watch began)");
			}
			if (flag)
			{
				lastSeen = tickCount;
				sawCaptureUi = true;
			}
			long num;
			lock (Gate)
			{
				num = watchStartedTicks;
			}
			bool num2;
			if (lastSeen.HasValue)
			{
				long valueOrDefault = lastSeen.GetValueOrDefault();
				num2 = tickCount - valueOrDefault >= 300;
			}
			else
			{
				num2 = tickCount - num >= 3000;
			}
			if (num2 || tickCount - started >= 600000)
			{
				break;
			}
			await Task.Delay(100).ConfigureAwait(continueOnCapturedContext: false);
		}
		int num3 = (int)(Environment.TickCount64 - started);
		lock (Gate)
		{
			yielding = false;
			foreach (nint item in Cloaked.ToList())
			{
				Cloak(item, cloak: false);
			}
			Cloaked.Clear();
		}
		LastWatch = (SawCaptureUi: sawCaptureUi, YieldedMilliseconds: num3);
		Log.Info("CaptureYield", $"the snip UI {(sawCaptureUi ? "closed" : "never showed")}: HeyClicky's windows are back after {num3} ms");
		try
		{
			ScreenCaptureYield.Changed?.Invoke(obj: false);
		}
		catch (Exception ex2)
		{
			Log.Warn("CaptureYield", "observer threw: " + ex2.Message);
		}
	}

	private static bool CaptureUiIsOnScreen()
	{
		bool found = false;
		using (DpiAwarenessScope.PerMonitorV2())
		{
			IReadOnlyList<DisplayInfo> displays = DisplayEnumerator.GetDisplays();
			if (displays.Count == 0)
			{
				Log.Warn("CaptureYield", "no displays to check the snip UI against");
			}
			EnumWindows(delegate(nint hwnd, nint _)
			{
				if (!IsWindowVisible(hwnd) || IsCloaked(hwnd))
				{
					return true;
				}
				if (!IsCaptureUiProcess(ProcessNameOf(hwnd)))
				{
					return true;
				}
				if (!GetWindowRect(hwnd, out var rect))
				{
					return true;
				}
				foreach (DisplayInfo item in displays)
				{
					double num = Math.Min(rect.Right, item.BoundsPx.Right) - Math.Max(rect.Left, item.BoundsPx.X);
					double num2 = Math.Min(rect.Bottom, item.BoundsPx.Bottom) - Math.Max(rect.Top, item.BoundsPx.Y);
					if (num > 0.0 && num2 > 0.0 && num * num2 >= 0.9 * item.BoundsPx.Width * item.BoundsPx.Height)
					{
						found = true;
						return false;
					}
				}
				return true;
			}, 0);
			return found;
		}
	}

	private static bool IsCaptureUiProcess(string? name)
	{
		if (name != null)
		{
			return CaptureUiProcesses.Any((string p) => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	private static string? ProcessNameOf(nint hwnd)
	{
		GetWindowThreadProcessId(hwnd, out var processId);
		if (processId == 0 || processId == (uint)Environment.ProcessId)
		{
			return null;
		}
		lock (ProcessNames)
		{
			if (ProcessNames.TryGetValue(processId, out string value))
			{
				return value;
			}
			string text = null;
			try
			{
				using Process process = Process.GetProcessById((int)processId);
				text = process.ProcessName;
			}
			catch
			{
			}
			if (ProcessNames.Count > 256)
			{
				ProcessNames.Clear();
			}
			ProcessNames[processId] = text;
			return text;
		}
	}

	private static void Cloak(nint hwnd, bool cloak)
	{
		int value = (cloak ? 1 : 0);
		int num = DwmSetWindowAttribute(hwnd, 13, ref value, 4);
		if (num != 0)
		{
			Log.Warn("CaptureYield", $"{(cloak ? "cloak" : "uncloak")} 0x{hwnd:X} failed (0x{num:X8})");
		}
		else if (cloak)
		{
			Cloaked.Add(hwnd);
		}
	}

	private static bool IsCloaked(nint hwnd)
	{
		if (DwmGetWindowAttribute(hwnd, 14, out var value, 4) == 0)
		{
			return value != 0;
		}
		return false;
	}

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hwnd, out RECT rect);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);
}
