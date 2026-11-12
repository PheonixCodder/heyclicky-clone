using System.CodeDom.Compiler;
using Windows.Win32.UI.Accessibility;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class UI_Accessibility_IUIAutomationProxyFactoryEntry_Extensions
{
	internal unsafe static void put_ClassName(this IUIAutomationProxyFactoryEntry @this, string className)
	{
		fixed (char* ptr = className)
		{
			@this.put_ClassName(ptr);
		}
	}

	internal unsafe static void put_ImageName(this IUIAutomationProxyFactoryEntry @this, string imageName)
	{
		fixed (char* ptr = imageName)
		{
			@this.put_ImageName(ptr);
		}
	}
}
