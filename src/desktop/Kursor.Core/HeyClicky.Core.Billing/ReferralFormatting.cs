using System;
using System.Globalization;

namespace HeyClicky.Core.Billing;

public static class ReferralFormatting
{
	private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

	public static string Dollars(int usdCents)
	{
		return ((double)usdCents / 100.0).ToString("C2", En);
	}

	public static string WholeDollars(int usdCents)
	{
		return Math.Round((double)usdCents / 100.0, MidpointRounding.AwayFromZero).ToString("C0", En);
	}

	public static string CompactDollars(int usdCents)
	{
		if (usdCents % 100 != 0)
		{
			return Dollars(usdCents);
		}
		return WholeDollars(usdCents);
	}

	public static string Percent(double value)
	{
		if (value != Math.Round(value))
		{
			return value.ToString("0.0", En) + "%";
		}
		return $"{(int)value}%";
	}

	public static string Months(int count)
	{
		if (count != 1)
		{
			return $"{count} months";
		}
		return "1 month";
	}

	public static string Referrals(int count, string planName)
	{
		if (count != 1)
		{
			return $"{count} {planName} referrals";
		}
		return "1 " + planName + " referral";
	}

	public static string People(int count)
	{
		if (count != 1)
		{
			return $"{count} people";
		}
		return "1 person";
	}

	public static string ShortMonthLabel(string month)
	{
		if (month.Length >= 2)
		{
			int length = month.Length;
			int num = length - 2;
			if (int.TryParse(month.Substring(num, length - num), out var result) && result >= 1 && result <= 12)
			{
				return En.DateTimeFormat.AbbreviatedMonthNames[result - 1];
			}
		}
		return month;
	}

	public static string FirstYearExplanation(ReferralEarningsMixEstimate mix, int monthsRetained, int commissionMonths)
	{
		string text = WholeDollars(mix.FirstYearUsdCents);
		if (Math.Min(monthsRetained, commissionMonths) <= 1)
		{
			return "That's about " + text + " in your first year.";
		}
		return "That's about " + text + " in your first year, and the people who join late keep paying into the next.";
	}

	public static string? NegativeBalanceExplanation(int availableUsdCents, int reversedPaymentCount, int reversedUsdCents)
	{
		if (availableUsdCents >= 0)
		{
			return null;
		}
		string text = Dollars(-availableUsdCents);
		string text2 = "You don't owe anything. Your next earnings just cover the " + text + " first, then payouts carry on like normal (・ω・)";
		if (reversedPaymentCount <= 0)
		{
			return "You got paid a bit more than has cleared so far. " + text2;
		}
		string value = ((reversedPaymentCount == 1) ? "One of your friends' payments got" : $"{reversedPaymentCount} of your friends' payments got");
		return $"{value} refunded or pulled back by their bank, so {Dollars(reversedUsdCents)} of what you earned came back out, and some of it had already been paid to you. {text2}";
	}
}
