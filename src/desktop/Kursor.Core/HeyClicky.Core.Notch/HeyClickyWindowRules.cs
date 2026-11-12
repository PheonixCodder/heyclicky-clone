using System;

namespace HeyClicky.Core.Notch;

public static class HeyClickyWindowRules
{
	public const double GapAboveComposer = 24.0;

	public const double NotchGapAboveComposer = 6.0;

	public static double BottomRoom(double composerHeight, double composerExtraHeight, bool isInWindow)
	{
		return composerHeight + Math.Max(0.0, composerExtraHeight) + (isInWindow ? 24.0 : 6.0);
	}

	public static string AgentComposerPlaceholder(string? name)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			return "Message " + name.Trim() + "…";
		}
		return "Message it…";
	}
}
