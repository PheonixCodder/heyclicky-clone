using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.System.Threading;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal class AvRevertMmThreadCharacteristicsSafeHandle : SafeHandle
{
	private static readonly nint INVALID_HANDLE_VALUE = new IntPtr(0L);

	public override bool IsInvalid => ((IntPtr)handle).ToInt64() == 0;

	internal AvRevertMmThreadCharacteristicsSafeHandle()
		: base(INVALID_HANDLE_VALUE, ownsHandle: true)
	{
	}

	internal AvRevertMmThreadCharacteristicsSafeHandle(nint preexistingHandle, bool ownsHandle = true)
		: base(INVALID_HANDLE_VALUE, ownsHandle)
	{
		SetHandle(preexistingHandle);
	}

	protected override bool ReleaseHandle()
	{
		return PInvoke.AvRevertMmThreadCharacteristics((AVRT_TASK_HANDLE)handle);
	}
}
