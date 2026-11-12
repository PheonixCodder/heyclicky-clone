using System.Collections.Generic;

namespace HeyClicky.Core.Billing;

public sealed record ReferralEarningsEstimate(int PerReferralUsdCents, int MonthlyUsdCents, int FirstYearUsdCents, IReadOnlyList<int> FirstYearMonthlyUsdCents);
