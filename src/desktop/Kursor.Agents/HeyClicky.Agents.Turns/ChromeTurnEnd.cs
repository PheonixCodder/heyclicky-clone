using System;
using System.Linq;

namespace HeyClicky.Agents.Turns;

public static class ChromeTurnEnd
{
	public static bool HelperStoppedForChrome(string? finalResponseMarkdown, string? finalSummary)
	{
		return string.Join("\n", new string[2] { finalResponseMarkdown, finalSummary }.OfType<string>()).Contains("chrome", StringComparison.OrdinalIgnoreCase);
	}

	public static bool AsksAtTurnEnd(bool foundChromeNotConnected, bool stoppedForChrome, bool wentWithoutChrome)
	{
		if (foundChromeNotConnected && stoppedForChrome)
		{
			return !wentWithoutChrome;
		}
		return false;
	}
}
