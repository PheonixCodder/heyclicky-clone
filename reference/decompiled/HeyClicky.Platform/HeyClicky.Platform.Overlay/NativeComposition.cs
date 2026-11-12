using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace HeyClicky.Platform.Overlay;

internal static class NativeComposition
{
	[DllImport("dcomp.dll", ExactSpelling = true)]
	[LibraryImport("dcomp.dll")]
	public unsafe static extern uint DCompositionWaitForCompositorClock(uint count, nint* handles, uint timeoutInMs);

	[LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static nint CreateWaitableTimerExW(nint attributes, string? name, uint flags, uint desiredAccess)
	{
		nint result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(name))
		{
			void* _name_native = ptr;
			result = __PInvoke(attributes, (ushort*)_name_native, flags, desiredAccess);
		}
		return result;
		[DllImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", ExactSpelling = true)]
		static extern unsafe nint __PInvoke(nint __attributes_native, ushort* __name_native, uint __flags_native, uint __desiredAccess_native);
	}

	[LibraryImport("kernel32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	public unsafe static bool SetWaitableTimerEx(nint timer, long* dueTime, int period, nint completionRoutine, nint argument, void* wakeContext, uint tolerableDelay)
	{
		return __PInvoke(timer, dueTime, period, completionRoutine, argument, wakeContext, tolerableDelay) != 0;
		[DllImport("kernel32.dll", EntryPoint = "SetWaitableTimerEx", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __timer_native, long* __dueTime_native, int __period_native, nint __completionRoutine_native, nint __argument_native, void* __wakeContext_native, uint __tolerableDelay_native);
	}

	[DllImport("kernel32.dll", ExactSpelling = true)]
	[LibraryImport("kernel32.dll")]
	public static extern uint WaitForSingleObject(nint handle, uint milliseconds);

	[LibraryImport("dcomp.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static int DCompositionCreateDevice2(nint renderingDevice, in Guid iid, out nint compositionDevice)
	{
		compositionDevice = 0;
		int result;
		fixed (nint* _compositionDevice_native = &compositionDevice)
		{
			fixed (Guid* _iid_native = &iid)
			{
				result = __PInvoke(renderingDevice, _iid_native, _compositionDevice_native);
			}
		}
		return result;
		[DllImport("dcomp.dll", EntryPoint = "DCompositionCreateDevice2", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __renderingDevice_native, Guid* __iid_native, nint* __compositionDevice_native);
	}

	[LibraryImport("dcomp.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "9.0.15.2218")]
	[SkipLocalsInit]
	public unsafe static int DCompositionCreateDevice3(nint renderingDevice, in Guid iid, out nint compositionDevice)
	{
		compositionDevice = 0;
		int result;
		fixed (nint* _compositionDevice_native = &compositionDevice)
		{
			fixed (Guid* _iid_native = &iid)
			{
				result = __PInvoke(renderingDevice, _iid_native, _compositionDevice_native);
			}
		}
		return result;
		[DllImport("dcomp.dll", EntryPoint = "DCompositionCreateDevice3", ExactSpelling = true)]
		static extern unsafe int __PInvoke(nint __renderingDevice_native, Guid* __iid_native, nint* __compositionDevice_native);
	}
}
