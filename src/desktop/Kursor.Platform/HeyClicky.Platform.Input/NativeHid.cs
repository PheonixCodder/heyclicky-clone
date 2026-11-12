using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HeyClicky.Platform.Input;

internal static class NativeHid
{
	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static uint GetRawInputDeviceInfoW(nint device, uint command, void* data, ref uint size)
	{
		uint result;
		int lastSystemError;
		fixed (uint* _size_native = &size)
		{
			Marshal.SetLastSystemError(0);
			result = __PInvoke(device, command, data, _size_native);
			lastSystemError = Marshal.GetLastSystemError();
		}
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", ExactSpelling = true)]
		static extern unsafe uint __PInvoke(nint __device_native, uint __command_native, void* __data_native, uint* __size_native);
	}

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static uint GetRawInputDeviceList(void* list, ref uint count, uint size)
	{
		uint result;
		int lastSystemError;
		fixed (uint* _count_native = &count)
		{
			Marshal.SetLastSystemError(0);
			result = __PInvoke(list, _count_native, size);
			lastSystemError = Marshal.GetLastSystemError();
		}
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "GetRawInputDeviceList", ExactSpelling = true)]
		static extern unsafe uint __PInvoke(void* __list_native, uint* __count_native, uint __size_native);
	}
}
