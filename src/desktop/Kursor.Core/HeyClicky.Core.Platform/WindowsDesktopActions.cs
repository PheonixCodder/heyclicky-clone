using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Platform;

public sealed class WindowsDesktopActions : IDesktopActions
{
	private struct KeyboardInput
	{
		public ushort VirtualKey;

		public ushort ScanCode;

		public uint Flags;

		public uint Time;

		public nint ExtraInfo;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct Input
	{
		[FieldOffset(0)]
		public uint Type;

		[FieldOffset(8)]
		public KeyboardInput Keyboard;

		[FieldOffset(8)]
		public MousePadding Padding;
	}

	private struct MousePadding
	{
		public int Dx;

		public int Dy;

		public uint MouseData;

		public uint Flags;

		public uint Time;

		public nint ExtraInfo;
	}

	private const uint CfUnicodeText = 13u;

	private const uint GmemMoveable = 2u;

	private const uint InputKeyboard = 1u;

	private const uint KeyEventKeyUp = 2u;

	private const ushort VirtualKeyControl = 17;

	private const ushort VirtualKeyMenu = 18;

	private const ushort VirtualKeyV = 86;

	public bool OpenUrl(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result) || (result.Scheme != Uri.UriSchemeHttps && result.Scheme != Uri.UriSchemeHttp) || string.IsNullOrEmpty(result.Host))
		{
			return false;
		}
		try
		{
			ShellOpen.Start(result.AbsoluteUri);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("Desktop", "open url failed: " + ex.Message);
			return false;
		}
	}

	public bool CopyToClipboard(string text)
	{
		return RunOnStaThread(() => SetClipboardText(text));
	}

	public async Task<string> TypeIntoFocusedFieldAsync(string text)
	{
		if (GetForegroundWindow() == IntPtr.Zero)
		{
			return CopyToClipboard(text) ? "copied_to_clipboard" : "refused";
		}
		string previous = RunOnStaThread(GetClipboardText);
		if (!CopyToClipboard(text))
		{
			return "refused";
		}
		SendKeys(18, keyUp: true);
		SendKeys(17, keyUp: false);
		SendKeys(86, keyUp: false);
		SendKeys(86, keyUp: true);
		SendKeys(17, keyUp: true);
		await Task.Delay(400).ConfigureAwait(continueOnCapturedContext: false);
		if (previous != null)
		{
			RunOnStaThread(() => SetClipboardText(previous));
		}
		return "typed";
	}

	public (string? DisplayName, string? ProcessName) FrontApplication()
	{
		try
		{
			nint foregroundWindow = GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero)
			{
				return (DisplayName: null, ProcessName: null);
			}
			GetWindowThreadProcessId(foregroundWindow, out var processId);
			using Process process = Process.GetProcessById((int)processId);
			string text = null;
			try
			{
				text = process.MainModule?.FileVersionInfo.FileDescription;
			}
			catch
			{
			}
			string processName = process.ProcessName;
			return (DisplayName: string.IsNullOrWhiteSpace(text) ? processName : text.Trim(), ProcessName: processName);
		}
		catch
		{
			return (DisplayName: null, ProcessName: null);
		}
	}

	private static T RunOnStaThread<T>(Func<T> work)
	{
		if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
		{
			return work();
		}
		T result = default(T);
		Thread thread = new Thread((ThreadStart)delegate
		{
			result = work();
		});
		thread.IsBackground = true;
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		return result;
	}

	private unsafe static bool SetClipboardText(string text)
	{
		for (int i = 0; i < 10; i++)
		{
			if (OpenClipboard(IntPtr.Zero))
			{
				break;
			}
			if (i == 9)
			{
				return false;
			}
			Thread.Sleep(20);
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
			if (num3 == IntPtr.Zero)
			{
				GlobalFree(num2);
				return false;
			}
			Span<char> destination = new Span<char>((void*)num3, text.Length + 1);
			text.AsSpan().CopyTo(destination);
			destination[destination.Length - 1] = '\0';
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

	private static string? GetClipboardText()
	{
		if (!IsClipboardFormatAvailable(13u) || !OpenClipboard(IntPtr.Zero))
		{
			return null;
		}
		try
		{
			nint clipboardData = GetClipboardData(13u);
			if (clipboardData == IntPtr.Zero)
			{
				return null;
			}
			nint num = GlobalLock(clipboardData);
			if (num == IntPtr.Zero)
			{
				return null;
			}
			try
			{
				return Marshal.PtrToStringUni(num);
			}
			finally
			{
				GlobalUnlock(clipboardData);
			}
		}
		finally
		{
			CloseClipboard();
		}
	}

	private static void SendKeys(ushort virtualKey, bool keyUp)
	{
		Input input = default(Input);
		input.Type = 1u;
		input.Keyboard = new KeyboardInput
		{
			VirtualKey = virtualKey,
			Flags = (keyUp ? 2u : 0u)
		};
		Input input2 = input;
		SendInput(1u, ref input2, Marshal.SizeOf<Input>());
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	private unsafe static uint GetWindowThreadProcessId(nint window, out uint processId)
	{
		processId = 0u;
		uint result;
		fixed (uint* _processId_native = &processId)
		{
			result = __PInvoke(window, _processId_native);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", ExactSpelling = true)]
		static extern unsafe uint __PInvoke(nint __window_native, uint* __processId_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool OpenClipboard(nint owner)
	{
		Marshal.SetLastSystemError(0);
		int num = __PInvoke(owner);
		int lastSystemError = Marshal.GetLastSystemError();
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "OpenClipboard", ExactSpelling = true)]
		static extern int __PInvoke(nint __owner_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool CloseClipboard()
	{
		return __PInvoke() != 0;
		[DllImport("user32.dll", EntryPoint = "CloseClipboard", ExactSpelling = true)]
		static extern int __PInvoke();
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool EmptyClipboard()
	{
		return __PInvoke() != 0;
		[DllImport("user32.dll", EntryPoint = "EmptyClipboard", ExactSpelling = true)]
		static extern int __PInvoke();
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	private static extern nint SetClipboardData(uint format, nint memory);

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	private static extern nint GetClipboardData(uint format);

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool IsClipboardFormatAvailable(uint format)
	{
		return __PInvoke(format) != 0;
		[DllImport("user32.dll", EntryPoint = "IsClipboardFormatAvailable", ExactSpelling = true)]
		static extern int __PInvoke(uint __format_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	private unsafe static uint SendInput(uint count, ref Input input, int size)
	{
		uint result;
		fixed (Input* _input_native = &input)
		{
			result = __PInvoke(count, _input_native, size);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "SendInput", ExactSpelling = true)]
		static extern unsafe uint __PInvoke(uint __count_native, Input* __input_native, int __size_native);
	}

	[DllImport("kernel32.dll", ExactSpelling = true)]
	[LibraryImport("kernel32.dll")]
	private static extern nint GlobalAlloc(uint flags, nuint bytes);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	[LibraryImport("kernel32.dll")]
	private static extern nint GlobalLock(nint memory);

	[LibraryImport("kernel32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool GlobalUnlock(nint memory)
	{
		return __PInvoke(memory) != 0;
		[DllImport("kernel32.dll", EntryPoint = "GlobalUnlock", ExactSpelling = true)]
		static extern int __PInvoke(nint __memory_native);
	}

	[DllImport("kernel32.dll", ExactSpelling = true)]
	[LibraryImport("kernel32.dll")]
	private static extern nint GlobalFree(nint memory);
}
