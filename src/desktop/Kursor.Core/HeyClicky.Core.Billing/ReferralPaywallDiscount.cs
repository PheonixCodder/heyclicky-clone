using System;
using System.Globalization;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Billing;

public sealed record ReferralPaywallDiscount(double Percent, int Months, bool AppliesToYearlyPlans, string? ReferrerFirstName, string? ReferrerAvatarUrl)
{
	public string PercentText
	{
		get
		{
			if (Percent != Math.Round(Percent))
			{
				return Percent.ToString("0.0", CultureInfo.InvariantCulture) + "%";
			}
			return $"{(int)Percent}%";
		}
	}

	public string PeriodText
	{
		get
		{
			if (Months != 1)
			{
				return $"your first {Months} months";
			}
			return "your first month";
		}
	}

	public static ReferralPaywallDiscount? FromSnapshot(JsonObject? snapshot)
	{
		if (!(snapshot?["referred_by"] is JsonObject jsonObject))
		{
			return null;
		}
		if (!Bool(jsonObject["is_discount_available"]))
		{
			return null;
		}
		double valueOrDefault = Number(jsonObject["discount_percent"]).GetValueOrDefault();
		if (valueOrDefault <= 0.0)
		{
			return null;
		}
		int months = Math.Max(1, (int)(Number(jsonObject["discount_months"]) ?? 1.0));
		bool appliesToYearlyPlans = snapshot["terms"] is JsonObject jsonObject2 && Bool(jsonObject2["customer_discount_applies_to_yearly_plans"]);
		string text = (jsonObject["referrer_first_name"] as JsonValue)?.ToString().Trim();
		string text2 = (jsonObject["referrer_avatar_url"] as JsonValue)?.ToString();
		Uri result;
		return new ReferralPaywallDiscount(valueOrDefault, months, appliesToYearlyPlans, string.IsNullOrEmpty(text) ? null : text, Uri.TryCreate(text2, UriKind.Absolute, out result) ? text2 : null);
	}

	public string? DiscountedAmountText(string amountText)
	{
		int num = -1;
		int num2 = -1;
		for (int i = 0; i < amountText.Length; i++)
		{
			if (char.IsDigit(amountText[i]))
			{
				if (num < 0)
				{
					num = i;
				}
				num2 = i;
			}
		}
		if (num < 0)
		{
			return null;
		}
		int num3 = num;
		string text = amountText.Substring(num3, num2 + 1 - num3);
		if (!double.TryParse(text.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		double num4 = Math.Round(result * (100.0 - Percent) / 100.0 * 100.0) / 100.0;
		bool flag = num4 == Math.Round(num4);
		string text2 = (text.Contains(',') ? "#,0" : "0") + (flag ? "" : ".00");
		string text3 = amountText.Substring(0, num);
		string text4 = num4.ToString(text2, CultureInfo.InvariantCulture);
		num3 = num2 + 1;
		return text3 + text4 + amountText.Substring(num3, amountText.Length - num3);
	}

	private static bool Bool(JsonNode? n)
	{
		bool value = default(bool);
		return n is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value) && value;
	}

	private static double? Number(JsonNode? n)
	{
		if (!(n is JsonValue jsonValue))
		{
			return null;
		}
		if (jsonValue.TryGetValue<double>(out var value))
		{
			return value;
		}
		if (jsonValue.TryGetValue<int>(out var value2))
		{
			return value2;
		}
		if (!double.TryParse(jsonValue.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
		{
			return null;
		}
		return value;
	}
}
