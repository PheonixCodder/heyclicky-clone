using System.Text.RegularExpressions;

namespace HeyClicky.Core.Telemetry;

public static class AgentFailureReason
{
	private const int MaximumKeyLength = 120;

	private static readonly (Regex Pattern, string Replacement)[] Replacements = new(Regex, string)[8]
	{
		(new Regex("[a-z][a-z0-9+.-]*://\\S+", RegexOptions.CultureInvariant), "<url>"),
		(new Regex("[a-z0-9._%+-]+@[a-z0-9.-]+\\.[a-z]+", RegexOptions.CultureInvariant), "<email>"),
		(new Regex("[a-z]:\\\\\\S+", RegexOptions.CultureInvariant), "<path>"),
		(new Regex("(?:/[^\\s/'\"`,:;()]+){2,}/?", RegexOptions.CultureInvariant), "<path>"),
		(new Regex("\\b(?=[a-z0-9_-]*\\d)[a-z0-9_-]{16,}\\b", RegexOptions.CultureInvariant), "<id>"),
		(new Regex("\\b(?=[0-9a-f]*\\d)[0-9a-f]{8,}\\b", RegexOptions.CultureInvariant), "<id>"),
		(new Regex("\\d+", RegexOptions.CultureInvariant), "#"),
		(new Regex("[{}\\[\\]\"\\\\\\s]+", RegexOptions.CultureInvariant), " ")
	};

	public static string Key(string summary)
	{
		string text = summary.ToLowerInvariant();
		(Regex, string)[] replacements = Replacements;
		for (int i = 0; i < replacements.Length; i++)
		{
			(Regex, string) tuple = replacements[i];
			Regex item = tuple.Item1;
			string item2 = tuple.Item2;
			text = item.Replace(text, item2);
		}
		text = text.Trim();
		if (text.Length > 120)
		{
			text = text.Substring(0, 120).TrimEnd();
		}
		if (text.Length != 0)
		{
			return text;
		}
		return "no_reason";
	}
}
