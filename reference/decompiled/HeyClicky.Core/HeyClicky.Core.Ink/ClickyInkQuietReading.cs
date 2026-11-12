using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkQuietReading
{
	public string? CheckedLine { get; }

	public long Runs { get; }

	public ClickyInkQuietReading(ClickyInkDocument.Block block)
	{
		string text = (block["checked"] ?? block["sources"])?.TrimWs();
		string text2 = (block["at"] ?? block["when"])?.TrimWs();
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(text))
		{
			list.Add("checked " + (text.Characters().All(InkText.IsNumber) ? (text + " sources") : text));
		}
		if (!string.IsNullOrEmpty(text2))
		{
			if (list.Count == 0)
			{
				list.Add("checked " + text2);
			}
			else
			{
				List<string> list2 = list;
				list2[0] = list2[0] + " at " + text2;
			}
		}
		string text3 = block["since"];
		if (text3 != null && text3.Length > 0)
		{
			list.Add("nothing new since " + text3);
		}
		CheckedLine = ((list.Count == 0) ? null : string.Join(" · ", list));
		Runs = Math.Max(1L, InkText.InkClamping(ClickyInkNumber.Parse(block["runs"] ?? block["streak"])?.Value ?? 1.0));
	}
}
