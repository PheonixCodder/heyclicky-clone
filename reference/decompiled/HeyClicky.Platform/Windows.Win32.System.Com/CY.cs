using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Windows.Win32.System.Com;

[StructLayout(LayoutKind.Explicit)]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct CY
{
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Anonymous_e__Struct
	{
		internal uint Lo;

		internal int Hi;
	}

	[FieldOffset(0)]
	internal _Anonymous_e__Struct Anonymous;

	[FieldOffset(0)]
	internal long int64;

	[UnscopedRef]
	internal ref uint Lo => ref Anonymous.Lo;

	[UnscopedRef]
	internal ref int Hi => ref Anonymous.Hi;
}
