using System.CodeDom.Compiler;
using System.Drawing;

namespace Windows.Win32.UI.WindowsAndMessaging;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct MSLLHOOKSTRUCT
{
	internal Point pt;

	internal uint mouseData;

	internal uint flags;

	internal uint time;

	internal nuint dwExtraInfo;
}
