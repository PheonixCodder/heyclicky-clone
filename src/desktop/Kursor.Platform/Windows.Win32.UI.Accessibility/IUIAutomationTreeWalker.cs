using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("4042C624-389C-4AFC-A630-9DF854A541FC")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationTreeWalker
{
	IUIAutomationElement GetParentElement(IUIAutomationElement element);

	IUIAutomationElement GetFirstChildElement(IUIAutomationElement element);

	IUIAutomationElement GetLastChildElement(IUIAutomationElement element);

	IUIAutomationElement GetNextSiblingElement(IUIAutomationElement element);

	IUIAutomationElement GetPreviousSiblingElement(IUIAutomationElement element);

	IUIAutomationElement NormalizeElement(IUIAutomationElement element);

	IUIAutomationElement GetParentElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement GetFirstChildElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement GetLastChildElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement GetNextSiblingElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement GetPreviousSiblingElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement NormalizeElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationCondition Condition { get; }
}
