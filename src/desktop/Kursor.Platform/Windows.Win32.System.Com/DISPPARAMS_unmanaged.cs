using System.CodeDom.Compiler;
using Windows.Win32.System.Variant;

namespace Windows.Win32.System.Com;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct DISPPARAMS_unmanaged
{
	internal unsafe VARIANT_unmanaged* rgvarg;

	internal unsafe int* rgdispidNamedArgs;

	internal uint cArgs;

	internal uint cNamedArgs;
}
