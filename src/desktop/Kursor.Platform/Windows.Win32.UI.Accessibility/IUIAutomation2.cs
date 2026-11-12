using System.CodeDom.Compiler;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32.UI.Accessibility;

[ComImport]
[Guid("34723AFF-0C9D-49D0-9896-7AB52DF8CD8A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[SupportedOSPlatform("windows8.0")]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal interface IUIAutomation2 : IUIAutomation
{
	new BOOL CompareElements(IUIAutomationElement el1, IUIAutomationElement el2);

	new unsafe BOOL CompareRuntimeIds(SAFEARRAY* runtimeId1, SAFEARRAY* runtimeId2);

	new IUIAutomationElement GetRootElement();

	new IUIAutomationElement ElementFromHandle(HWND hwnd);

	new IUIAutomationElement ElementFromPoint(Point pt);

	new IUIAutomationElement GetFocusedElement();

	new IUIAutomationElement GetRootElementBuildCache(IUIAutomationCacheRequest cacheRequest);

	new IUIAutomationElement ElementFromHandleBuildCache(HWND hwnd, IUIAutomationCacheRequest cacheRequest);

	new IUIAutomationElement ElementFromPointBuildCache(Point pt, IUIAutomationCacheRequest cacheRequest);

	new IUIAutomationElement GetFocusedElementBuildCache(IUIAutomationCacheRequest cacheRequest);

	new IUIAutomationTreeWalker CreateTreeWalker(IUIAutomationCondition pCondition);

	new IUIAutomationTreeWalker ControlViewWalker { get; }

	new IUIAutomationTreeWalker ContentViewWalker { get; }

	new IUIAutomationTreeWalker RawViewWalker { get; }

	new IUIAutomationCondition RawViewCondition { get; }

	new IUIAutomationCondition ControlViewCondition { get; }

	new IUIAutomationCondition ContentViewCondition { get; }

	new IUIAutomationCacheRequest CreateCacheRequest();

	new IUIAutomationCondition CreateTrueCondition();

	new IUIAutomationCondition CreateFalseCondition();

	new IUIAutomationCondition CreatePropertyCondition(UIA_PROPERTY_ID propertyId, [MarshalAs(UnmanagedType.Struct)] object value);

	new IUIAutomationCondition CreatePropertyConditionEx(UIA_PROPERTY_ID propertyId, [MarshalAs(UnmanagedType.Struct)] object value, PropertyConditionFlags flags);

	new IUIAutomationCondition CreateAndCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2);

	new unsafe IUIAutomationCondition CreateAndConditionFromArray(SAFEARRAY* conditions);

	new IUIAutomationCondition CreateAndConditionFromNativeArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IUIAutomationCondition[] conditions, int conditionCount);

	new IUIAutomationCondition CreateOrCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2);

	new unsafe IUIAutomationCondition CreateOrConditionFromArray(SAFEARRAY* conditions);

	new IUIAutomationCondition CreateOrConditionFromNativeArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IUIAutomationCondition[] conditions, int conditionCount);

	new IUIAutomationCondition CreateNotCondition(IUIAutomationCondition condition);

	new void AddAutomationEventHandler(UIA_EVENT_ID eventId, IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationEventHandler handler);

	new void RemoveAutomationEventHandler(UIA_EVENT_ID eventId, IUIAutomationElement element, IUIAutomationEventHandler handler);

	new unsafe void AddPropertyChangedEventHandlerNativeArray(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationPropertyChangedEventHandler handler, UIA_PROPERTY_ID* propertyArray, int propertyCount);

	new unsafe void AddPropertyChangedEventHandler(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationPropertyChangedEventHandler handler, SAFEARRAY* propertyArray);

	new void RemovePropertyChangedEventHandler(IUIAutomationElement element, IUIAutomationPropertyChangedEventHandler handler);

	new void AddStructureChangedEventHandler(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationStructureChangedEventHandler handler);

	new void RemoveStructureChangedEventHandler(IUIAutomationElement element, IUIAutomationStructureChangedEventHandler handler);

	new void AddFocusChangedEventHandler(IUIAutomationCacheRequest cacheRequest, IUIAutomationFocusChangedEventHandler handler);

	new void RemoveFocusChangedEventHandler(IUIAutomationFocusChangedEventHandler handler);

	new void RemoveAllEventHandlers();

	new unsafe SAFEARRAY* IntNativeArrayToSafeArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] array, int arrayCount);

	new unsafe int IntSafeArrayToNativeArray(SAFEARRAY* intArray, int** array);

	[return: MarshalAs(UnmanagedType.Struct)]
	new object RectToVariant(RECT rc);

	new RECT VariantToRect([MarshalAs(UnmanagedType.Struct)] object var);

	new unsafe int SafeArrayToRectNativeArray(SAFEARRAY* rects, RECT** rectArray);

	new IUIAutomationProxyFactoryEntry CreateProxyFactoryEntry(IUIAutomationProxyFactory factory);

	new IUIAutomationProxyFactoryMapping ProxyFactoryMapping { get; }

	new BSTR GetPropertyProgrammaticName(UIA_PROPERTY_ID property);

	new BSTR GetPatternProgrammaticName(UIA_PATTERN_ID pattern);

	new unsafe void PollForPotentialSupportedPatterns(IUIAutomationElement pElement, SAFEARRAY** patternIds, SAFEARRAY** patternNames);

	new unsafe void PollForPotentialSupportedProperties(IUIAutomationElement pElement, SAFEARRAY** propertyIds, SAFEARRAY** propertyNames);

	new BOOL CheckNotSupported([MarshalAs(UnmanagedType.Struct)] object value);

	new object ReservedNotSupportedValue
	{
		[return: MarshalAs(UnmanagedType.IUnknown)]
		get;
	}

	new object ReservedMixedAttributeValue
	{
		[return: MarshalAs(UnmanagedType.IUnknown)]
		get;
	}

	new IUIAutomationElement ElementFromIAccessible(IAccessible accessible, int childId);

	new IUIAutomationElement ElementFromIAccessibleBuildCache(IAccessible accessible, int childId, IUIAutomationCacheRequest cacheRequest);

	BOOL AutoSetFocus { get; set; }

	uint ConnectionTimeout { get; set; }

	uint TransactionTimeout { get; set; }
}
