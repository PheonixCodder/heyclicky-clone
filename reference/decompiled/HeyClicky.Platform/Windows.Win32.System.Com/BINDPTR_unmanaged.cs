using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Windows.Win32.System.Com;

[StructLayout(LayoutKind.Explicit)]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct BINDPTR_unmanaged
{
	[FieldOffset(0)]
	internal unsafe FUNCDESC_unmanaged* lpfuncdesc;

	[FieldOffset(0)]
	internal unsafe VARDESC_unmanaged* lpvardesc;

	[FieldOffset(0)]
	internal unsafe ITypeComp_unmanaged* lptcomp;
}
