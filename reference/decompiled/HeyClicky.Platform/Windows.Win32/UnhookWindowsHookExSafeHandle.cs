using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal class UnhookWindowsHookExSafeHandle : SafeHandle
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

	internal UnhookWindowsHookExSafeHandle()
		: base(INVALID_HANDLE_VALUE, ownsHandle: true)
	{
	}

	internal UnhookWindowsHookExSafeHandle(nint preexistingHandle, bool ownsHandle = true)
		: base(INVALID_HANDLE_VALUE, ownsHandle)
	{
		SetHandle(preexistingHandle);
	}

	protected override bool ReleaseHandle()
	{
		return PInvoke.UnhookWindowsHookEx((HHOOK)handle);
	}
}
