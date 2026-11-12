using System;
using System.CodeDom.Compiler;

namespace Windows.Win32.UI.Input;

[Flags]
[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal enum RAWINPUTDEVICE_FLAGS : uint
{
	RIDEV_REMOVE = 1u,
	RIDEV_EXCLUDE = 0x10u,
	RIDEV_PAGEONLY = 0x20u,
	RIDEV_NOLEGACY = 0x30u,
	RIDEV_INPUTSINK = 0x100u,
	RIDEV_CAPTUREMOUSE = 0x200u,
	RIDEV_NOHOTKEYS = 0x200u,
	RIDEV_APPKEYS = 0x400u,
	RIDEV_EXINPUTSINK = 0x1000u,
	RIDEV_DEVNOTIFY = 0x2000u
}
