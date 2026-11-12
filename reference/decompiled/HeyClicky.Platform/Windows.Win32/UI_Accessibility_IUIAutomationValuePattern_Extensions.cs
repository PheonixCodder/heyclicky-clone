using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class UI_Accessibility_IUIAutomationValuePattern_Extensions
{
	internal static void SetValue(this IUIAutomationValuePattern @this, SafeHandle val)
	{
		bool success = false;
		try
		{
			if (val != null)
			{
				val.DangerousAddRef(ref success);
				BSTR value = (BSTR)val.DangerousGetHandle();
				@this.SetValue(value);
				return;
			}
			throw new ArgumentNullException("val");
		}
		finally
		{
			if (success)
			{
				val.DangerousRelease();
			}
		}
	}
}
