using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace HeyClicky.Agents.Jobs;

public static class ClickyWorkReplyParts
{
	private static readonly string[] MetadataBlocks = new string[9] { "NEXT_ACTIONS", "DONE_TITLE", "ARTIFACTS", "SUMMARY", "ROUTINE", "COMPUTER_USE_REQUEST", "WAIT_FOR", "LEARNED", "ANSWER" };

	public static (string Text, string? Title, string? Summary, IReadOnlyList<string> ArtifactPaths) Parse(string rawReply)
	{
		string text = rawReply;
		string text2 = FirstBlock("DONE_TITLE", text);
		string text3 = FirstBlock("SUMMARY", text) ?? ClickyAnswer.Parse(FirstBlock("ANSWER", text))?.Line;
		List<string> item = (from l in (FirstBlock("ARTIFACTS", text) ?? "").Split(['\r', '\n'])
			select l.Trim().Trim('-', '*', '•', ' ', '\t') into l
			where l.StartsWith('/') || l.StartsWith('~') || l.StartsWith("http", StringComparison.Ordinal) || IsWindowsPath(l)
			select l).Select(ExpandTilde).ToList();
		string[] metadataBlocks = MetadataBlocks;
		foreach (string value in metadataBlocks)
		{
			text = Regex.Replace(text, $"(?s)<{value}>.*?</{value}>", "");
		}
		return (Text: text.Trim(), Title: NilIfEmpty(text2?.Trim()), Summary: NilIfEmpty(text3?.Trim()), ArtifactPaths: item);
	}

	private static bool IsWindowsPath(string line)
	{
		bool flag = line.Length > 2 && char.IsLetter(line[0]) && line[1] == ':';
		if (flag)
		{
			char c = line[2];
			bool flag2 = ((c == '/' || c == '\\') ? true : false);
			flag = flag2;
		}
		if (!flag)
		{
			return line.StartsWith("\\\\", StringComparison.Ordinal);
		}
		return true;
	}

	private static string ExpandTilde(string path)
	{
		if (!path.StartsWith('~'))
		{
			return path;
		}
		return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.Substring(1, path.Length - 1).TrimStart('/', '\\'));
	}

	private static string? NilIfEmpty(string? s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s;
		}
		return null;
	}

	private static string? FirstBlock(string name, string text)
	{
		int num = text.IndexOf("<" + name + ">", StringComparison.Ordinal);
		if (num < 0)
		{
			return null;
		}
		num += name.Length + 2;
		int num2 = text.IndexOf("</" + name + ">", num, StringComparison.Ordinal);
		if (num2 >= 0)
		{
			int num3 = num;
			return text.Substring(num3, num2 - num3);
		}
		return null;
	}

	public static string FirstSentence(string markdown, int maximumLength = 220)
	{
		string input = Regex.Replace(markdown, "(?m)^#+\\s*", "");
		input = Regex.Replace(input, "[*_`>|]", "");
		input = Regex.Replace(input, "\\[([^\\]]+)\\]\\([^)]+\\)", "$1");
		input = Regex.Replace(input, "\\s+", " ").Trim(' ');
		if (input.Length == 0)
		{
			return "";
		}
		Match match = Regex.Match(input, "(?<=[.!?])\\s");
		if (match.Success && match.Index < maximumLength)
		{
			return input.Substring(0, match.Index);
		}
		if (input.Length <= maximumLength)
		{
			return input;
		}
		return input.Substring(0, maximumLength - 1) + "…";
	}
}
