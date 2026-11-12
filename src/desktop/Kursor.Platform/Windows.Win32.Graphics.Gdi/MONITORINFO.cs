using System.CodeDom.Compiler;
using Windows.Win32.Foundation;

namespace Windows.Win32.Graphics.Gdi;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct MONITORINFO
{
	internal uint cbSize;

	internal RECT rcMonitor;

	internal RECT rcWork;

	internal uint dwFlags;
}
