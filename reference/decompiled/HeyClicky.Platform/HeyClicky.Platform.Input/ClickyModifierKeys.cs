using System;

namespace HeyClicky.Platform.Input;

[Flags]
public enum ClickyModifierKeys
{
	None = 0,
	Control = 1,
	Alt = 2,
	Shift = 4,
	Windows = 8
}
