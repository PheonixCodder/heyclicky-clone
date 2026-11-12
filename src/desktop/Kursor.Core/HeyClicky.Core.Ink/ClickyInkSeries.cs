using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkSeries
{
	public readonly record struct Change(int Added, int Slid);

	public const int MaximumPoints = 60;

	public static List<string> Tokens(string? text)
	{
		string text2 = text?.TrimWs() ?? "";
		if (text2.Length == 0)
		{
			return new List<string>();
		}
		string s = text2.Replace("→", " ").Replace("->", " ").Replace("|", " ")
			.Replace(";", ", ");
		s = s.TrimSet("[]() ");
		List<string> source = ((!s.Contains(", ", StringComparison.Ordinal) && (!s.Contains(' ') || s.Contains(','))) ? s.Components(",").ToList() : s.Components(", ").SelectMany((string p) => p.SplitOmittingEmpty(' ')).ToList());
		List<string> list = (from p in source
			select p.TrimWs() into p
			where ClickyInkNumber.Parse(p).HasValue
			select p).ToList();
		return list.Skip(Math.Max(0, list.Count - 60)).ToList();
	}

	public static List<double> Values(string? text)
	{
		return (from n in Tokens(text).Select(ClickyInkNumber.Parse)
			where n.HasValue
			select n.Value.Value).ToList();
	}

	public static List<string> Labels(string? text)
	{
		string text2 = text?.TrimWs() ?? "";
		if (text2.Length == 0)
		{
			return new List<string>();
		}
		return (from p in text2.Components(",")
			select p.TrimWs() into p
			where p.Length > 0
			select p).ToList();
	}

	public static Change? ChangeBetween(IReadOnlyList<double> old, IReadOnlyList<double> @new)
	{
		if (old.Count == 0 || old.SequenceEqual(@new))
		{
			return null;
		}
		if (@new.Count > old.Count && @new.Take(old.Count).SequenceEqual(old))
		{
			return new Change(@new.Count - old.Count, 0);
		}
		for (int i = 1; i < Math.Min(old.Count, 8); i++)
		{
			List<double> list = old.Skip(i).ToList();
			if (list.Count > 0 && @new.Count >= list.Count && @new.Take(list.Count).SequenceEqual(list) && @new.Count - list.Count >= 1)
			{
				return new Change(@new.Count - list.Count, i);
			}
		}
		return null;
	}
}
