using System;

namespace HeyClicky;

[Flags]
public enum HotkeyModifiers : byte
{
	None = 0,
	Control = 1,
	Alt = 2,
	Shift = 4,
	Win = 8
}
