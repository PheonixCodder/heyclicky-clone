namespace HeyClicky.App.Settings;

public static class ShortcutCaptureRules
{
	public const string ModifiersOnly = "Shortcuts use only Ctrl, Alt, Shift and Win. Hold two of them together, then let go.";

	public static string? HoldCaptureProblem(HotkeyModifiers held, bool pressedOtherKey)
	{
		if (pressedOtherKey)
		{
			return "Shortcuts use only Ctrl, Alt, Shift and Win. Hold two of them together, then let go.";
		}
		if (held == HotkeyModifiers.None || !IsSingleKey(held))
		{
			return null;
		}
		string text = new HotkeyBinding(held).KeyNames[0];
		return "Only " + text + " came through. Hold two of Ctrl, Alt, Shift and Win together, then let go. If you did, another app may be using that combo.";
	}

	public static string ConflictLine(HotkeyBinding captured, string otherTitle, HotkeyBinding other)
	{
		return $"{string.Join(" + ", captured.KeyNames)} is already used by {otherTitle} ({other.DisplayText}). Press a different combo, or change {otherTitle} first.";
	}

	private static bool IsSingleKey(HotkeyModifiers modifiers)
	{
		return (modifiers & (modifiers - 1)) == 0;
	}
}
