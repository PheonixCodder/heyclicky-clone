using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Win32.System.Ole;

namespace Windows.Win32.System.Com;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct ELEMDESC_unmanaged
{
	[StructLayout(LayoutKind.Explicit)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Anonymous_e__Union_unmanaged
	{
		[FieldOffset(0)]
		internal IDLDESC idldesc;

		[FieldOffset(0)]
		internal PARAMDESC_unmanaged paramdesc;
	}

	internal TYPEDESC tdesc;

	internal _Anonymous_e__Union_unmanaged Anonymous;

	[UnscopedRef]
	internal ref IDLDESC idldesc => ref Anonymous.idldesc;

	[UnscopedRef]
	internal ref PARAMDESC_unmanaged paramdesc => ref Anonymous.paramdesc;
}
