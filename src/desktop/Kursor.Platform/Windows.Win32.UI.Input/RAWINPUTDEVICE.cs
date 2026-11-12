using System.CodeDom.Compiler;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.Input;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct RAWINPUTDEVICE
{
	internal ushort usUsagePage;

	internal ushort usUsage;

	internal RAWINPUTDEVICE_FLAGS dwFlags;

	internal HWND hwndTarget;
}
