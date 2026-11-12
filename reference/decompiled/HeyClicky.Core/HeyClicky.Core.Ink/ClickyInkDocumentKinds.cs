using System.Collections.Generic;

namespace HeyClicky.Core.Ink;

public static class ClickyInkDocumentKinds
{
	public static string RawValue(this ClickyInkDocument.Kind kind)
	{
		return kind.ToString().ToLowerInvariant();
	}

	public static string RawValue(this ClickyInkDocument.Mark mark)
	{
		return mark.ToString().ToLowerInvariant();
	}

	public static ClickyInkDocument.Mark? MarkFromWord(string word)
	{
		switch (word.ToLowerInvariant())
		{
		case "out":
		case "skip":
		case "ruled-out":
		case "ruledout":
		case "no":
			return ClickyInkDocument.Mark.Gone;
		case "new":
			return ClickyInkDocument.Mark.New;
		case "needs":
			return ClickyInkDocument.Mark.Needs;
		case "done":
			return ClickyInkDocument.Mark.Done;
		case "pick":
			return ClickyInkDocument.Mark.Pick;
		case "now":
			return ClickyInkDocument.Mark.Now;
		case "gone":
			return ClickyInkDocument.Mark.Gone;
		default:
			return null;
		}
	}

	public static ClickyInkDocument.Mark? FirstMark(IEnumerable<string> keys)
	{
		foreach (string key in keys)
		{
			ClickyInkDocument.Mark? mark = MarkFromWord(key);
			if (mark.HasValue)
			{
				return mark.GetValueOrDefault();
			}
		}
		return null;
	}
}
