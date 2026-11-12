using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.UI.WindowsAndMessaging;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum PEEK_MESSAGE_REMOVE_TYPE : uint
{
	PM_NOREMOVE = 0u,
	PM_REMOVE = 1u,
	PM_NOYIELD = 2u,
	PM_QS_INPUT = 0x4070000u,
	PM_QS_POSTMESSAGE = 0x980000u,
	PM_QS_PAINT = 0x200000u,
	PM_QS_SENDMESSAGE = 0x400000u
}
