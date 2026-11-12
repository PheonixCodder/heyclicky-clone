using System;
using System.Collections.Generic;
using System.Globalization;

namespace HeyClicky.Core.Ink;

public readonly record struct ClickyInkProgress
{
	public double? Fraction { get; }

	public string Shown { get; }

	public double Fill => Math.Min(1.0, Math.Max(0.0, Fraction.GetValueOrDefault()));

	public ClickyInkProgress(IReadOnlyDictionary<string, string> attributes)
	{
		string text = (Get("value") ?? Get("progress") ?? Get("percent") ?? Get("done"))?.TrimWs() ?? "";
		string text2 = Get("of") ?? Get("total") ?? Get("goal") ?? Get("max") ?? Get("target");
		double? num = ((text2 == null) ? ((double?)null) : ClickyInkNumber.Parse(text2)?.Value);
		string[] array = new string[2] { "/", " of " };
		foreach (string text3 in array)
		{
			if (!text.Contains(text3, StringComparison.Ordinal))
			{
				continue;
			}
			string[] array2 = text.Components(text3);
			if (array2.Length != 2)
			{
				continue;
			}
			double? num2 = ClickyInkNumber.Parse(array2[0])?.Value;
			if (!num2.HasValue)
			{
				continue;
			}
			double valueOrDefault = num2.GetValueOrDefault();
			num2 = ClickyInkNumber.Parse(array2[1])?.Value;
			if (num2.HasValue)
			{
				double valueOrDefault2 = num2.GetValueOrDefault();
				if (valueOrDefault2 > 0.0)
				{
					Fraction = valueOrDefault / valueOrDefault2;
					Shown = Compact(valueOrDefault) + " of " + Compact(valueOrDefault2);
					return;
				}
			}
		}
		ClickyInkNumber? clickyInkNumber = ClickyInkNumber.Parse(text);
		if (clickyInkNumber.HasValue)
		{
			ClickyInkNumber valueOrDefault3 = clickyInkNumber.GetValueOrDefault();
			if (num.HasValue)
			{
				double valueOrDefault4 = num.GetValueOrDefault();
				if (valueOrDefault4 > 0.0)
				{
					Fraction = valueOrDefault3.Value / valueOrDefault4;
					Shown = Compact(valueOrDefault3.Value) + " of " + Compact(valueOrDefault4);
					return;
				}
			}
			if (valueOrDefault3.Suffix.TrimWs() == "%" || valueOrDefault3.Value > 1.0)
			{
				Fraction = valueOrDefault3.Value / 100.0;
				Shown = Compact(valueOrDefault3.Value) + "%";
			}
			else
			{
				Fraction = valueOrDefault3.Value;
				Shown = Compact(valueOrDefault3.Value * 100.0) + "%";
			}
		}
		else
		{
			Fraction = null;
			Shown = ((text.CharacterCount() <= 12) ? text : "");
		}
		string? Get(string key)
		{
			if (!attributes.TryGetValue(key, out string value))
			{
				return null;
			}
			return value;
		}
	}

	private static string Compact(double value)
	{
		if (Math.Round(value, MidpointRounding.ToEven) != value)
		{
			return ClickyInkNumberFormatting.Fixed(value, 1);
		}
		return InkText.InkClamping(value).ToString(CultureInfo.InvariantCulture);
	}
}
