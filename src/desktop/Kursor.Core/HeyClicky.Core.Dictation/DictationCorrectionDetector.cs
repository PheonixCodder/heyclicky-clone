using System;
using System.Linq;

namespace HeyClicky.Core.Dictation;

public static class DictationCorrectionDetector
{
	public static readonly TimeSpan ObservationWindow = TimeSpan.FromSeconds(20L);

	public static readonly TimeSpan BaselineDelay = TimeSpan.FromSeconds(0.7);

	public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2.5);

	public const int MaxFieldCharacterCount = 20000;

	private static readonly char[] WordTrim = ".,;:!?\"'()[]{}…".ToCharArray();

	public static DictationLearnedCorrection? DetectSingleWordCorrection(string baseline, string current, string insertedText)
	{
		string[] array = Tokenize(baseline);
		string[] array2 = Tokenize(current);
		if (array.Length == 0 || array.Length != array2.Length)
		{
			return null;
		}
		int? num = null;
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] == array2[i]))
			{
				if (num.HasValue)
				{
					return null;
				}
				num = i;
			}
		}
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			string text = array[valueOrDefault].Trim(WordTrim);
			string text2 = array2[valueOrDefault].Trim(WordTrim);
			if (text.Length == 0 || text2.Length == 0 || string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			if (!IsDistinctiveLearnableWord(text2))
			{
				return null;
			}
			if (!(from w in Tokenize(insertedText)
				select w.Trim(WordTrim).ToLowerInvariant()).ToHashSet().Contains(text.ToLowerInvariant()))
			{
				return null;
			}
			if (NormalizedSimilarity(text.ToLowerInvariant(), text2.ToLowerInvariant()) < 0.3)
			{
				return null;
			}
			return new DictationLearnedCorrection(text, text2);
		}
		return null;
	}

	private static string[] Tokenize(string text)
	{
		return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
	}

	private static bool IsDistinctiveLearnableWord(string word)
	{
		int length = word.Length;
		bool flag = ((length < 2 || length > 40) ? true : false);
		if (flag || !word.Any(char.IsLetter))
		{
			return false;
		}
		if (word.All(char.IsDigit))
		{
			return false;
		}
		if (char.IsUpper(word[0]))
		{
			return true;
		}
		if (word.Contains('_'))
		{
			return true;
		}
		if (word.Length <= 6 && word.All((char c) => char.IsUpper(c) || char.IsDigit(c)))
		{
			return true;
		}
		if (char.IsLower(word[0]))
		{
			return word.Skip(1).Any(char.IsUpper);
		}
		return false;
	}

	private static double NormalizedSimilarity(string a, string b)
	{
		int num = Math.Max(a.Length, b.Length);
		if (num != 0)
		{
			return 1.0 - (double)Levenshtein(a, b) / (double)num;
		}
		return 1.0;
	}

	private static int Levenshtein(string a, string b)
	{
		if (a.Length == 0)
		{
			return b.Length;
		}
		if (b.Length == 0)
		{
			return a.Length;
		}
		int[] array = new int[b.Length + 1];
		int[] array2 = new int[b.Length + 1];
		for (int i = 0; i <= b.Length; i++)
		{
			array[i] = i;
		}
		for (int j = 1; j <= a.Length; j++)
		{
			array2[0] = j;
			for (int k = 1; k <= b.Length; k++)
			{
				array2[k] = Math.Min(Math.Min(array[k] + 1, array2[k - 1] + 1), array[k - 1] + ((a[j - 1] != b[k - 1]) ? 1 : 0));
			}
			int[] array3 = array2;
			array2 = array;
			array = array3;
		}
		return array[b.Length];
	}
}
