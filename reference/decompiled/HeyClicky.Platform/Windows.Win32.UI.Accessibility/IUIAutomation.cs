using System.CodeDom.Compiler;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows6.1")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomation
{
	BOOL CompareElements(IUIAutomationElement el1, IUIAutomationElement el2);

	unsafe BOOL CompareRuntimeIds(SAFEARRAY* runtimeId1, SAFEARRAY* runtimeId2);

	IUIAutomationElement GetRootElement();

	IUIAutomationElement ElementFromHandle(HWND hwnd);

	IUIAutomationElement ElementFromPoint(Point pt);

	IUIAutomationElement GetFocusedElement();

	IUIAutomationElement GetRootElementBuildCache(IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement ElementFromHandleBuildCache(HWND hwnd, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement ElementFromPointBuildCache(Point pt, IUIAutomationCacheRequest cacheRequest);

	IUIAutomationElement GetFocusedElementBuildCache(IUIAutomationCacheRequest cacheRequest);

	IUIAutomationTreeWalker CreateTreeWalker(IUIAutomationCondition pCondition);

	IUIAutomationTreeWalker ControlViewWalker { get; }

	IUIAutomationTreeWalker ContentViewWalker { get; }

	IUIAutomationTreeWalker RawViewWalker { get; }

	IUIAutomationCondition RawViewCondition { get; }

	IUIAutomationCondition ControlViewCondition { get; }

	IUIAutomationCondition ContentViewCondition { get; }

	IUIAutomationCacheRequest CreateCacheRequest();

	IUIAutomationCondition CreateTrueCondition();

	IUIAutomationCondition CreateFalseCondition();

	IUIAutomationCondition CreatePropertyCondition(UIA_PROPERTY_ID propertyId, [MarshalAs(UnmanagedType.Struct)] object value);

	IUIAutomationCondition CreatePropertyConditionEx(UIA_PROPERTY_ID propertyId, [MarshalAs(UnmanagedType.Struct)] object value, PropertyConditionFlags flags);

	IUIAutomationCondition CreateAndCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2);

	unsafe IUIAutomationCondition CreateAndConditionFromArray(SAFEARRAY* conditions);

	IUIAutomationCondition CreateAndConditionFromNativeArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IUIAutomationCondition[] conditions, int conditionCount);

	IUIAutomationCondition CreateOrCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2);

	unsafe IUIAutomationCondition CreateOrConditionFromArray(SAFEARRAY* conditions);

	IUIAutomationCondition CreateOrConditionFromNativeArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IUIAutomationCondition[] conditions, int conditionCount);

	IUIAutomationCondition CreateNotCondition(IUIAutomationCondition condition);

	void AddAutomationEventHandler(UIA_EVENT_ID eventId, IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationEventHandler handler);

	void RemoveAutomationEventHandler(UIA_EVENT_ID eventId, IUIAutomationElement element, IUIAutomationEventHandler handler);

	unsafe void AddPropertyChangedEventHandlerNativeArray(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationPropertyChangedEventHandler handler, UIA_PROPERTY_ID* propertyArray, int propertyCount);

	unsafe void AddPropertyChangedEventHandler(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationPropertyChangedEventHandler handler, SAFEARRAY* propertyArray);

	void RemovePropertyChangedEventHandler(IUIAutomationElement element, IUIAutomationPropertyChangedEventHandler handler);

	void AddStructureChangedEventHandler(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationStructureChangedEventHandler handler);

	void RemoveStructureChangedEventHandler(IUIAutomationElement element, IUIAutomationStructureChangedEventHandler handler);

	void AddFocusChangedEventHandler(IUIAutomationCacheRequest cacheRequest, IUIAutomationFocusChangedEventHandler handler);

	void RemoveFocusChangedEventHandler(IUIAutomationFocusChangedEventHandler handler);

	void RemoveAllEventHandlers();

	unsafe SAFEARRAY* IntNativeArrayToSafeArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] array, int arrayCount);

	unsafe int IntSafeArrayToNativeArray(SAFEARRAY* intArray, int** array);

	[return: MarshalAs(UnmanagedType.Struct)]
	object RectToVariant(RECT rc);

	RECT VariantToRect([MarshalAs(UnmanagedType.Struct)] object var);

	unsafe int SafeArrayToRectNativeArray(SAFEARRAY* rects, RECT** rectArray);

	IUIAutomationProxyFactoryEntry CreateProxyFactoryEntry(IUIAutomationProxyFactory factory);

	IUIAutomationProxyFactoryMapping ProxyFactoryMapping { get; }

	BSTR GetPropertyProgrammaticName(UIA_PROPERTY_ID property);

	BSTR GetPatternProgrammaticName(UIA_PATTERN_ID pattern);

	unsafe void PollForPotentialSupportedPatterns(IUIAutomationElement pElement, SAFEARRAY** patternIds, SAFEARRAY** patternNames);

	unsafe void PollForPotentialSupportedProperties(IUIAutomationElement pElement, SAFEARRAY** propertyIds, SAFEARRAY** propertyNames);

	BOOL CheckNotSupported([MarshalAs(UnmanagedType.Struct)] object value);

	object ReservedNotSupportedValue
	{
		[return: MarshalAs(UnmanagedType.IUnknown)]
		get;
	}

	object ReservedMixedAttributeValue
	{
		[return: MarshalAs(UnmanagedType.IUnknown)]
		get;
	}

	IUIAutomationElement ElementFromIAccessible(IAccessible accessible, int childId);

	IUIAutomationElement ElementFromIAccessibleBuildCache(IAccessible accessible, int childId, IUIAutomationCacheRequest cacheRequest);
}
