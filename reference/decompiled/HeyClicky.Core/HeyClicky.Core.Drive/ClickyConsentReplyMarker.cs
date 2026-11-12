using System;

namespace HeyClicky.Core.Drive;

public static class ClickyConsentReplyMarker
{
	public enum Kind
	{
		Allowed,
		Declined,
		Drove
	}

	public const string ApprovedPrefix = "[Computer use approved for this turn]";

	public const string DeclinedPrefix = "Not now. Finish without controlling my PC";

	public const string DrovePrefix = "[Clicky drove your screen for this step]";

	public static Kind? KindOf(string messageText)
	{
		string text = messageText.Trim();
		if (text.StartsWith("[Computer use approved for this turn]", StringComparison.Ordinal))
		{
			return Kind.Allowed;
		}
		if (text.StartsWith("[Clicky drove your screen for this step]", StringComparison.Ordinal))
		{
			return Kind.Drove;
		}
		if (text.StartsWith("Not now. Finish without controlling my PC", StringComparison.Ordinal))
		{
			return Kind.Declined;
		}
		return null;
	}

	public static string PreviewLine(Kind kind)
	{
		return kind switch
		{
			Kind.Allowed => "You allowed it to use your PC", 
			Kind.Declined => "You said not now", 
			_ => "HeyClicky drove your PC for it", 
		};
	}
}
