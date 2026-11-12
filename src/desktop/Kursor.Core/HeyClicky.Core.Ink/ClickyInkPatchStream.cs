using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Ink;

public static class ClickyInkPatchStream
{
	public static string? PartialText(string current, string kind, string diff)
	{
		if (kind == "add")
		{
			if (diff.Length != 0)
			{
				return diff;
			}
			return null;
		}
		if (kind != "update" || diff.Length == 0)
		{
			return null;
		}
		List<string> list = current.Split('\n').ToList();
		List<List<string>> list2 = new List<List<string>>();
		string[] array = diff.Split('\n');
		foreach (string text in array)
		{
			if (text.HasPrefix("@@"))
			{
				int num = 1;
				List<string> list3 = new List<string>(num);
				CollectionsMarshal.SetCount(list3, num);
				Span<string> span = CollectionsMarshal.AsSpan(list3);
				int index = 0;
				span[index] = text;
				list2.Add(list3);
			}
			else if (list2.Count > 0)
			{
				list2[list2.Count - 1].Add(text);
			}
		}
		int num2 = 0;
		for (int j = 0; j < list2.Count; j++)
		{
			List<string> list4 = list2[j];
			bool flag = j == list2.Count - 1;
			string text2 = list4[0];
			string text3 = text2.Substring(2, text2.Length - 2).TrimWs();
			if (text3.Length > 0)
			{
				int num3 = -1;
				for (int k = num2; k < list.Count; k++)
				{
					if (Same(list[k], text3))
					{
						num3 = k;
						break;
					}
				}
				if (num3 < 0)
				{
					return null;
				}
				num2 = num3 + 1;
			}
			List<string> list5 = (from l in list4.Skip(1)
				where !l.HasPrefix("***") && !l.HasPrefix("\\")
				select l).ToList();
			if (list5.Count > 0)
			{
				if (list5[list5.Count - 1] == "")
				{
					list5.RemoveAt(list5.Count - 1);
				}
			}
			int? num4 = Location((from l in list5
				where !l.HasPrefix("+")
				select (l.Length != 0) ? l.Substring(1, l.Length - 1) : "").ToList(), list, num2);
			if (num4.HasValue)
			{
				int valueOrDefault = num4.GetValueOrDefault();
				int num5 = valueOrDefault;
				int m = 0;
				while (m < list5.Count)
				{
					string s = list5[m];
					if (!s.HasPrefix("-") && !s.HasPrefix("+"))
					{
						num5++;
						m++;
						continue;
					}
					int num6 = 0;
					List<string> list6 = new List<string>();
					for (; m < list5.Count && list5[m].HasPrefix("-"); m++)
					{
						num6++;
					}
					for (; m < list5.Count && list5[m].HasPrefix("+"); m++)
					{
						text2 = list5[m];
						list6.Add(text2.Substring(1, text2.Length - 1));
					}
					List<string> list7 = ((flag && m >= list5.Count && list6.Count < num6) ? list.GetRange(num5 + list6.Count, num6 - list6.Count) : new List<string>());
					list.RemoveRange(num5, num6);
					int index2 = num5;
					List<string> list8 = list7;
					int i = 0;
					array = new string[list6.Count + list8.Count];
					Span<string> span = CollectionsMarshal.AsSpan(list6);
					span.CopyTo(new Span<string>(array).Slice(i, span.Length));
					i += span.Length;
					Span<string> span2 = CollectionsMarshal.AsSpan(list8);
					span2.CopyTo(new Span<string>(array).Slice(i, span2.Length));
					i += span2.Length;
					list.InsertRange(index2, new global::_003C_003Ez__ReadOnlyArray<string>(array));
					num5 += list6.Count + list7.Count;
				}
				num2 = Math.Min(num5, list.Count);
				continue;
			}
			return null;
		}
		return string.Join("\n", list);
	}

	private static int? Location(List<string> old, List<string> lines, int start)
	{
		if (old.Count == 0)
		{
			return Math.Min(start, lines.Count);
		}
		if (old.Count > lines.Count || start > lines.Count - old.Count)
		{
			return null;
		}
		for (int i = start; i <= lines.Count - old.Count; i++)
		{
			bool flag = true;
			for (int j = 0; j < old.Count; j++)
			{
				if (!Same(lines[i + j], old[j]))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return i;
			}
		}
		return null;
	}

	private static bool Same(string a, string b)
	{
		return a.TrimWs() == b.TrimWs();
	}
}
