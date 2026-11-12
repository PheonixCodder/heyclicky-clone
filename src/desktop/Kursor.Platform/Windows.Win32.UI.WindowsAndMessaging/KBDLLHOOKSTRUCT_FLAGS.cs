using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.UI.WindowsAndMessaging;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum KBDLLHOOKSTRUCT_FLAGS : uint
{
	LLKHF_EXTENDED = 1u,
	LLKHF_ALTDOWN = 0x20u,
	LLKHF_UP = 0x80u,
	LLKHF_INJECTED = 0x10u,
	LLKHF_LOWER_IL_INJECTED = 2u
}
