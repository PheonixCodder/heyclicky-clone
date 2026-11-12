using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("A94CD8B1-0844-4CD6-9D2D-640537AB39E9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationValuePattern
{
	void SetValue(BSTR val);

	BSTR CurrentValue { get; }

	BOOL CurrentIsReadOnly { get; }

	BSTR CachedValue { get; }

	BOOL CachedIsReadOnly { get; }
}
