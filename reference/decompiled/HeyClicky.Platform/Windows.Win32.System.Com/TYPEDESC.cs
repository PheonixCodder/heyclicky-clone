using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Win32.System.Ole;
using Windows.Win32.System.Variant;

namespace Windows.Win32.System.Com;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct TYPEDESC
{
	[StructLayout(LayoutKind.Explicit)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Anonymous_e__Union
	{
		[FieldOffset(0)]
		internal unsafe TYPEDESC* lptdesc;

		[FieldOffset(0)]
		internal unsafe ARRAYDESC* lpadesc;

		[FieldOffset(0)]
		internal uint hreftype;
	}

	internal _Anonymous_e__Union Anonymous;

	internal VARENUM vt;

	[UnscopedRef]
	internal unsafe ref TYPEDESC* lptdesc => ref Anonymous.lptdesc;

	[UnscopedRef]
	internal unsafe ref ARRAYDESC* lpadesc => ref Anonymous.lpadesc;

	[UnscopedRef]
	internal ref uint hreftype => ref Anonymous.hreftype;
}
