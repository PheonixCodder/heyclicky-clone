using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace HeyClicky.Platform.Input;

internal static class NativeHooks
{
	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static nint SetWindowsHookExW(int idHook, delegate* unmanaged<int, nuint, nint, nint> lpfn, nint hmod, uint dwThreadId)
	{
		Marshal.SetLastSystemError(0);
		nint result = __PInvoke(idHook, lpfn, hmod, dwThreadId);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", ExactSpelling = true)]
		static extern unsafe nint __PInvoke(int __idHook_native, delegate* unmanaged<int, nuint, nint, nint> __lpfn_native, nint __hmod_native, uint __dwThreadId_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static bool UnhookWindowsHookEx(nint hhk)
	{
		Marshal.SetLastSystemError(0);
		int num = __PInvoke(hhk);
		int lastSystemError = Marshal.GetLastSystemError();
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "UnhookWindowsHookEx", ExactSpelling = true)]
		static extern int __PInvoke(nint __hhk_native);
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	public static extern nint CallNextHookEx(nint hhk, int nCode, nuint wParam, nint lParam);

	[LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static nint GetModuleHandleW(string? lpModuleName)
	{
		nint result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(lpModuleName))
		{
			void* _lpModuleName_native = ptr;
			result = __PInvoke((ushort*)_lpModuleName_native);
		}
		return result;
		[DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", ExactSpelling = true)]
		static extern unsafe nint __PInvoke(ushort* __lpModuleName_native);
	}

	[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static nint CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param)
	{
		nint result;
		int lastSystemError;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(windowName))
		{
			void* _windowName_native = ptr;
			fixed (char* ptr2 = &Utf16StringMarshaller.GetPinnableReference(className))
			{
				void* _className_native = ptr2;
				Marshal.SetLastSystemError(0);
				result = __PInvoke(exStyle, (ushort*)_className_native, (ushort*)_windowName_native, style, x, y, width, height, parent, menu, instance, param);
				lastSystemError = Marshal.GetLastSystemError();
			}
		}
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "CreateWindowExW", ExactSpelling = true)]
		static extern unsafe nint __PInvoke(uint __exStyle_native, ushort* __className_native, ushort* __windowName_native, uint __style_native, int __x_native, int __y_native, int __width_native, int __height_native, nint __parent_native, nint __menu_native, nint __instance_native, nint __param_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static bool DestroyWindow(nint hwnd)
	{
		return __PInvoke(hwnd) != 0;
		[DllImport("user32.dll", EntryPoint = "DestroyWindow", ExactSpelling = true)]
		static extern int __PInvoke(nint __hwnd_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	public unsafe static bool RegisterRawInputDevices(RawInputDevice* devices, uint count, uint size)
	{
		Marshal.SetLastSystemError(0);
		int num = __PInvoke(devices, count, size);
		int lastSystemError = Marshal.GetLastSystemError();
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "RegisterRawInputDevices", ExactSpelling = true)]
		static extern unsafe int __PInvoke(RawInputDevice* __devices_native, uint __count_native, uint __size_native);
	}

	public unsafe static bool RegisterRawInputDevices(System.ReadOnlySpan<RawInputDevice> devices, uint count, uint size)
	{
		fixed (RawInputDevice* p = devices)
		{
			return RegisterRawInputDevices(p, count, size);
		}
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	public static extern nuint SetTimer(nint hwnd, nuint id, uint milliseconds, nint callback);

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static bool KillTimer(nint hwnd, nuint id)
	{
		return __PInvoke(hwnd, id) != 0;
		[DllImport("user32.dll", EntryPoint = "KillTimer", ExactSpelling = true)]
		static extern int __PInvoke(nint __hwnd_native, nuint __id_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static uint GetRawInputData(nint rawInput, uint command, void* data, ref uint size, uint headerSize)
	{
		uint result;
		fixed (uint* _size_native = &size)
		{
			result = __PInvoke(rawInput, command, data, _size_native, headerSize);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "GetRawInputData", ExactSpelling = true)]
		static extern unsafe uint __PInvoke(nint __rawInput_native, uint __command_native, void* __data_native, uint* __size_native, uint __headerSize_native);
	}
}
