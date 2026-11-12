using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Core.Ink;

public record struct ClickyInkNumber
{
	public double Value { get; set; }

	public string Prefix { get; set; }

	public string Suffix { get; set; }

	public int Decimals { get; set; }

	public bool UsesGrouping { get; set; }

	public double Scale { get; set; }

	public bool ShowsPlus { get; set; }

	// [GeneratedRegex("^([+-]?)([^\\d+\\-.]{0,4}?)([+-]?)(\\d[\\d,]*(?:\\.\\d+)?|\\.\\d+)(.*)$")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex NumberRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__NumberRegex_36.Instance;
	}

	public static ClickyInkNumber? Parse(string? input)
	{
		if (input == null)
		{
			return null;
		}
		string text = input.TrimWs();
		if (text.Length == 0 || text.CharacterCount() > 40)
		{
			return null;
		}
		text = text.Replace("−", "-").Replace("–", "-");
		Match match = NumberRegex().Match(text);
		if (!match.Success)
		{
			return null;
		}
		string text2 = match.Groups[1].Value + match.Groups[3].Value;
		string value = match.Groups[2].Value;
		if (value.Characters().Any(InkText.IsLetter))
		{
			return null;
		}
		string value2 = match.Groups[4].Value;
		string value3 = match.Groups[5].Value;
		double? num = InkText.ParseDouble(value2.Replace(",", ""));
		string[] lowerCharacters;
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			double num2 = 1.0;
			string s = value3.Lowered();
			lowerCharacters = s.Characters();
			if (s.HasPrefix("bn") && AfterLetter(2))
			{
				num2 = 1000000000.0;
			}
			else if (s.HasPrefix("k") && AfterLetter(1))
			{
				num2 = 1000.0;
			}
			else if (s.HasPrefix("m") && AfterLetter(1) && !s.HasPrefix("min") && !s.HasPrefix("mo"))
			{
				num2 = 1000000.0;
			}
			else if (s.HasPrefix("b") && AfterLetter(1))
			{
				num2 = 1000000000.0;
			}
			bool flag = text2.Contains('-');
			string[] array = value2.Split('.');
			ClickyInkNumber value4 = default(ClickyInkNumber);
			value4.Value = (flag ? (0.0 - valueOrDefault) : valueOrDefault) * num2;
			value4.Prefix = value;
			value4.Suffix = value3;
			value4.Decimals = ((array.Length > 1) ? array[1].CharacterCount() : 0);
			value4.UsesGrouping = value2.Contains(',');
			value4.Scale = num2;
			value4.ShowsPlus = text2.Contains('+');
			return value4;
		}
		return null;
		bool AfterLetter(int count)
		{
			if (lowerCharacters.Length != count)
			{
				if (count < lowerCharacters.Length)
				{
					return !InkText.IsLetter(lowerCharacters[count]);
				}
				return true;
			}
			return true;
		}
	}

	public readonly string Formatted(double newValue)
	{
		double num = newValue / Scale;
		string text = ClickyInkNumberFormatting.Decimal(Math.Abs(num), Decimals, UsesGrouping);
		bool flag = InkText.ParseDouble(text.Replace(",", "")).GetValueOrDefault() == 0.0;
		return ((num < 0.0 && !flag) ? "-" : ((ShowsPlus && !flag) ? "+" : "")) + Prefix + text + Suffix;
	}

	public readonly bool IsSameKind(ClickyInkNumber other)
	{
		if (Prefix == other.Prefix)
		{
			return Suffix.TrimWs().Lowered() == other.Suffix.TrimWs().Lowered();
		}
		return false;
	}

	public static string? Interpolated(string? from, string? to, double progress)
	{
		if (to != null)
		{
			ClickyInkNumber? clickyInkNumber = Parse(to);
			if (clickyInkNumber.HasValue)
			{
				ClickyInkNumber valueOrDefault = clickyInkNumber.GetValueOrDefault();
				if (progress >= 1.0)
				{
					return to;
				}
				if (from != null)
				{
					clickyInkNumber = Parse(from);
					if (clickyInkNumber.HasValue)
					{
						ClickyInkNumber valueOrDefault2 = clickyInkNumber.GetValueOrDefault();
						if (valueOrDefault2.IsSameKind(valueOrDefault))
						{
							ClickyInkNumber clickyInkNumber2 = valueOrDefault with
							{
								Decimals = Math.Max(valueOrDefault.Decimals, valueOrDefault2.Decimals),
								UsesGrouping = (valueOrDefault.UsesGrouping || valueOrDefault2.UsesGrouping)
							};
							if (valueOrDefault2.Scale != valueOrDefault.Scale)
							{
								clickyInkNumber2.Scale = valueOrDefault.Scale;
							}
							return clickyInkNumber2.Formatted(valueOrDefault2.Value + (valueOrDefault.Value - valueOrDefault2.Value) * Math.Min(1.0, Math.Max(0.0, progress)));
						}
					}
				}
				return null;
			}
		}
		return null;
	}

	public static (string Text, double Value)? Difference(string? from, string? to)
	{
		if (from != null && to != null)
		{
			ClickyInkNumber? clickyInkNumber = Parse(from);
			if (clickyInkNumber.HasValue)
			{
				ClickyInkNumber valueOrDefault = clickyInkNumber.GetValueOrDefault();
				clickyInkNumber = Parse(to);
				if (clickyInkNumber.HasValue)
				{
					ClickyInkNumber valueOrDefault2 = clickyInkNumber.GetValueOrDefault();
					if (valueOrDefault.IsSameKind(valueOrDefault2))
					{
						double num = valueOrDefault2.Value - valueOrDefault.Value;
						ClickyInkNumber clickyInkNumber2 = valueOrDefault2 with
						{
							ShowsPlus = true,
							Decimals = Math.Max(valueOrDefault2.Decimals, valueOrDefault.Decimals)
						};
						return (clickyInkNumber2.Formatted(num), num);
					}
				}
			}
		}
		return null;
	}
}
