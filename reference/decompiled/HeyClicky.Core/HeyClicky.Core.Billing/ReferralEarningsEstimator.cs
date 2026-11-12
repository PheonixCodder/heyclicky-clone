using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Billing;

public static class ReferralEarningsEstimator
{
	public static ReferralEarningsEstimate Estimate(int referralsPerMonth, int monthsRetained, int monthlyPriceUsdCents, double commissionPercent, int commissionMonths)
	{
		int num = Math.Max(0, Math.Min(monthsRetained, commissionMonths));
		int num2 = CommissionUsdCents(monthlyPriceUsdCents, commissionPercent);
		int num3 = num * num2;
		int[] array = new int[12];
		for (int i = 1; i <= 12; i++)
		{
			int num4 = 0;
			for (int j = 1; j <= i; j++)
			{
				if (i - j + 1 <= num)
				{
					num4 += referralsPerMonth * num2;
				}
			}
			array[i - 1] = num4;
		}
		return new ReferralEarningsEstimate(num3, referralsPerMonth * num3, array.Sum(), array);
	}

	public static ReferralEarningsMixEstimate Estimate(IReadOnlyList<ReferralEarningsPlanMix> mixes, int monthsRetained, double commissionPercent, int commissionMonths)
	{
		List<ReferralEarningsEstimate> plans = mixes.Select((ReferralEarningsPlanMix m) => Estimate(m.ReferralsPerMonth, monthsRetained, m.MonthlyPriceUsdCents, commissionPercent, commissionMonths)).ToList();
		List<int> firstYearMonthlyUsdCents = (from i in Enumerable.Range(0, 12)
			select plans.Sum((ReferralEarningsEstimate p) => p.FirstYearMonthlyUsdCents[i])).ToList();
		return new ReferralEarningsMixEstimate(plans, plans.Sum((ReferralEarningsEstimate p) => p.MonthlyUsdCents), plans.Sum((ReferralEarningsEstimate p) => p.FirstYearUsdCents), firstYearMonthlyUsdCents);
	}

	private static int CommissionUsdCents(int monthlyPriceUsdCents, double commissionPercent)
	{
		return (int)Math.Floor((double)monthlyPriceUsdCents * commissionPercent / 100.0);
	}
}
