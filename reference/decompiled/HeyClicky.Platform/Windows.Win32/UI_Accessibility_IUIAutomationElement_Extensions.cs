using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class UI_Accessibility_IUIAutomationElement_Extensions
{
	[OverloadResolutionPriority(1)]
	internal unsafe static void GetCurrentPatternAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(this IUIAutomationElement @this, UIA_PATTERN_ID patternId, out T patternObject) where T : class
	{
		void* patternObject2 = null;
		try
		{
			Guid iID = ComOrWinRTObjectMarshaller.GetIID<T>();
			@this.GetCurrentPatternAs(patternId, &iID, out patternObject2);
			patternObject = (T)ComOrWinRTObjectMarshaller.ConvertToManaged((nint)patternObject2);
		}
		finally
		{
			if (patternObject2 != null)
			{
				Marshal.Release((nint)patternObject2);
			}
		}
	}

	[OverloadResolutionPriority(1)]
	internal unsafe static void GetCachedPatternAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(this IUIAutomationElement @this, UIA_PATTERN_ID patternId, out T patternObject) where T : class
	{
		void* patternObject2 = null;
		try
		{
			Guid iID = ComOrWinRTObjectMarshaller.GetIID<T>();
			@this.GetCachedPatternAs(patternId, &iID, out patternObject2);
			patternObject = (T)ComOrWinRTObjectMarshaller.ConvertToManaged((nint)patternObject2);
		}
		finally
		{
			if (patternObject2 != null)
			{
				Marshal.Release((nint)patternObject2);
			}
		}
	}

	internal unsafe static BOOL GetClickablePoint(this IUIAutomationElement @this, out Point clickable)
	{
		fixed (Point* clickable2 = &clickable)
		{
			return @this.GetClickablePoint(clickable2);
		}
	}
}
