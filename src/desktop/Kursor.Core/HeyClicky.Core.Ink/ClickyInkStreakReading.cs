using System.Collections.Generic;
using System.Globalization;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkStreakReading
{
	public List<ClickyInkStreakDay> Days { get; }

	public string Shown { get; }

	public string Unit { get; }

	public string? PreviousShown { get; }

	public int? PreviousCount { get; }

	public ClickyInkStreakReading(ClickyInkDocument.Block block)
	{
		string key = ((block["days"] != null) ? "days" : ((block["marks"] != null) ? "marks" : "week"));
		Days = ClickyInkStreak.Days(block[key]);
		string text = block["count"];
		long num2;
		if (text != null)
		{
			double? num = ClickyInkNumber.Parse(text)?.Value;
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				num2 = InkText.InkClamping(valueOrDefault);
				goto IL_00b0;
			}
		}
		num2 = ClickyInkStreak.CurrentStreak(Days);
		goto IL_00b0;
		IL_00b0:
		long num3 = num2;
		Shown = num3.ToString(CultureInfo.InvariantCulture);
		Unit = block["unit"] ?? ((num3 == 1) ? "day in a row" : "days in a row");
		string text2 = ClickyInkChange.Previous(block, key);
		if (text2 == null)
		{
			return;
		}
		List<ClickyInkStreakDay> list = ClickyInkStreak.Days(text2);
		PreviousCount = list.Count;
		string text3 = ClickyInkChange.Previous(block, "count");
		long num4;
		if (text3 != null)
		{
			double? num = ClickyInkNumber.Parse(text3)?.Value;
			if (num.HasValue)
			{
				double valueOrDefault2 = num.GetValueOrDefault();
				num4 = InkText.InkClamping(valueOrDefault2);
				goto IL_017f;
			}
		}
		num4 = ClickyInkStreak.CurrentStreak(list);
		goto IL_017f;
		IL_017f:
		long num5 = num4;
		PreviousShown = num5.ToString(CultureInfo.InvariantCulture);
	}
}
