using System.CodeDom.Compiler;
using System.Drawing;

namespace Windows.Win32.UI.WindowsAndMessaging;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct CURSORINFO
{
	internal uint cbSize;

	internal CURSORINFO_FLAGS flags;

	internal HCURSOR hCursor;

	internal Point ptScreenPos;
}
