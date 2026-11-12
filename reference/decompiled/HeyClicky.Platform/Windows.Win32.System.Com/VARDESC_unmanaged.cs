using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Variant;

namespace Windows.Win32.System.Com;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct VARDESC_unmanaged
{
	[StructLayout(LayoutKind.Explicit)]
	[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
	internal struct _Anonymous_e__Union_unmanaged
	{
		[FieldOffset(0)]
		internal uint oInst;

		[FieldOffset(0)]
		internal unsafe VARIANT_unmanaged* lpvarValue;
	}

	internal int memid;

	internal PWSTR lpstrSchema;

	internal _Anonymous_e__Union_unmanaged Anonymous;

	internal ELEMDESC_unmanaged elemdescVar;

	internal VARFLAGS wVarFlags;

	internal VARKIND varkind;

	[UnscopedRef]
	internal ref uint oInst => ref Anonymous.oInst;

	[UnscopedRef]
	internal unsafe ref VARIANT_unmanaged* lpvarValue => ref Anonymous.lpvarValue;
}
