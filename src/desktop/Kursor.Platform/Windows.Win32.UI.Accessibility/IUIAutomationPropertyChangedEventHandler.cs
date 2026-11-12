using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("40CD37D4-C756-4B0C-8C6F-BDDFEEB13B50")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationPropertyChangedEventHandler
{
	void HandlePropertyChangedEvent(IUIAutomationElement sender, UIA_PROPERTY_ID propertyId, [MarshalAs(UnmanagedType.Struct)] object newValue);
}
