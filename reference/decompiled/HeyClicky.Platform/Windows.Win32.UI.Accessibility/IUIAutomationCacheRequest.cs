using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("B32A92B5-BC25-4078-9C08-D7EE95C48E03")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationCacheRequest
{
	void AddProperty(UIA_PROPERTY_ID propertyId);

	void AddPattern(UIA_PATTERN_ID patternId);

	IUIAutomationCacheRequest Clone();

	TreeScope TreeScope { get; set; }

	IUIAutomationCondition TreeFilter { get; set; }

	AutomationElementMode AutomationElementMode { get; set; }
}
