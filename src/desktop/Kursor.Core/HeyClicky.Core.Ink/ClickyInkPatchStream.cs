using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
				list2.Add(new List<string> { text });
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
			string anchor = text2.Substring(2, text2.Length - 2).Trim();
			if (anchor.EndsWith("@@", StringComparison.Ordinal))
			{
				anchor = anchor.Substring(0, anchor.Length - 2).Trim();
			}
			if (anchor.Length > 0)
			{
				if (Regex.IsMatch(anchor, @"^-\d+(?:,\d+)?\s+\+\d+(?:,\d+)?$"))
				{
					Match rangeMatch = Regex.Match(anchor, @"^-(\d+)");
					if (rangeMatch.Success && int.TryParse(rangeMatch.Groups[1].Value, out int parsedLine) && parsedLine > 0)
					{
						num2 = Math.Clamp(parsedLine - 1, 0, list.Count);
					}
				}
				else
				{
					Match headerWithTrailingContext = Regex.Match(anchor, @"^-(\d+)(?:,\d+)?\s+\+\d+(?:,\d+)?\s+@@\s*(.*)$");
					if (headerWithTrailingContext.Success)
					{
						string trailing = headerWithTrailingContext.Groups[2].Value.Trim();
						if (trailing.Length > 0)
						{
							int num3 = -1;
							for (int k = num2; k < list.Count; k++)
							{
								if (Same(list[k], trailing))
								{
									num3 = k;
									break;
								}
							}
							if (num3 >= 0)
							{
								num2 = num3 + 1;
							}
						}
						else if (int.TryParse(headerWithTrailingContext.Groups[1].Value, out int parsedLine) && parsedLine > 0)
						{
							num2 = Math.Clamp(parsedLine - 1, 0, list.Count);
						}
					}
					else
					{
						int num3 = -1;
						for (int k = num2; k < list.Count; k++)
						{
							if (Same(list[k], anchor))
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
				}
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
			List<string> oldLines = (from l in list5
				where !l.HasPrefix("+")
				select (l.StartsWith(" ") || l.StartsWith("+") || l.StartsWith("-")) ? l.Substring(1) : l).ToList();
			int? num4 = Location(oldLines, list, num2) ?? ((num2 > 0 && oldLines.Count > 0) ? Location(oldLines, list, 0) : null);
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
						list6.Add((text2.StartsWith(" ") || text2.StartsWith("+") || text2.StartsWith("-")) ? text2.Substring(1) : text2);
					}
					List<string> list7 = ((flag && m >= list5.Count && list6.Count < num6) ? list.GetRange(num5 + list6.Count, num6 - list6.Count) : new List<string>());
					list.RemoveRange(num5, num6);
					list.InsertRange(num5, list6.Concat(list7));
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
