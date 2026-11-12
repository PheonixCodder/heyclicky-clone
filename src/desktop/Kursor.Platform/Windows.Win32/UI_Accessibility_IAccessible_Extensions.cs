using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class UI_Accessibility_IAccessible_Extensions
{
	internal unsafe static int get_accHelpTopic(this IAccessible @this, out SysFreeStringSafeHandle pszHelpFile, object varChild)
	{
		pszHelpFile = new SysFreeStringSafeHandle(0);
		BSTR bSTR = default(BSTR);
		int result = @this.get_accHelpTopic(&bSTR, varChild);
		Marshal.InitHandle(pszHelpFile, (nint)bSTR);
		return result;
	}

	internal static void put_accName(this IAccessible @this, object varChild, SafeHandle szName)
	{
		bool success = false;
		try
		{
			if (szName != null)
			{
				szName.DangerousAddRef(ref success);
				BSTR szName2 = (BSTR)szName.DangerousGetHandle();
				@this.put_accName(varChild, szName2);
				return;
			}
			throw new ArgumentNullException("szName");
		}
		finally
		{
			if (success)
			{
				szName.DangerousRelease();
			}
		}
	}

	internal static void put_accValue(this IAccessible @this, object varChild, SafeHandle szValue)
	{
		bool success = false;
		try
		{
			if (szValue != null)
			{
				szValue.DangerousAddRef(ref success);
				BSTR szValue2 = (BSTR)szValue.DangerousGetHandle();
				@this.put_accValue(varChild, szValue2);
				return;
			}
			throw new ArgumentNullException("szValue");
		}
		finally
		{
			if (success)
			{
				szValue.DangerousRelease();
			}
		}
	}
}
