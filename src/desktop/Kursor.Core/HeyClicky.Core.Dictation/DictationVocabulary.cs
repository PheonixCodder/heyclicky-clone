using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HeyClicky.Core.Settings;

namespace HeyClicky.Core.Dictation;

public static class DictationVocabulary
{
	public const int MaxTermCount = 50;

	public const int MaxTermCharacterCount = 60;

	private const int MaxContextKeytermCount = 12;

	private static readonly char[] PunctuationTrim = ".,;:!?\"'()[]{}".ToCharArray();

	public static IReadOnlyList<string> Terms(IEnumerable<string> rawLines)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		foreach (string rawLine in rawLines)
		{
			string[] array = rawLine.Split([',', '\n', '\r', ';']);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length != 0 && text.Length <= 60 && hashSet.Add(text))
				{
					list.Add(text);
					if (list.Count >= 50)
					{
						return list;
					}
				}
			}
		}
		return list;
	}

	public static IReadOnlyList<string> SelectedTerms(SettingsStore settings)
	{
		return Terms(settings.Read((AppSettings s) => s.DictationCustomWords.ToList()));
	}

	public static bool AddTerm(SettingsStore settings, string term)
	{
		string trimmed = term.Trim();
		if (trimmed.Length == 0 || trimmed.Length > 60)
		{
			return false;
		}
		if (SelectedTerms(settings).Any((string t) => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		settings.Update(delegate(AppSettings s)
		{
			s.DictationCustomWords.Add(trimmed);
		});
		return true;
	}

	public static void AddBuiltInTermsIfNeeded(SettingsStore settings)
	{
		if (!settings.Read((AppSettings s) => s.DictationClaudeSeeded))
		{
			AddTerm(settings, "Claude");
			settings.Update(delegate(AppSettings s)
			{
				s.DictationClaudeSeeded = true;
			});
		}
	}

	public static void AddAccountNameIfNeeded(SettingsStore settings, string? displayName)
	{
		string name = displayName?.Trim();
		if (string.IsNullOrEmpty(name) || name.Contains('@') || settings.Read((AppSettings s) => s.DictationAccountNameSeeded) == name)
		{
			return;
		}
		foreach (string item in (from word in name.Split((char[]?)null, StringSplitOptions.None).Select(delegate(string word)
			{
				int i = 0;
				int num;
				for (num = word.Length; i < num && !Keeps(word[i]); i++)
				{
				}
				while (num > i && !Keeps(word[num - 1]))
				{
					num--;
				}
				int num2 = i;
				return word.Substring(num2, num - num2);
			})
			where word.Length >= 2
			select word).Take(3))
		{
			AddTerm(settings, item);
		}
		settings.Update(delegate(AppSettings s)
		{
			s.DictationAccountNameSeeded = name;
		});
		static bool Keeps(char c)
		{
			bool flag = char.IsLetter(c);
			if (!flag)
			{
				bool flag2 = ((c == '\'' || c == '-') ? true : false);
				flag = flag2;
			}
			return flag;
		}
	}

	public static void RemoveTerm(SettingsStore settings, string term)
	{
		string normalized = term.Trim();
		if (normalized.Length == 0)
		{
			return;
		}
		settings.Update(delegate(AppSettings s)
		{
			List<string> list = new List<string>();
			foreach (string dictationCustomWord in s.DictationCustomWords)
			{
				List<string> list2 = (from p in dictationCustomWord.Split(',')
					where !string.Equals(p.Trim(), normalized, StringComparison.OrdinalIgnoreCase)
					select p).ToList();
				if (!list2.All((string p) => p.Trim().Length == 0))
				{
					list.Add(string.Join(",", list2));
				}
			}
			s.DictationCustomWords = list;
		});
	}

	public static IReadOnlyList<string> ContextKeyterms(string surroundingText)
	{
		if (string.IsNullOrEmpty(surroundingText))
		{
			return Array.Empty<string>();
		}
		string[] array = surroundingText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		List<string> results = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int num = 0;
		while (num < array.Length && results.Count < 12)
		{
			string text = array[num];
			if (StartsCapitalized(text))
			{
				int i;
				for (i = num + 1; i < array.Length && StartsCapitalized(array[i]); i++)
				{
				}
				if (i - num >= 2)
				{
					Append(string.Join(" ", array[num..i]));
					num = i;
					continue;
				}
			}
			if (IsAcronymOrIdentifier(text))
			{
				Append(text);
			}
			num++;
		}
		return results;
		void Append(string candidate)
		{
			string text2 = candidate.Trim(PunctuationTrim);
			if (text2.Length >= 2 && text2.Length <= 60 && seen.Add(text2))
			{
				results.Add(text2);
			}
		}
	}

	private static bool StartsCapitalized(string word)
	{
		if (word.Length > 0)
		{
			return char.IsUpper(word[0]);
		}
		return false;
	}

	private static bool IsAcronymOrIdentifier(string word)
	{
		string text = word.Trim(PunctuationTrim);
		if (text.Length < 2)
		{
			return false;
		}
		if (text.Contains('_'))
		{
			return true;
		}
		if (text.Length <= 6 && text.All((char c) => char.IsUpper(c) || char.IsDigit(c)) && text.Any(char.IsLetter))
		{
			return true;
		}
		if (char.IsLower(text[0]))
		{
			return text.Skip(1).Any(char.IsUpper);
		}
		return false;
	}
}
