using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.System.Memory;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum GLOBAL_ALLOC_FLAGS : uint
{
	GHND = 0x42u,
	GMEM_FIXED = 0u,
	GMEM_MOVEABLE = 2u,
	GMEM_ZEROINIT = 0x40u,
	GPTR = 0x40u
}
