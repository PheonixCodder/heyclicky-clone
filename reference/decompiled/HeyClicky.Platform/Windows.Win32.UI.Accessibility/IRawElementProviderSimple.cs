using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("D6DD68D1-86FD-4332-8666-9ABEDEA2D24C")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows5.1.2600")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IRawElementProviderSimple
{
	ProviderOptions ProviderOptions { get; }

	[return: MarshalAs(UnmanagedType.IUnknown)]
	object GetPatternProvider(UIA_PATTERN_ID patternId);

	[return: MarshalAs(UnmanagedType.Struct)]
	object GetPropertyValue(UIA_PROPERTY_ID propertyId);

	IRawElementProviderSimple HostRawElementProvider { get; }
}
