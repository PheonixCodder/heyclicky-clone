using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.System.Com;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum COINIT
{
	COINIT_APARTMENTTHREADED = 2,
	COINIT_MULTITHREADED = 0,
	COINIT_DISABLE_OLE1DDE = 4,
	COINIT_SPEED_OVER_MEMORY = 8
}
