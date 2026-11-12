using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkPipeline
{
	public readonly record struct Stage(string Label, string Value);

	public const int MaximumStages = 6;

	public static List<Stage> Stages(string? input)
	{
		string text = input?.TrimWs() ?? "";
		if (text.Length == 0)
		{
			return new List<Stage>();
		}
		string[] array = new string[7] { "›", "»", "→", "->", ">", "|", ";" };
		foreach (string oldValue in array)
		{
			text = text.Replace(oldValue, "\u0001");
		}
		if (!text.Contains('\u0001'))
		{
			text = text.Replace(", ", "\u0001");
		}
		return (from s in (from p in text.Split('\u0001')
				select p.TrimWs() into p
				where p.Length > 0
				select p).Select(StageOf)
			where s.HasValue
			select s.Value).Take(6).ToList();
	}

	public static Stage? StageOf(string piece)
	{
		int num = piece.LastIndexOf(':');
		if (num >= 0)
		{
			string text = piece.Substring(0, num).TrimWs();
			int num2 = num + 1;
			string value = piece.Substring(num2, piece.Length - num2).TrimWs();
			if (text.Length > 0)
			{
				return new Stage(text, value);
			}
		}
		List<string> list = piece.SplitOmittingEmpty(' ');
		if (list.Count > 1)
		{
			if (ClickyInkNumber.Parse(list[list.Count - 1]).HasValue)
			{
				return new Stage(string.Join(" ", list.Take(list.Count - 1)), list[list.Count - 1]);
			}
		}
		if (list.Count > 1 && ClickyInkNumber.Parse(list[0]).HasValue)
		{
			return new Stage(string.Join(" ", list.Skip(1)), list[0]);
		}
		if (piece.Length != 0)
		{
			return new Stage(piece, "");
		}
		return null;
	}

	public static string Attribute(IEnumerable<Stage> stages)
	{
		return string.Join(" › ", stages.Select((Stage s) => (s.Value.Length != 0) ? (s.Label + ": " + s.Value) : s.Label));
	}
}
