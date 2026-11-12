using System.CodeDom.Compiler;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class UI_Shell_PropertiesSystem_IPropertyStore_Extensions
{
	internal unsafe static void GetAt(this IPropertyStore @this, uint iProp, out PROPERTYKEY pkey)
	{
		fixed (PROPERTYKEY* pkey2 = &pkey)
		{
			@this.GetAt(iProp, pkey2);
		}
	}

	internal unsafe static void GetValue(this IPropertyStore @this, in PROPERTYKEY key, out PROPVARIANT pv)
	{
		fixed (PROPERTYKEY* key2 = &key)
		{
			@this.GetValue(key2, out pv);
		}
	}

	internal unsafe static void SetValue(this IPropertyStore @this, in PROPERTYKEY key, in PROPVARIANT propvar)
	{
		fixed (PROPERTYKEY* key2 = &key)
		{
			@this.SetValue(key2, in propvar);
		}
	}
}
