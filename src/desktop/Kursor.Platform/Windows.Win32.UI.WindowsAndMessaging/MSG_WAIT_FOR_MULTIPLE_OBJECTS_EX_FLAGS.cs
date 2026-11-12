using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.UI.WindowsAndMessaging;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum MSG_WAIT_FOR_MULTIPLE_OBJECTS_EX_FLAGS : uint
{
	MWMO_NONE = 0u,
	MWMO_ALERTABLE = 2u,
	MWMO_INPUTAVAILABLE = 4u,
	MWMO_WAITALL = 1u
}
