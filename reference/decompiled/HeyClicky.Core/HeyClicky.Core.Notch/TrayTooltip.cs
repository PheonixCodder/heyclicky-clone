using System.Collections.Generic;

namespace HeyClicky.Core.Notch;

public static class TrayTooltip
{
	public static string Text(bool isSignedIn, bool isPaused, IReadOnlyList<string> workingClickyNames)
	{
		List<string> list = new List<string> { "HeyClicky" };
		if (!isSignedIn)
		{
			list.Add("Signed out");
			return string.Join(" · ", list);
		}
		if (isPaused)
		{
			list.Add("Paused");
		}
		if (workingClickyNames.Count == 1)
		{
			list.Add(workingClickyNames[0] + " is working");
		}
		else if (workingClickyNames.Count > 1)
		{
			list.Add($"{workingClickyNames.Count} Clickys working");
		}
		return string.Join(" · ", list);
	}
}
