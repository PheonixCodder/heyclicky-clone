using System;
using System.CodeDom.Compiler;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomationElement
{
	void SetFocus();

	unsafe SAFEARRAY* GetRuntimeId();

	IUIAutomationElement FindFirst(TreeScope scope, IUIAutomationCondition condition);

	IUIAutomationElementArray FindAll(TreeScope scope, IUIAutomationCondition condition);

	IUIAutomationElement FindFirstBuildCache(TreeScope scope, IUIAutomationCondition condition, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElementArray FindAllBuildCache(TreeScope scope, IUIAutomationCondition condition, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement BuildUpdatedCache(IUIAutomationCacheRequest cacheRequest);

	[return: MarshalAs(UnmanagedType.Struct)]
	object GetCurrentPropertyValue(UIA_PROPERTY_ID propertyId);

	[return: MarshalAs(UnmanagedType.Struct)]
	object GetCurrentPropertyValueEx(UIA_PROPERTY_ID propertyId, BOOL ignoreDefaultValue);

	[return: MarshalAs(UnmanagedType.Struct)]
	object GetCachedPropertyValue(UIA_PROPERTY_ID propertyId);

	[return: MarshalAs(UnmanagedType.Struct)]
	object GetCachedPropertyValueEx(UIA_PROPERTY_ID propertyId, BOOL ignoreDefaultValue);

	unsafe void GetCurrentPatternAs(UIA_PATTERN_ID patternId, Guid* riid, out void* patternObject);

	unsafe void GetCachedPatternAs(UIA_PATTERN_ID patternId, Guid* riid, out void* patternObject);

	[return: MarshalAs(UnmanagedType.IUnknown)]
	object GetCurrentPattern(UIA_PATTERN_ID patternId);

	[return: MarshalAs(UnmanagedType.IUnknown)]
	object GetCachedPattern(UIA_PATTERN_ID patternId);

	IUIAutomationElement GetCachedParent();

	IUIAutomationElementArray GetCachedChildren();

	int CurrentProcessId { get; }

	UIA_CONTROLTYPE_ID CurrentControlType { get; }

	BSTR CurrentLocalizedControlType { get; }

	BSTR CurrentName { get; }

	BSTR CurrentAcceleratorKey { get; }

	BSTR CurrentAccessKey { get; }

	BOOL CurrentHasKeyboardFocus { get; }

	BOOL CurrentIsKeyboardFocusable { get; }

	BOOL CurrentIsEnabled { get; }

	BSTR CurrentAutomationId { get; }

	BSTR CurrentClassName { get; }

	BSTR CurrentHelpText { get; }

	int CurrentCulture { get; }

	BOOL CurrentIsControlElement { get; }

	BOOL CurrentIsContentElement { get; }

	BOOL CurrentIsPassword { get; }

	HWND CurrentNativeWindowHandle { get; }

	BSTR CurrentItemType { get; }

	BOOL CurrentIsOffscreen { get; }

	OrientationType CurrentOrientation { get; }

	BSTR CurrentFrameworkId { get; }

	BOOL CurrentIsRequiredForForm { get; }

	BSTR CurrentItemStatus { get; }

	RECT CurrentBoundingRectangle { get; }

	IUIAutomationElement CurrentLabeledBy { get; }

	BSTR CurrentAriaRole { get; }

	BSTR CurrentAriaProperties { get; }

	BOOL CurrentIsDataValidForForm { get; }

	IUIAutomationElementArray CurrentControllerFor { get; }

	IUIAutomationElementArray CurrentDescribedBy { get; }

	IUIAutomationElementArray CurrentFlowsTo { get; }

	BSTR CurrentProviderDescription { get; }

	int CachedProcessId { get; }

	UIA_CONTROLTYPE_ID CachedControlType { get; }

	BSTR CachedLocalizedControlType { get; }

	BSTR CachedName { get; }

	BSTR CachedAcceleratorKey { get; }

	BSTR CachedAccessKey { get; }

	BOOL CachedHasKeyboardFocus { get; }

	BOOL CachedIsKeyboardFocusable { get; }

	BOOL CachedIsEnabled { get; }

	BSTR CachedAutomationId { get; }

	BSTR CachedClassName { get; }

	BSTR CachedHelpText { get; }

	int CachedCulture { get; }

	BOOL CachedIsControlElement { get; }

	BOOL CachedIsContentElement { get; }

	BOOL CachedIsPassword { get; }

	HWND CachedNativeWindowHandle { get; }

	BSTR CachedItemType { get; }

	BOOL CachedIsOffscreen { get; }

	OrientationType CachedOrientation { get; }

	BSTR CachedFrameworkId { get; }

	BOOL CachedIsRequiredForForm { get; }

	BSTR CachedItemStatus { get; }

	RECT CachedBoundingRectangle { get; }

	IUIAutomationElement CachedLabeledBy { get; }

	BSTR CachedAriaRole { get; }

	BSTR CachedAriaProperties { get; }

	BOOL CachedIsDataValidForForm { get; }

	IUIAutomationElementArray CachedControllerFor { get; }

	IUIAutomationElementArray CachedDescribedBy { get; }

	IUIAutomationElementArray CachedFlowsTo { get; }

	BSTR CachedProviderDescription { get; }

	unsafe BOOL GetClickablePoint(Point* clickable);
}
