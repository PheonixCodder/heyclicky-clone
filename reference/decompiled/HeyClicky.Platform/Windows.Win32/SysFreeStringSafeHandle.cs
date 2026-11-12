using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal class SysFreeStringSafeHandle : SafeHandle
{
	private static readonly nint INVALID_HANDLE_VALUE = new IntPtr(0L);

	public override bool IsInvalid => false;

	internal SysFreeStringSafeHandle()
		: base(INVALID_HANDLE_VALUE, ownsHandle: true)
	{
	}

	internal SysFreeStringSafeHandle(nint preexistingHandle, bool ownsHandle = true)
		: base(INVALID_HANDLE_VALUE, ownsHandle)
	{
		SetHandle(preexistingHandle);
	}

	protected override bool ReleaseHandle()
	{
		PInvoke.SysFreeString((BSTR)handle);
		return true;
	}
}
