using System.CodeDom.Compiler;
using System.Drawing;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.WindowsAndMessaging;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct MSG
{
	internal HWND hwnd;

	internal uint message;

	internal WPARAM wParam;

	internal LPARAM lParam;

	internal uint time;

	internal Point pt;
}
