using System.Collections.Generic;

namespace HeyClicky.Core.Billing;

public sealed record ReferralEarningsMixEstimate(IReadOnlyList<ReferralEarningsEstimate> PlanEstimates, int MonthlyUsdCents, int FirstYearUsdCents, IReadOnlyList<int> FirstYearMonthlyUsdCents);
