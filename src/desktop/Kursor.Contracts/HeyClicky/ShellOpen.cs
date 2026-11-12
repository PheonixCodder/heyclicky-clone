using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HeyClicky;

public static class ShellOpen
{
	private struct KEYBDINPUT
	{
		public ushort wVk;

		public ushort wScan;

		public uint dwFlags;

		public uint time;

		public nint dwExtraInfo;
	}

	private struct MOUSEINPUT
	{
		public int dx;

		public int dy;

		public uint mouseData;

		public uint dwFlags;

		public uint time;

		public nint dwExtraInfo;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct INPUT
	{
		[FieldOffset(0)]
		public uint type;

		[FieldOffset(8)]
		public KEYBDINPUT ki;

		[FieldOffset(8)]
		public MOUSEINPUT mi;
	}

	private struct MSG
	{
		public nint hwnd;

		public uint message;

		public nint wParam;

		public nint lParam;

		public uint time;

		public int x;

		public int y;
	}

	private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

	private static readonly TimeSpan FindDeadline = TimeSpan.FromSeconds(6L);

	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100L, 0L);

	private static readonly TimeSpan ForegroundLockAfterOpen = TimeSpan.FromSeconds(4L);

	private const uint ASFW_ANY = uint.MaxValue;

	private const int SW_RESTORE = 9;

	private const uint GW_OWNER = 4u;

	private const int GWL_EXSTYLE = -20;

	private const long WS_EX_TOOLWINDOW = 128L;

	private const int DWMWA_CLOAKED = 14;

	private const uint PROCESS_QUERY_LIMITED_INFORMATION = 4096u;

	private const uint ASSOCF_NOTRUNCATE = 32u;

	private const uint ASSOCF_INIT_IGNOREUNKNOWN = 1024u;

	private const uint ASSOCF_IS_PROTOCOL = 4096u;

	private const uint ASSOCSTR_EXECUTABLE = 2u;

	private const uint INPUT_KEYBOARD = 1u;

	private const ushort VK_MENU = 18;

	private const uint KEYEVENTF_KEYUP = 2u;

	private const nint HeyClickyInputSignature = 1212369995;

	private const uint PM_NOREMOVE = 0u;

	private const uint LSFW_LOCK = 1u;

	private const uint LSFW_UNLOCK = 2u;

	public static Func<bool>? UserIsTypingInHeyClicky { get; set; }

	public static void Start(string target)
	{
		bool flag = IsUserTyping();
		string handler = HandlerExecutable(target);
		IReadOnlyList<ShellOpenRules.TopWindow> before = (flag ? new List<ShellOpenRules.TopWindow>() : TopLevelWindows());
		if (!flag)
		{
			AllowSetForegroundWindow(uint.MaxValue);
		}
		else if (LockSetForegroundWindow(1u))
		{
			Task.Delay(ForegroundLockAfterOpen).ContinueWith((Task _) => LockSetForegroundWindow(2u), TaskScheduler.Default);
		}
		using (StartProcess(target, handler))
		{
		}
		if (flag)
		{
			Log.Info("Open", "opened " + Describe(target) + "; left behind: the user is typing in the composer");
			return;
		}
		Task.Run(() => BringOpenedWindowForwardAsync(target, (handler == null) ? null : Path.GetFileName(handler), before));
	}

	public static bool TryStart(string target, string area)
	{
		try
		{
			Start(target);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn(area, "open " + Describe(target) + " failed: " + ex.Message);
			return false;
		}
	}

	private static bool IsUserTyping()
	{
		try
		{
			return UserIsTypingInHeyClicky?.Invoke() ?? false;
		}
		catch
		{
			return false;
		}
	}

	private static Process? StartProcess(string target, string? handler)
	{
		return Process.Start(new ProcessStartInfo(target)
		{
			UseShellExecute = true
		});
	}

	private static async Task BringOpenedWindowForwardAsync(string target, string? handlerExeName, IReadOnlyList<ShellOpenRules.TopWindow> before)
	{
		try
		{
			int self = Environment.ProcessId;
			Stopwatch watch = Stopwatch.StartNew();
			bool flag;
			do
			{
				await Task.Delay(PollInterval).ConfigureAwait(continueOnCapturedContext: false);
				if (IsUserTyping())
				{
					Log.Info("Open", Describe(target) + ": left behind, the user started typing in the composer");
					return;
				}
				flag = watch.Elapsed >= FindDeadline;
				List<ShellOpenRules.TopWindow> now = TopLevelWindows();
				ShellOpenRules.TopWindow? topWindow = ShellOpenRules.PickOpenedWindow(before, now, handlerExeName, self, flag);
				if (topWindow.HasValue)
				{
					ShellOpenRules.TopWindow window = topWindow.GetValueOrDefault();
					ShellOpenRules.TopWindow topWindow2 = before.FirstOrDefault((ShellOpenRules.TopWindow w) => w.Handle == window.Handle);
					string value = ((topWindow2.Handle == 0) ? "a new window" : ((topWindow2.Title != window.Title) ? "its title changed" : "its frontmost window"));
					if (GetForegroundWindow() == window.Handle)
					{
						Log.Info("Open", $"{Describe(target)}: {window.ExeName} ({value}) is already in front");
					}
					else
					{
						string value2 = BringToFront(window.Handle);
						Log.Info("Open", $"{Describe(target)}: {window.ExeName} ({value}, after {watch.Elapsed.TotalSeconds:0.0} s) brought forward: {value2}");
					}
					return;
				}
			}
			while (!flag);
			Log.Info("Open", Describe(target) + ": no window of " + (handlerExeName ?? "its app") + " showed up to bring forward");
		}
		catch (Exception ex)
		{
			Log.Warn("Open", "bringing " + Describe(target) + " forward failed: " + ex.Message);
		}
	}

	private static string BringToFront(nint window)
	{
		PeekMessageW(out var _, 0, 0u, 0u, 0u);
		if (IsIconic(window))
		{
			ShowWindow(window, 9);
		}
		if (SetForegroundWindow(window) && GetForegroundWindow() == window)
		{
			return "yes";
		}
		nint foregroundWindow = GetForegroundWindow();
		uint processId;
		uint num = ((foregroundWindow != 0) ? GetWindowThreadProcessId(foregroundWindow, out processId) : 0u);
		uint currentThreadId = GetCurrentThreadId();
		if (num != 0 && num != currentThreadId && AttachThreadInput(currentThreadId, num, doAttach: true))
		{
			try
			{
				BringWindowToTop(window);
				SetForegroundWindow(window);
			}
			finally
			{
				AttachThreadInput(currentThreadId, num, doAttach: false);
			}
			if (GetForegroundWindow() == window)
			{
				return "yes (attached to the foreground input)";
			}
		}
		TapAlt();
		TapAlt();
		SetForegroundWindow(window);
		if (GetForegroundWindow() != window)
		{
			return "no, Windows refused";
		}
		return "yes (Alt taps)";
	}

	private static void TapAlt()
	{
		INPUT[] array = new INPUT[2]
		{
			new INPUT
			{
				type = 1u,
				ki = new KEYBDINPUT
				{
					wVk = 18,
					dwExtraInfo = 1212369995
				}
			},
			new INPUT
			{
				type = 1u,
				ki = new KEYBDINPUT
				{
					wVk = 18,
					dwFlags = 2u,
					dwExtraInfo = 1212369995
				}
			}
		};
		SendInput((uint)array.Length, array, Marshal.SizeOf<INPUT>());
	}

	private static string Describe(string target)
	{
		if (!ShellOpenRules.LooksLikeUri(target))
		{
			string fileName = Path.GetFileName(target.TrimEnd('\\', '/'));
			if (fileName == null || fileName.Length <= 0)
			{
				return "a folder";
			}
			return fileName;
		}
		if (!Uri.TryCreate(target, UriKind.Absolute, out Uri result))
		{
			return "a link";
		}
		return result.Scheme + ":" + (string.IsNullOrEmpty(result.Host) ? "" : ("//" + result.Host));
	}

	public static string? HandlerExecutable(string target)
	{
		try
		{
			bool isDirectory = !ShellOpenRules.LooksLikeUri(target) && Directory.Exists(target);
			var (text, flag) = ShellOpenRules.AssociationKey(target, isDirectory);
			if (text == null)
			{
				return null;
			}
			if (text == "Folder")
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
			}
			uint flags = (uint)(0x420 | (flag ? 4096 : 0));
			string[] array = new string[2] { "open", null };
			foreach (string extra in array)
			{
				char[] array2 = new char[1024];
				uint length = (uint)array2.Length;
				if (AssocQueryStringW(flags, 2u, text, extra, array2, ref length) == 0)
				{
					string text2 = new string(array2, 0, (int)Math.Min(length, (uint)array2.Length)).TrimEnd('\0');
					if (text2.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(text2))
					{
						return text2;
					}
				}
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static List<ShellOpenRules.TopWindow> TopLevelWindows()
	{
		List<ShellOpenRules.TopWindow> windows = new List<ShellOpenRules.TopWindow>();
		Dictionary<uint, string> exeByProcess = new Dictionary<uint, string>();
		char[] title = new char[256];
		EnumWindows(delegate(nint hwnd, nint _)
		{
			if (!IsWindowVisible(hwnd) || GetWindow(hwnd, 4u) != 0)
			{
				return true;
			}
			if (((ulong)GetWindowLongPtrW(hwnd, -20) & 0x80uL) != 0L)
			{
				return true;
			}
			if (DwmGetWindowAttribute(hwnd, 14, out var value, 4) == 0 && value != 0)
			{
				return true;
			}
			GetWindowThreadProcessId(hwnd, out var processId);
			if (!exeByProcess.TryGetValue(processId, out string value2))
			{
				value2 = (exeByProcess[processId] = ProcessExeName(processId));
			}
			int windowTextW = GetWindowTextW(hwnd, title, title.Length);
			windows.Add(new ShellOpenRules.TopWindow(hwnd, (int)processId, value2, new string(title, 0, Math.Max(0, windowTextW)), IsIconic(hwnd)));
			return true;
		}, 0);
		return windows;
	}

	private static string ProcessExeName(uint pid)
	{
		nint num = OpenProcess(4096u, inherit: false, pid);
		if (num == 0)
		{
			return "";
		}
		try
		{
			char[] array = new char[1024];
			int size = array.Length;
			return QueryFullProcessImageNameW(num, 0u, array, ref size) ? Path.GetFileName(new string(array, 0, size)) : "";
		}
		finally
		{
			CloseHandle(num);
		}
	}

	[DllImport("user32.dll")]
	private static extern bool AllowSetForegroundWindow(uint processId);

	[DllImport("user32.dll")]
	private static extern bool LockSetForegroundWindow(uint code);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool PeekMessageW(out MSG msg, nint hwnd, uint min, uint max, uint remove);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool BringWindowToTop(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hwnd, int command);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	private static extern nint GetWindow(nint hwnd, uint command);

	[DllImport("user32.dll")]
	private static extern nint GetWindowLongPtrW(nint hwnd, int index);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

	[DllImport("user32.dll")]
	private static extern bool AttachThreadInput(uint attach, uint attachTo, bool doAttach);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowTextW(nint hwnd, [Out] char[] text, int max);

	[DllImport("user32.dll")]
	private static extern uint SendInput(uint count, INPUT[] inputs, int size);

	[DllImport("kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("kernel32.dll")]
	private static extern nint OpenProcess(uint access, bool inherit, uint processId);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern bool QueryFullProcessImageNameW(nint process, uint flags, [Out] char[] name, ref int size);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(nint handle);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);

	[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
	private static extern int AssocQueryStringW(uint flags, uint str, string assoc, string? extra, [Out] char[]? output, ref uint length);
}
