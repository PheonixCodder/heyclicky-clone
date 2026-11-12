using System;
using System.Linq;

namespace HeyClicky.Core.Browser;

public static class BrowserErrorKinds
{
	private static readonly (string Kind, string[] Phrases)[] Kinds = new(string, string[])[13]
	{
		("detached", new string[1] { "chrome detached the debugger" }),
		("tab_closed", new string[1] { "was closed" }),
		("access_stopped", new string[4] { "browser access stopped", "browser access is unavailable", "browser access for this turn has ended", "browser actions timed out" }),
		("action_timeout", new string[1] { "this browser action took too long" }),
		("read_timeout", new string[2] { "didn't answer in time", "did not respond in time" }),
		("page_loading", new string[2] { "still loading", "is navigating" }),
		("stale_ref", new string[4] { "stale or unknown reference", "reference no longer applies", "element disappeared", "page changed" }),
		("not_clickable", new string[3] { "not clickable", "is obscured", "element action failed" }),
		("dialog_open", new string[2] { "a page dialog is open", "no page dialog" }),
		("script_threw", new string[1] { "the code threw" }),
		("not_connected", new string[2] { "chrome isn't connected", "browser connection changed" }),
		("other_clicky_tab", new string[1] { "belongs to another running clicky" }),
		("bad_request", new string[6] { "needs ", "is required", "unsupported", "must be", "only a tab", "only http" })
	};

	public static string Kind(string message)
	{
		string text = message.ToLowerInvariant();
		(string, string[])[] kinds = Kinds;
		for (int i = 0; i < kinds.Length; i++)
		{
			(string, string[]) tuple = kinds[i];
			var (result, _) = tuple;
			if (tuple.Item2.Any((string phrase) => text.Contains(phrase, StringComparison.Ordinal)))
			{
				return result;
			}
		}
		return "other";
	}

	public static string? DetachReason(string message)
	{
		int num = message.IndexOf("detached the debugger", StringComparison.Ordinal);
		if (num < 0)
		{
			return null;
		}
		int num2 = message.IndexOf('(', num);
		if (num2 < 0)
		{
			return null;
		}
		int num3 = message.IndexOf(')', num2 + 1);
		if (num3 < 0)
		{
			return null;
		}
		int num4 = num2 + 1;
		string text = message.Substring(num4, num3 - num4);
		if (text.Length == 0 || text.Length > 40 || !text.All((char c) => char.IsLetter(c) || c == '_'))
		{
			return null;
		}
		return text;
	}
}
