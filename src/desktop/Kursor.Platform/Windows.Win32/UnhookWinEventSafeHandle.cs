using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Accessibility;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal class UnhookWinEventSafeHandle : SafeHandle
{
	private static readonly nint INVALID_HANDLE_VALUE = new IntPtr(-1L);

	public override bool IsInvalid
	{
		get
		{
			if (((IntPtr)handle).ToInt64() != -1)
			{
				return ((IntPtr)handle).ToInt64() == 0;
			}
			return true;
		}
	}

	internal UnhookWinEventSafeHandle()
		: base(INVALID_HANDLE_VALUE, ownsHandle: true)
	{
	}

	internal UnhookWinEventSafeHandle(nint preexistingHandle, bool ownsHandle = true)
		: base(INVALID_HANDLE_VALUE, ownsHandle)
	{
		SetHandle(preexistingHandle);
	}

	protected override bool ReleaseHandle()
	{
		return PInvoke.UnhookWinEvent((HWINEVENTHOOK)handle);
	}
}
