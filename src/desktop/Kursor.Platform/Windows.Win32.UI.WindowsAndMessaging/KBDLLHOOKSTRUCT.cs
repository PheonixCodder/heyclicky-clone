using System.CodeDom.Compiler;

namespace Windows.Win32.UI.WindowsAndMessaging;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal struct KBDLLHOOKSTRUCT
{
	internal uint vkCode;

	internal uint scanCode;

	internal KBDLLHOOKSTRUCT_FLAGS flags;

	internal uint time;

	internal nuint dwExtraInfo;
}
