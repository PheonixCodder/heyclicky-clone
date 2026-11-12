using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Accessibility;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class UI_Accessibility_IUIAutomation_Extensions
{
	[OverloadResolutionPriority(1)]
	internal static IUIAutomationCondition CreateAndConditionFromNativeArray(this IUIAutomation @this, IUIAutomationCondition[] conditions)
	{
		return @this.CreateAndConditionFromNativeArray(conditions, (conditions != null) ? conditions.Length : 0);
	}

	[OverloadResolutionPriority(1)]
	internal static IUIAutomationCondition CreateOrConditionFromNativeArray(this IUIAutomation @this, IUIAutomationCondition[] conditions)
	{
		return @this.CreateOrConditionFromNativeArray(conditions, (conditions != null) ? conditions.Length : 0);
	}

	[OverloadResolutionPriority(1)]
	internal unsafe static void AddPropertyChangedEventHandlerNativeArray(this IUIAutomation @this, IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationPropertyChangedEventHandler handler, ReadOnlySpan<UIA_PROPERTY_ID> propertyArray)
	{
		fixed (UIA_PROPERTY_ID* propertyArray2 = propertyArray)
		{
			@this.AddPropertyChangedEventHandlerNativeArray(element, scope, cacheRequest, handler, propertyArray2, propertyArray.Length);
		}
	}

	[OverloadResolutionPriority(1)]
	internal unsafe static SAFEARRAY* IntNativeArrayToSafeArray(this IUIAutomation @this, int[] array)
	{
		return @this.IntNativeArrayToSafeArray(array, (array != null) ? array.Length : 0);
	}

	internal unsafe static void PollForPotentialSupportedPatterns(this IUIAutomation @this, IUIAutomationElement pElement, out SAFEARRAY* patternIds, out SAFEARRAY* patternNames)
	{
		fixed (SAFEARRAY** patternNames2 = &patternNames)
		{
			fixed (SAFEARRAY** patternIds2 = &patternIds)
			{
				@this.PollForPotentialSupportedPatterns(pElement, patternIds2, patternNames2);
			}
		}
	}

	internal unsafe static void PollForPotentialSupportedProperties(this IUIAutomation @this, IUIAutomationElement pElement, out SAFEARRAY* propertyIds, out SAFEARRAY* propertyNames)
	{
		fixed (SAFEARRAY** propertyNames2 = &propertyNames)
		{
			fixed (SAFEARRAY** propertyIds2 = &propertyIds)
			{
				@this.PollForPotentialSupportedProperties(pElement, propertyIds2, propertyNames2);
			}
		}
	}
}
