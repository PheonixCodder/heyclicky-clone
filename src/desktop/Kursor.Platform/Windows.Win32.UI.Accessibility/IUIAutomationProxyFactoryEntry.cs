using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("D50E472E-B64B-490C-BCA1-D30696F9F289")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationProxyFactoryEntry
{
	IUIAutomationProxyFactory ProxyFactory { get; }

	BSTR get_ClassName();

	BSTR get_ImageName();

	BOOL get_AllowSubstringMatch();

	BOOL get_CanCheckBaseClass();

	BOOL get_NeedsAdviseEvents();

	void put_ClassName(PCWSTR className);

	void put_ImageName(PCWSTR imageName);

	void put_AllowSubstringMatch(BOOL allowSubstringMatch);

	void put_CanCheckBaseClass(BOOL canCheckBaseClass);

	void put_NeedsAdviseEvents(BOOL adviseEvents);

	unsafe void SetWinEventsForAutomationEvent(UIA_EVENT_ID eventId, UIA_PROPERTY_ID propertyId, SAFEARRAY* winEvents);

	unsafe SAFEARRAY* GetWinEventsForAutomationEvent(UIA_EVENT_ID eventId, UIA_PROPERTY_ID propertyId);
}
