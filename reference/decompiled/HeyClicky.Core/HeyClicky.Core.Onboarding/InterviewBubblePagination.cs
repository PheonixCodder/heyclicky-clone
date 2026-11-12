using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Onboarding;

public sealed class InterviewBubblePagination
{
	private static readonly string[] Abbreviations = new string[7] { "mr.", "mrs.", "ms.", "dr.", "e.g.", "i.e.", "etc." };

	private string source = "";

	private readonly List<string> bubbles = new List<string>();

	private bool startsNextBubble;

	public IReadOnlyList<string> Bubbles => bubbles;

	public void Update(string text, Func<string, bool> fits)
	{
		if (!text.StartsWith(source, StringComparison.Ordinal))
		{
			source = "";
			bubbles.Clear();
			startsNextBubble = false;
		}
		int length = source.Length;
		string text2 = text.Substring(length, text.Length - length);
		for (int i = 0; i < text2.Length; i++)
		{
			char c = text2[i];
			if (bubbles.Count == 0)
			{
				bubbles.Add("");
			}
			if (char.IsWhiteSpace(c))
			{
				if (c != '\n')
				{
					List<string> list = bubbles;
					if (!EndsSentence(list[list.Count - 1]))
					{
						List<string> list2 = bubbles;
						list2[list2.Count - 1] += c;
						continue;
					}
				}
				startsNextBubble = true;
				continue;
			}
			if (startsNextBubble)
			{
				bubbles.Add("");
				startsNextBubble = false;
			}
			List<string> list3 = bubbles;
			string text3 = list3[list3.Count - 1] + c;
			List<string> list4 = bubbles;
			if (list4[list4.Count - 1].Length > 0 && !fits(text3))
			{
				bubbles.Add(c.ToString());
				continue;
			}
			List<string> list5 = bubbles;
			list5[list5.Count - 1] = text3;
		}
		source = text;
	}

	private static bool EndsSentence(string text)
	{
		string text2 = text.Trim().TrimEnd('"', '\'', '”', '’', ')', ']');
		if (text2.Length != 0)
		{
			if (".!?。！？".Contains(text2[text2.Length - 1]))
			{
				string text3 = text2.Split(' ').Last().ToLowerInvariant();
				if (Abbreviations.Contains(text3))
				{
					return false;
				}
				if (text3.Length == 2 && char.IsLetter(text3[0]))
				{
					return false;
				}
				return true;
			}
		}
		return false;
	}
}
