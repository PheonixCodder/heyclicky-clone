using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("85B94ECD-849D-42B6-B94D-D6DB23FDF5A4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationProxyFactory
{
	IRawElementProviderSimple CreateProvider(HWND hwnd, int idObject, int idChild);

	BSTR ProxyFactoryId { get; }
}
