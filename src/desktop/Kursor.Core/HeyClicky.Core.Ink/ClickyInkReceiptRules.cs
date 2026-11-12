using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkReceiptRules
{
	public static bool IsTotalLine(ClickyInkDocument.Item item)
	{
		string text = ((item.Cells.Count > 0) ? item.Cells[0] : item.Text).ToLowerInvariant().TrimWs();
		if (!(text == "total") && !text.HasPrefix("total "))
		{
			return text == "grand total";
		}
		return true;
	}

	public static double Width(ClickyInkDocument.Block block)
	{
		return (((block.Items.Count != 0) ? block.Items.Max((ClickyInkDocument.Item i) => ((i.Cells.Count > 0) ? i.Cells[0] : i.Text).CharacterCount() + ((i.Cells.Count > 1) ? i.Cells[1].CharacterCount() : 0)) : 0) > 30) ? 300 : 250;
	}

	public static string? Total(ClickyInkDocument.Block block)
	{
		string text = block["total"];
		if (text != null && text.Length > 0)
		{
			return text;
		}
		ClickyInkDocument.Item item = block.Items.FirstOrDefault(IsTotalLine);
		if ((object)item != null && item.Cells.Count > 1)
		{
			return item.Cells[1];
		}
		double num = 0.0;
		string text2 = "";
		foreach (ClickyInkDocument.Item item2 in block.Items)
		{
			if (item2.Cells.Count <= 1)
			{
				return null;
			}
			string[] source = item2.Cells[1].Characters();
			string text3 = string.Concat(source.TakeWhile((string c) => !InkText.IsNumber(c) && c != "-")).TrimWs();
			double? num2 = InkText.ParseDouble(string.Concat(source.Where((string c) => InkText.IsNumber(c) || c == ".")));
			if (num2.HasValue)
			{
				double valueOrDefault = num2.GetValueOrDefault();
				if (text2.Length == 0)
				{
					text2 = text3;
				}
				else if (text2 != text3)
				{
					return null;
				}
				num += valueOrDefault;
				continue;
			}
			return null;
		}
		if (!(num > 0.0))
		{
			return null;
		}
		int decimals = ((num % 1.0 != 0.0) ? 2 : 0);
		return text2 + ClickyInkNumberFormatting.Decimal(num, decimals, grouping: true);
	}
}
