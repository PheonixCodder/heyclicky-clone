using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkReviewListRules
{
	public static bool IsPathLike(string text)
	{
		string text2 = ClickyInkParser.Emphasis.Strip(text).TrimWs();
		if (!text2.HasPrefix("/") && !text2.HasPrefix("~/"))
		{
			return text2.Count((char c) => c == '/') >= 2;
		}
		return true;
	}

	public static bool IsCompact(int itemCount)
	{
		return itemCount > 6;
	}

	public static string Count(int toSend, int open)
	{
		if (toSend != 0)
		{
			return $"{toSend} of {open} ticked";
		}
		return "tick the ones to do";
	}
}
