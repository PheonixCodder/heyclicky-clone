using System.CodeDom.Compiler;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Windows.Win32.UI.WindowsAndMessaging;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct WNDCLASSEXW
{
	internal uint cbSize;

	internal WNDCLASS_STYLES style;

	internal WNDPROC lpfnWndProc;

	internal int cbClsExtra;

	internal int cbWndExtra;

	internal HINSTANCE hInstance;

	internal HICON hIcon;

	internal HCURSOR hCursor;

	internal HBRUSH hbrBackground;

	internal PCWSTR lpszMenuName;

	internal PCWSTR lpszClassName;

	internal HICON hIconSm;
}
