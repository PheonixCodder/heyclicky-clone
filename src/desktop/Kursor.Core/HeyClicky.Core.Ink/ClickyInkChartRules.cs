using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkChartRules
{
	public static string ValueWithUnit(string value, string unit)
	{
		string text = value.TrimWs();
		string text2 = unit.TrimWs();
		if (text.Length != 0 && text2.Length != 0)
		{
			ClickyInkNumber? clickyInkNumber = ClickyInkNumber.Parse(text);
			if (clickyInkNumber.HasValue)
			{
				ClickyInkNumber valueOrDefault = clickyInkNumber.GetValueOrDefault();
				if (valueOrDefault.Prefix.Length == 0 && valueOrDefault.Suffix.TrimWs().Length == 0)
				{
					if (new string[9] { "$", "€", "£", "¥", "₹", "₩", "R$", "A$", "C$" }.Contains(text2))
					{
						return text2 + text;
					}
					string text3 = text2.Characters().FirstOrDefault();
					return text + ((text3 != null && InkText.IsLetter(text3)) ? " " : "") + text2;
				}
			}
		}
		return text;
	}

	public static List<double> Values(ClickyInkDocument.Block block)
	{
		return block.Items.Select(delegate(ClickyInkDocument.Item item)
		{
			string text = ((item.Cells.Count > 1) ? item.Cells[1] : item.Text);
			return ClickyInkNumber.Parse(text)?.Value ?? InkText.ParseDouble(string.Concat(from c in text.Characters()
				where InkText.IsNumber(c) || c == "." || c == "-"
				select c)).GetValueOrDefault();
		}).ToList();
	}

	public static double LabelWidth(ClickyInkDocument.Block block)
	{
		int num = ((block.Items.Count == 0) ? 8 : block.Items.Max((ClickyInkDocument.Item i) => ((i.Cells.Count > 0) ? i.Cells[0] : i.Text).CharacterCount()));
		return Math.Min(150.0, Math.Max(70.0, (double)num * 7.2));
	}
}
