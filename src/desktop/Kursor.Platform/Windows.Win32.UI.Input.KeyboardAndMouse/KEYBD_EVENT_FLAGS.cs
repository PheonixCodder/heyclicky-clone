using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.UI.Input.KeyboardAndMouse;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum KEYBD_EVENT_FLAGS : uint
{
	KEYEVENTF_EXTENDEDKEY = 1u,
	KEYEVENTF_KEYUP = 2u,
	KEYEVENTF_SCANCODE = 8u,
	KEYEVENTF_UNICODE = 4u
}
