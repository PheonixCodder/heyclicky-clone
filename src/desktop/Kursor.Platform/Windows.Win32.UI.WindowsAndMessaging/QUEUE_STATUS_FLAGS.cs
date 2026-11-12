using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.UI.WindowsAndMessaging;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum QUEUE_STATUS_FLAGS : uint
{
	QS_ALLEVENTS = 0x4BFu,
	QS_ALLINPUT = 0x4FFu,
	QS_ALLPOSTMESSAGE = 0x100u,
	QS_HOTKEY = 0x80u,
	QS_INPUT = 0x407u,
	QS_KEY = 1u,
	QS_MOUSE = 6u,
	QS_MOUSEBUTTON = 4u,
	QS_MOUSEMOVE = 2u,
	QS_PAINT = 0x20u,
	QS_POSTMESSAGE = 8u,
	QS_RAWINPUT = 0x400u,
	QS_SENDMESSAGE = 0x40u,
	QS_TIMER = 0x10u
}
