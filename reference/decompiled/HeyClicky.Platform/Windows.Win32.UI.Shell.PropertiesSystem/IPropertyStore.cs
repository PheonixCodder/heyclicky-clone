using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;

namespace Windows.Win32.UI.Shell.PropertiesSystem;

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IPropertyStore
{
	void GetCount(out uint cProps);

	unsafe void GetAt(uint iProp, PROPERTYKEY* pkey);

	unsafe void GetValue(PROPERTYKEY* key, out PROPVARIANT pv);

	unsafe void SetValue(PROPERTYKEY* key, in PROPVARIANT propvar);

	void Commit();
}
