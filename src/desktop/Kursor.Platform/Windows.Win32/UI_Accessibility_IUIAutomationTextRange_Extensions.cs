using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class UI_Accessibility_IUIAutomationTextRange_Extensions
{
	internal static IUIAutomationTextRange FindText(this IUIAutomationTextRange @this, SafeHandle text, BOOL backward, BOOL ignoreCase)
	{
		bool success = false;
		try
		{
			if (text != null)
			{
				text.DangerousAddRef(ref success);
				BSTR text2 = (BSTR)text.DangerousGetHandle();
				return @this.FindText(text2, backward, ignoreCase);
			}
			throw new ArgumentNullException("text");
		}
		finally
		{
			if (success)
			{
				text.DangerousRelease();
			}
		}
	}
}
