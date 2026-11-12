using System;
using System.Globalization;

namespace HeyClicky.Core.Ink;

public static class ClickyInkNumberFormatting
{
	public static string Decimal(double magnitude, int decimals, bool grouping)
	{
		string text = (grouping ? "#,##0" : "0") + ((decimals > 0) ? ("." + new string('0', decimals)) : "");
		try
		{
			decimal d = decimal.Parse(magnitude.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
			return Math.Round(d, Math.Min(decimals, 28), MidpointRounding.ToEven).ToString(text, CultureInfo.InvariantCulture);
		}
		catch (Exception) when (1 != 0)
		{
			return magnitude.ToString(text, CultureInfo.InvariantCulture);
		}
	}

	public static string Fixed(double value, int decimals)
	{
		try
		{
			return Math.Round(decimal.Parse(value.ToString("G17", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture), decimals, MidpointRounding.ToEven).ToString("F" + decimals, CultureInfo.InvariantCulture);
		}
		catch (Exception) when (1 != 0)
		{
			return value.ToString("F" + decimals, CultureInfo.InvariantCulture);
		}
	}
}
