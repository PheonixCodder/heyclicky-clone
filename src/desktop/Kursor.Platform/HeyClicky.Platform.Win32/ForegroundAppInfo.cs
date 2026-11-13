using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Win32;

public static class ForegroundAppInfo
{
	private static readonly HashSet<string> ChromiumBrowsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "chrome", "msedge", "brave", "opera", "vivaldi", "arc", "chromium", "thorium" };

	private static readonly Dictionary<string, string> DescriptionCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static async Task<ForegroundApp?> CaptureAsync(bool includeBrowserUrl = true, CancellationToken ct = default(CancellationToken))
	{
		ForegroundApp foregroundApp = Capture();
		if ((object)foregroundApp == null || !includeBrowserUrl)
		{
			return foregroundApp;
		}
		return await WithBrowserUrlAsync(foregroundApp, ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static async Task<ForegroundApp?> CaptureUserAppAsync(bool includeBrowserUrl = true, CancellationToken ct = default(CancellationToken))
	{
		ForegroundApp foregroundApp = CaptureUserApp();
		if ((object)foregroundApp == null || !includeBrowserUrl)
		{
			return foregroundApp;
		}
		return await WithBrowserUrlAsync(foregroundApp, ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static ForegroundApp? CaptureUserApp()
	{
		HWND window = PInvoke.GetForegroundWindow();
		if (!window.IsNull && IsOwnWindow(window))
		{
			nint lastExternalWindow = ForegroundTracker.LastExternalWindow;
			if (lastExternalWindow != 0 && (bool)PInvoke.IsWindow(new HWND(lastExternalWindow)))
			{
				window = new HWND(lastExternalWindow);
			}
		}
		if (!window.IsNull)
		{
			return Capture(window);
		}
		return null;
	}

	private static async Task<ForegroundApp> WithBrowserUrlAsync(ForegroundApp app, CancellationToken ct)
	{
		bool isFirefox = app.ProcessName.Equals("firefox", StringComparison.OrdinalIgnoreCase);
		if (!isFirefox && !ChromiumBrowsers.Contains(app.ProcessName))
		{
			return app;
		}
		string text = await UIAutomationHelpers.RunAsync((IUIAutomation automation) => UIAutomationHelpers.ReadBrowserAddress(automation, app.WindowHandle, isFirefox), ct).ConfigureAwait(continueOnCapturedContext: false);
		return string.IsNullOrWhiteSpace(text) ? app : app with
		{
			BrowserUrl = text
		};
	}

	public static bool IsBrowser(string processName)
	{
		if (!processName.Equals("firefox", StringComparison.OrdinalIgnoreCase))
		{
			return ChromiumBrowsers.Contains(processName);
		}
		return true;
	}

	private unsafe static bool IsOwnWindow(HWND window)
	{
		uint num = default(uint);
		PInvoke.GetWindowThreadProcessId(window, &num);
		return num == (uint)Environment.ProcessId;
	}

	public static ForegroundApp? Capture()
	{
		HWND foregroundWindow = PInvoke.GetForegroundWindow();
		if (!foregroundWindow.IsNull)
		{
			return Capture(foregroundWindow);
		}
		return null;
	}

	internal unsafe static ForegroundApp? Capture(HWND window)
	{
		if (window.IsNull)
		{
			return null;
		}
		HWND ancestor = PInvoke.GetAncestor(window, GET_ANCESTOR_FLAGS.GA_ROOTOWNER);
		if (!ancestor.IsNull)
		{
			window = ancestor;
		}
		uint num = default(uint);
		PInvoke.GetWindowThreadProcessId(window, &num);
		string windowTitle = WindowTitle(window);
		string text = ProcessImagePath(num);
		string processName = ((text == null) ? $"pid{num}" : Path.GetFileNameWithoutExtension(text));
		return new ForegroundApp((nint)window, (int)num, processName, text, AppDisplayName(text, processName), windowTitle, null);
	}

	public static (string ProcessName, string AppName) DescribeProcess(int processId)
	{
		string text = ProcessImagePath((uint)processId);
		string text2 = ((text == null) ? $"pid{processId}" : Path.GetFileNameWithoutExtension(text));
		return (ProcessName: text2, AppName: AppDisplayName(text, text2));
	}

	public static bool IsBrowserProcess(string processName, out bool isFirefox)
	{
		isFirefox = processName.Equals("firefox", StringComparison.OrdinalIgnoreCase);
		if (!isFirefox)
		{
			return ChromiumBrowsers.Contains(processName);
		}
		return true;
	}

	private unsafe static string WindowTitle(HWND window)
	{
		int windowTextLength = PInvoke.GetWindowTextLength(window);
		if (windowTextLength <= 0)
		{
			return "";
		}
		Span<char> span = ((windowTextLength >= 1024) ? ((Span<char>)new char[windowTextLength + 1]) : stackalloc char[windowTextLength + 1]);
		Span<char> span2 = span;
		fixed (char* ptr = span2)
		{
			return new string(span2[..PInvoke.GetWindowText(window, ptr, span2.Length)]);
		}
	}

	private static string? ProcessImagePath(uint processId)
	{
		using SafeFileHandle safeFileHandle = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
		if (safeFileHandle.IsInvalid)
		{
			return null;
		}
		Span<char> lpExeName = stackalloc char[1024];
		uint lpdwSize = (uint)lpExeName.Length;
		if (!PInvoke.QueryFullProcessImageName(safeFileHandle, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, lpExeName, ref lpdwSize))
		{
			return null;
		}
		return new string(lpExeName.Slice(0, (int)lpdwSize));
	}

	private static string AppDisplayName(string? path, string processName)
	{
		if (path == null)
		{
			return processName;
		}
		lock (DescriptionCache)
		{
			if (DescriptionCache.TryGetValue(path, out string value))
			{
				return value;
			}
			string text = processName;
			try
			{
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
				text = ((!string.IsNullOrWhiteSpace(versionInfo.FileDescription)) ? versionInfo.FileDescription.Trim() : ((!string.IsNullOrWhiteSpace(versionInfo.ProductName)) ? versionInfo.ProductName.Trim() : processName));
			}
			catch
			{
			}
			DescriptionCache[path] = text;
			return text;
		}
	}
}
