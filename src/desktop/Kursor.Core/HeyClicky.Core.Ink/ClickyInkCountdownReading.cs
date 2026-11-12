using System;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkCountdownReading
{
	public string Big { get; }

	public string Unit { get; }

	public string? DateLine { get; }

	public bool IsPast { get; }

	public bool IsClose { get; }

	public ClickyInkCountdownReading(ClickyInkDocument.Block block)
	{
		string text = block["to"] ?? block["due"] ?? block["at"] ?? block["date"] ?? block["when"];
		DateTimeOffset? dateTimeOffset = ClickyInkDates.Parse(text, null);
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			ClickyInkDates.Remaining remaining = ClickyInkDates.RemainingUntil(valueOrDefault, null);
			Big = remaining.Big;
			Unit = remaining.Unit;
			IsPast = remaining.IsPast;
			IsClose = remaining.IsClose;
			DateLine = ClickyInkDates.ShortLine(valueOrDefault);
			return;
		}
		string text2 = block["left"];
		if (text2 != null)
		{
			string text3 = text2.SplitOmittingEmpty(' ').FirstOrDefault();
			if (text3 != null && ClickyInkNumber.Parse(text3).HasValue)
			{
				Big = text3;
				Unit = string.Join(" ", text2.SplitOmittingEmpty(' ').Skip(1));
				IsPast = false;
				IsClose = Unit.HasPrefix("h") || Unit.HasPrefix("min");
				DateLine = text;
				return;
			}
		}
		Big = "";
		Unit = "";
		IsPast = false;
		IsClose = false;
		DateLine = text ?? block["left"];
	}
}
