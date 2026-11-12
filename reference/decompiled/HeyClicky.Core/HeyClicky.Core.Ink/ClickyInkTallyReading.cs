using System;
using System.Linq;

namespace HeyClicky.Core.Ink;

public readonly record struct ClickyInkTallyReading
{
	public string Key { get; }

	public long Count { get; }

	public long Fresh { get; }

	public ClickyInkTallyReading(ClickyInkDocument.Block block, int? latestEdit = null)
	{
		Key = new string[3] { "value", "count", "n" }.FirstOrDefault((string k) => block[k] != null) ?? "value";
		Count = Math.Max(0L, InkText.InkClamping(Math.Round(ClickyInkNumber.Parse(block[Key])?.Value ?? 0.0, MidpointRounding.AwayFromZero)));
		string text = block["new"];
		if (text != null)
		{
			double? num = ClickyInkNumber.Parse(text)?.Value;
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				Fresh = Math.Max(0L, InkText.InkClamping(valueOrDefault));
				return;
			}
		}
		if (ClickyInkChange.IsLatest(block, latestEdit))
		{
			string text2 = ClickyInkChange.Previous(block, Key);
			if (text2 != null)
			{
				double? num = ClickyInkNumber.Parse(text2)?.Value;
				if (num.HasValue)
				{
					double valueOrDefault2 = num.GetValueOrDefault();
					Fresh = Math.Max(0L, Count - InkText.InkClamping(Math.Round(valueOrDefault2, MidpointRounding.AwayFromZero)));
					return;
				}
			}
		}
		Fresh = 0L;
	}
}
