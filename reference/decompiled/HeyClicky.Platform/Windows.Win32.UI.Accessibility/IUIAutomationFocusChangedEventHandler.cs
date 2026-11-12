using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("C270F6B5-5C69-4290-9745-7A7F97169468")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationFocusChangedEventHandler
{
	void HandleFocusChangedEvent(IUIAutomationElement sender);
}
