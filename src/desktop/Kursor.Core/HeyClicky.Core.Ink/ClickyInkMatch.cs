using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HeyClicky.Core.Ink;

public static class ClickyInkMatch
{
	public enum Change
	{
		Kept,
		Removed,
		Added
	}

	public static List<string> Words(string text)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		foreach (Rune item in text.Lowered().EnumerateRunes())
		{
			UnicodeCategory unicodeCategory = Rune.GetUnicodeCategory(item);
			bool flag2;
			if ((uint)(unicodeCategory - 5) <= 2u)
			{
				flag2 = flag;
			}
			else
			{
				bool flag3 = Rune.IsLetter(item) || Rune.IsNumber(item);
				if (!flag3)
				{
					int value = item.Value;
					bool flag4 = ((value == 36 || value == 46) ? true : false);
					flag3 = flag4;
				}
				flag2 = flag3;
			}
			bool num = flag2;
			if (num)
			{
				stringBuilder.Append(item.ToString());
			}
			else if (stringBuilder.Length > 0)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
			}
			flag = num;
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	public static double Similarity(string a, string b)
	{
		if (a == b)
		{
			return 1.0;
		}
		HashSet<string> hashSet = Words(ClickyInkParser.Emphasis.Strip(a)).ToHashSet();
		HashSet<string> hashSet2 = Words(ClickyInkParser.Emphasis.Strip(b)).ToHashSet();
		if (hashSet.Count == 0 && hashSet2.Count == 0)
		{
			return 0.0;
		}
		int num = hashSet.Count(hashSet2.Contains);
		int num2 = hashSet.Count + hashSet2.Count - num;
		return (double)num / (double)num2;
	}

	public static string Signature(ClickyInkDocument.Block block)
	{
		List<string> list = new List<string> { block.Text };
		list.AddRange(block.Columns);
		list.AddRange(from i in block.Items.Take(8)
			select i.Text);
		string[] array = new string[7] { "to", "from", "subject", "path", "query", "url", "title" };
		foreach (string key in array)
		{
			string text = block[key];
			if (text != null)
			{
				list.Add(text);
			}
		}
		return string.Join(" ", list);
	}

	public static bool SameFamily(ClickyInkDocument.Kind a, ClickyInkDocument.Kind b)
	{
		if (a == b)
		{
			return true;
		}
		if (!IsList(a) || !IsList(b))
		{
			if (IsProse(a))
			{
				return IsProse(b);
			}
			return false;
		}
		return true;
		static bool IsList(ClickyInkDocument.Kind k)
		{
			if (k == ClickyInkDocument.Kind.List || k == ClickyInkDocument.Kind.Numbered)
			{
				return true;
			}
			return false;
		}
		static bool IsProse(ClickyInkDocument.Kind k)
		{
			if (k == ClickyInkDocument.Kind.Paragraph || k == ClickyInkDocument.Kind.Quote || k == ClickyInkDocument.Kind.Note)
			{
				return true;
			}
			return false;
		}
	}

	public static bool IsStillCatchingUp(ClickyInkDocument.Block old, ClickyInkDocument.Block @new)
	{
		string s = ClickyInkParser.Emphasis.Strip(old.Text);
		string s2 = ClickyInkParser.Emphasis.Strip(@new.Text);
		if (@new.Items.Count < old.Items.Count)
		{
			return true;
		}
		if (s2.CharacterCount() < s.CharacterCount())
		{
			return s.Lowered().HasPrefix(s2.Lowered());
		}
		return false;
	}

	public static List<string> Tokens(string text)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = text.Characters();
		foreach (string text2 in array)
		{
			stringBuilder.Append(text2);
			if (text2 == " ")
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	public static List<(string Piece, Change Change)> Lcs(List<string> a, List<string> b)
	{
		int count = a.Count;
		int count2 = b.Count;
		if ((long)count * (long)count2 >= 200000)
		{
			List<(string, Change)> list = new List<(string, Change)>();
			list.AddRange(a.Select((string x) => (x: x, Removed: Change.Removed)));
			list.AddRange(b.Select((string x) => (x: x, Added: Change.Added)));
			return list;
		}
		string[] array = a.Select(Key).ToArray();
		string[] array2 = b.Select(Key).ToArray();
		int[,] array3 = new int[count + 1, count2 + 1];
		for (int num = count - 1; num >= 0; num--)
		{
			for (int num2 = count2 - 1; num2 >= 0; num2--)
			{
				array3[num, num2] = ((array[num] == array2[num2]) ? (array3[num + 1, num2 + 1] + 1) : Math.Max(array3[num + 1, num2], array3[num, num2 + 1]));
			}
		}
		List<(string, Change)> list2 = new List<(string, Change)>();
		int i = 0;
		int j = 0;
		while (i < count && j < count2)
		{
			if (array[i] == array2[j])
			{
				list2.Add((b[j], Change.Kept));
				i++;
				j++;
			}
			else if (array3[i + 1, j] >= array3[i, j + 1])
			{
				list2.Add((a[i], Change.Removed));
				i++;
			}
			else
			{
				list2.Add((b[j], Change.Added));
				j++;
			}
		}
		for (; i < count; i++)
		{
			list2.Add((a[i], Change.Removed));
		}
		for (; j < count2; j++)
		{
			list2.Add((b[j], Change.Added));
		}
		return list2;
		static string Key(string s)
		{
			return ClickyInkParser.Emphasis.Strip(s).TrimWs().Lowered();
		}
	}

	public static List<ClickyInkLiveNote.Segment> Merged(List<ClickyInkLiveNote.Segment> segments)
	{
		List<ClickyInkLiveNote.Segment> list = new List<ClickyInkLiveNote.Segment>();
		foreach (ClickyInkLiveNote.Segment segment2 in segments)
		{
			if (list.Count > 0)
			{
				ClickyInkLiveNote.Segment segment = list[list.Count - 1];
				InkRange? struck = segment.Struck;
				InkRange? struck2 = segment2.Struck;
				if (struck.HasValue == struck2.HasValue && (!struck.HasValue || struck.GetValueOrDefault() == struck2.GetValueOrDefault()) && segment.VanishesAt == segment2.VanishesAt && segment.Edit == segment2.Edit && segment.IsPreview == segment2.IsPreview)
				{
					struck2 = segment.Written;
					if (struck2.HasValue)
					{
						InkRange valueOrDefault = struck2.GetValueOrDefault();
						struck2 = segment2.Written;
						if (struck2.HasValue)
						{
							InkRange valueOrDefault2 = struck2.GetValueOrDefault();
							if (Math.Abs(valueOrDefault.UpperBound - valueOrDefault2.LowerBound) < 0.001)
							{
								list[list.Count - 1] = segment with
								{
									Text = segment.Text + segment2.Text,
									Written = new InkRange(valueOrDefault.LowerBound, valueOrDefault2.UpperBound)
								};
								continue;
							}
						}
					}
				}
				if (!segment.Written.HasValue && !segment2.Written.HasValue)
				{
					struck2 = segment.Struck;
					struck = segment2.Struck;
					if (struck2.HasValue == struck.HasValue && (!struck2.HasValue || struck2.GetValueOrDefault() == struck.GetValueOrDefault()) && segment.VanishesAt == segment2.VanishesAt && segment.Edit == segment2.Edit && segment.IsPreview == segment2.IsPreview)
					{
						list[list.Count - 1] = segment with
						{
							Text = segment.Text + segment2.Text
						};
						continue;
					}
				}
			}
			list.Add(segment2);
		}
		return list;
	}
}
