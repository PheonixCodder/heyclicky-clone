using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Win32;

namespace HeyClicky.Platform.Win32;

public static class QuietContext
{
	public enum NotificationState
	{
		Unknown,
		NotPresent,
		Busy,
		RunningD3DFullScreen,
		PresentationMode,
		AcceptsNotifications,
		QuietTime,
		App
	}

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	private struct LASTINPUTINFO
	{
		public uint cbSize;

		public uint dwTime;
	}

	private const ulong WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED = 973628810547108981uL;

	private const string MicrophoneConsentStore = "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone";

	private static readonly HashSet<string> RecordingExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"audacity", "obs64", "obs32", "obs", "reaper", "reaper64", "fl", "fl64", "protools", "ocenaudio",
		"adobe audition", "bitwig studio", "studio one", "streamlabs obs", "streamlabs desktop", "voicemeeter", "voicemeeterpro", "voicemeeter8", "voicemeeter8x64", "wavelab",
		"sound forge pro"
	};

	private static readonly string[] RecordingExecutablePrefixes = new string[6] { "ableton live", "cubase", "nuendo", "izotope rx", "camtasia", "wavelab " };

	private static readonly HashSet<string> RecordingPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Microsoft.WindowsSoundRecorder", "Clipchamp.Clipchamp", "Microsoft.ScreenSketch" };

	public static readonly bool IgnoredForTests = Environment.GetEnvironmentVariable("HEYCLICKY_IGNORE_QUIET_CONTEXT") == "1";

	[LibraryImport("shell32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	private unsafe static int SHQueryUserNotificationState(out int state)
	{
		state = 0;
		int result;
		fixed (int* _state_native = &state)
		{
			result = __PInvoke(_state_native);
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHQueryUserNotificationState", ExactSpelling = true)]
		static extern unsafe int __PInvoke(int* __state_native);
	}

	public static NotificationState QueryNotificationState()
	{
		try
		{
			int state;
			return (SHQueryUserNotificationState(out state) == 0) ? ((NotificationState)state) : NotificationState.Unknown;
		}
		catch
		{
			return NotificationState.Unknown;
		}
	}

	public static bool ShellReportsFullscreen()
	{
		NotificationState notificationState = QueryNotificationState();
		if ((uint)(notificationState - 3) <= 1u || notificationState == NotificationState.App)
		{
			return true;
		}
		return false;
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	private static extern nint GetShellWindow();

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	private static extern nint GetDesktopWindow();

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool GetWindowRect(nint hwnd, out RECT rect)
	{
		rect = default(RECT);
		int num;
		fixed (RECT* _rect_native = &rect)
		{
			num = __PInvoke(hwnd, _rect_native);
		}
		return num != 0;
		[DllImport("user32.dll", EntryPoint = "GetWindowRect", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __hwnd_native, RECT* __rect_native);
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint hwnd, uint flags);

	[LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool GetMonitorInfo(nint monitor, ref MONITORINFO info)
	{
		int num;
		fixed (MONITORINFO* _info_native = &info)
		{
			num = __PInvoke(monitor, _info_native);
		}
		return num != 0;
		[DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __monitor_native, MONITORINFO* __info_native);
	}

	[LibraryImport("user32.dll", EntryPoint = "GetClassNameW", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	private unsafe static int GetClassName(nint hwnd, [Out] char[] name, int max)
	{
		int result;
		fixed (char* ptr = &ArrayMarshaller<char, char>.ManagedToUnmanagedIn.GetPinnableReference(name))
		{
			void* _name_native = ptr;
			result = __PInvoke(hwnd, (char*)_name_native, max);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "GetClassNameW", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __hwnd_native, char* __name_native, int __max_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	private unsafe static uint GetWindowThreadProcessId(nint hwnd, out uint processId)
	{
		processId = 0u;
		uint result;
		fixed (uint* _processId_native = &processId)
		{
			result = __PInvoke(hwnd, _processId_native);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", ExactSpelling = true)]
		static extern unsafe uint __PInvoke(nint __hwnd_native, uint* __processId_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool IsWindowVisible(nint hwnd)
	{
		return __PInvoke(hwnd) != 0;
		[DllImport("user32.dll", EntryPoint = "IsWindowVisible", ExactSpelling = true)]
		static extern int __PInvoke(nint __hwnd_native);
	}

	public static nint FullscreenForegroundMonitor()
	{
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == 0 || foregroundWindow == GetShellWindow() || foregroundWindow == GetDesktopWindow() || !IsWindowVisible(foregroundWindow))
		{
			return 0;
		}
		GetWindowThreadProcessId(foregroundWindow, out var processId);
		if (processId == Environment.ProcessId)
		{
			return 0;
		}
		char[] array = new char[64];
		int className = GetClassName(foregroundWindow, array, array.Length);
		bool flag;
		switch (new string(array, 0, Math.Max(0, className)))
		{
		case "Progman":
		case "WorkerW":
		case "Shell_TrayWnd":
		case "Shell_SecondaryTrayWnd":
		case "Windows.UI.Core.CoreWindow":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return 0;
		}
		if (!GetWindowRect(foregroundWindow, out var rect))
		{
			return 0;
		}
		nint num = MonitorFromWindow(foregroundWindow, 2u);
		MONITORINFO mONITORINFO = default(MONITORINFO);
		mONITORINFO.cbSize = Marshal.SizeOf<MONITORINFO>();
		MONITORINFO info = mONITORINFO;
		if (!GetMonitorInfo(num, ref info))
		{
			return 0;
		}
		RECT rcMonitor = info.rcMonitor;
		if (rect.Left > rcMonitor.Left || rect.Top > rcMonitor.Top || rect.Right < rcMonitor.Right || rect.Bottom < rcMonitor.Bottom)
		{
			return 0;
		}
		return num;
	}

	public static bool IsFullscreenOn(nint monitor)
	{
		nint num = FullscreenForegroundMonitor();
		if (num != 0)
		{
			if (monitor != 0)
			{
				return num == monitor;
			}
			return true;
		}
		if (!ShellReportsFullscreen())
		{
			return false;
		}
		if (monitor == 0)
		{
			return true;
		}
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow != 0)
		{
			return MonitorFromWindow(foregroundWindow, 2u) == monitor;
		}
		return false;
	}

	[LibraryImport("ntdll.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	private unsafe static int NtQueryWnfStateData(ulong* stateName, nint typeId, nint explicitScope, out uint changeStamp, void* buffer, ref uint bufferSize)
	{
		changeStamp = 0u;
		int result;
		fixed (uint* _bufferSize_native = &bufferSize)
		{
			fixed (uint* _changeStamp_native = &changeStamp)
			{
				result = __PInvoke(stateName, typeId, explicitScope, _changeStamp_native, buffer, _bufferSize_native);
			}
		}
		return result;
		[DllImport("ntdll.dll", EntryPoint = "NtQueryWnfStateData", ExactSpelling = true)]
		static extern unsafe int __PInvoke(ulong* __stateName_native, nint __typeId_native, nint __explicitScope_native, uint* __changeStamp_native, void* __buffer_native, uint* __bufferSize_native);
	}

	public unsafe static bool IsDoNotDisturbOn()
	{
		try
		{
			ulong num = 973628810547108981uL;
			uint num2 = 0u;
			uint bufferSize = 4u;
			uint changeStamp;
			return NtQueryWnfStateData(&num, 0, 0, out changeStamp, &num2, ref bufferSize) == 0 && bufferSize >= 4 && num2 != 0;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsAnotherAppUsingMicrophone()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone");
			if (registryKey == null)
			{
				return false;
			}
			string value = (Environment.ProcessPath ?? "").Replace('\\', '#');
			string[] subKeyNames = registryKey.GetSubKeyNames();
			foreach (string text in subKeyNames)
			{
				if (text == "NonPackaged")
				{
					using RegistryKey registryKey2 = registryKey.OpenSubKey(text);
					if (registryKey2 == null)
					{
						continue;
					}
					string[] subKeyNames2 = registryKey2.GetSubKeyNames();
					foreach (string text2 in subKeyNames2)
					{
						if (text2.Equals(value, StringComparison.OrdinalIgnoreCase) || text2.Contains("HeyClicky", StringComparison.OrdinalIgnoreCase) || IsRecordingApp(text2, packaged: false))
						{
							continue;
						}
						using RegistryKey key2 = registryKey2.OpenSubKey(text2);
						if (InUse(key2))
						{
							return true;
						}
					}
				}
				else
				{
					if (IsRecordingApp(text, packaged: true))
					{
						continue;
					}
					using RegistryKey key3 = registryKey.OpenSubKey(text);
					if (InUse(key3))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
		static bool InUse(RegistryKey? key)
		{
			if (key?.GetValue("LastUsedTimeStart") is long num && num != 0L && key.GetValue("LastUsedTimeStop") is long num2)
			{
				return num2 == 0;
			}
			return false;
		}
	}

	public static bool IsRecordingApp(string consentStoreKeyName, bool packaged)
	{
		if (packaged)
		{
			int num = consentStoreKeyName.LastIndexOf('_');
			return RecordingPackages.Contains((num > 0) ? consentStoreKeyName.Substring(0, num) : consentStoreKeyName);
		}
		string text = consentStoreKeyName;
		int num2 = Math.Max(consentStoreKeyName.LastIndexOf('#'), consentStoreKeyName.LastIndexOf('\\')) + 1;
		string text2 = text.Substring(num2, text.Length - num2);
		string text3;
		if (!text2.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			text3 = text2;
		}
		else
		{
			text = text2;
			text3 = text.Substring(0, text.Length - 4);
		}
		string name = text3;
		if (!RecordingExecutables.Contains(name))
		{
			return RecordingExecutablePrefixes.Any((string prefix) => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
		}
		return true;
	}

	public static bool IsQuiet()
	{
		if (!IgnoredForTests)
		{
			if (!IsAnotherAppUsingMicrophone())
			{
				return IsDoNotDisturbOn();
			}
			return true;
		}
		return false;
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool GetLastInputInfo(ref LASTINPUTINFO info)
	{
		int num;
		fixed (LASTINPUTINFO* _info_native = &info)
		{
			num = __PInvoke(_info_native);
		}
		return num != 0;
		[DllImport("user32.dll", EntryPoint = "GetLastInputInfo", ExactSpelling = true)]
		static extern unsafe int __PInvoke(LASTINPUTINFO* __info_native);
	}

	public static double SecondsSinceLastInput()
	{
		LASTINPUTINFO lASTINPUTINFO = default(LASTINPUTINFO);
		lASTINPUTINFO.cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>();
		LASTINPUTINFO info = lASTINPUTINFO;
		if (!GetLastInputInfo(ref info))
		{
			return 0.0;
		}
		return (double)((uint)Environment.TickCount - info.dwTime) / 1000.0;
	}

	public static QuietReason Current()
	{
		if (IgnoredForTests)
		{
			return QuietReason.None;
		}
		NotificationState notificationState = QueryNotificationState();
		if (!IsDoNotDisturbOn())
		{
			switch (notificationState)
			{
			case NotificationState.QuietTime:
				break;
			case NotificationState.PresentationMode:
				return QuietReason.Presentation;
			default:
				if (IsFullscreenOn(0))
				{
					return QuietReason.FullScreen;
				}
				if (!IsAnotherAppUsingMicrophone())
				{
					return QuietReason.None;
				}
				return QuietReason.OnCall;
			}
		}
		return QuietReason.DoNotDisturb;
	}
}
