using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkObjectKey
{
	public static string Normalized(string text)
	{
		string s = ClickyInkParser.Emphasis.Strip(text).Lowered();
		return string.Join(" ", s.SplitCharacters(InkText.IsWhitespace)).TrimSet(" .,:;!?…\"'“”‘’");
	}

	public static string Of(ClickyInkDocument.Block block)
	{
		switch (block.Kind)
		{
		case ClickyInkDocument.Kind.Ask:
			return "ask|" + Normalized(block["say"] ?? block["ask"] ?? block.Text);
		case ClickyInkDocument.Kind.Question:
			return "question|" + Normalized(block.Text);
		case ClickyInkDocument.Kind.Letter:
		{
			string text2 = block["to"] ?? "";
			string text3 = block["subject"] ?? "";
			if (text2.Length == 0 && text3.Length == 0)
			{
				return "letter|" + Normalized(block.Text.PrefixCharacters(80));
			}
			return "letter|" + Normalized(text2) + "|" + Normalized(text3);
		}
		case ClickyInkDocument.Kind.Review:
		{
			string text = ClickyInkReviewRules.Label(block);
			if (text.Length > 0)
			{
				return "review|" + Normalized(text);
			}
			return "review|" + string.Join("|", from i in block.Items.Take(3)
				select Normalized(i.Text));
		}
		default:
			return block.Kind.RawValue() + "|" + Normalized(block.Text);
		}
	}
}
