using System;

namespace HeyClicky.Platform.Input;

public static class ClickyKeyNames
{
	public static ClickyModifierKeys? Modifier(string name)
	{
		switch (name)
		{
		case "cmd":
		case "command":
		case "control":
		case "ctrl":
			return ClickyModifierKeys.Control;
		case "shift":
			return ClickyModifierKeys.Shift;
		case "opt":
		case "alt":
		case "option":
			return ClickyModifierKeys.Alt;
		case "win":
		case "windows":
		case "meta":
		case "super":
			return ClickyModifierKeys.Windows;
		case "fn":
			return ClickyModifierKeys.None;
		default:
			return null;
		}
	}

	public static ushort? VirtualKey(string name)
	{
		if (name.Length == 1)
		{
			char c = name[0];
			if (c >= 'a' && c <= 'z')
			{
				return char.ToUpperInvariant(c);
			}
			if (c >= '0' && c <= '9')
			{
				return c;
			}
		}
		int length = name.Length;
		bool flag = (uint)(length - 2) <= 1u;
		if (flag && name[0] == 'f' && int.TryParse(name.AsSpan(1), out var result) && result >= 1 && result <= 12)
		{
			return (ushort)(112 + result - 1);
		}
		switch (name)
		{
		case "=":
			return (ushort)187;
		case "-":
			return (ushort)189;
		case "]":
			return (ushort)221;
		case "[":
			return (ushort)219;
		case "'":
			return (ushort)222;
		case ";":
			return (ushort)186;
		case "\\":
			return (ushort)220;
		case ",":
			return (ushort)188;
		case "/":
			return (ushort)191;
		case ".":
			return (ushort)190;
		case "`":
			return (ushort)192;
		case "return":
		case "enter":
			return (ushort)13;
		case "tab":
			return (ushort)9;
		case "space":
			return (ushort)32;
		case "delete":
		case "backspace":
			return (ushort)8;
		case "escape":
		case "esc":
			return (ushort)27;
		case "forwarddelete":
			return (ushort)46;
		case "home":
			return (ushort)36;
		case "end":
			return (ushort)35;
		case "pageup":
			return (ushort)33;
		case "pagedown":
			return (ushort)34;
		case "left":
			return (ushort)37;
		case "up":
			return (ushort)38;
		case "right":
			return (ushort)39;
		case "down":
			return (ushort)40;
		case "insert":
			return (ushort)45;
		case "win":
		case "windows":
			return (ushort)91;
		default:
			return null;
		}
	}
}
