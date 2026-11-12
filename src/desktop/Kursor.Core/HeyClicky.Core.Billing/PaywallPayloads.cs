using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Billing;

public static class PaywallPayloads
{
	public const string QuotaExceeded = "quota_exceeded";

	public const string PaymentProblem = "payment_problem";

	private static PaywallPlanOption Pro(int cap, bool recommended, bool yearly)
	{
		return new PaywallPlanOption("pro", "HeyClicky Pro", "$20/mo", cap, $"{cap} agent messages per month", "Get HeyClicky Pro", recommended, yearly ? "$16/mo" : null, yearly ? "billed annually ($192)" : null);
	}

	private static PaywallPlanOption Max(int cap, bool recommended, bool yearly)
	{
		return new PaywallPlanOption("max", "HeyClicky Max", "$100/mo", cap, $"{cap} agent messages per month", "Get HeyClicky Max", recommended, yearly ? "$80/mo" : null, yearly ? "billed annually ($960)" : null);
	}

	public static PaywallPayload? ManualUpgrade(string currentTier, int? proAgentsCap, int? maxAgentsCap, bool proYearlyAvailable, bool maxYearlyAvailable)
	{
		int num = proAgentsCap ?? 150;
		int num2 = maxAgentsCap ?? 1000;
		PaywallPlanOption paywallPlanOption = Pro(num, currentTier == "free", proYearlyAvailable);
		PaywallPlanOption paywallPlanOption2 = Max(num2, currentTier == "pro", maxYearlyAvailable);
		IReadOnlyList<PaywallPlanOption> readOnlyList;
		if (!(currentTier == "free"))
		{
			if (!(currentTier == "pro"))
			{
				return null;
			}
			readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<PaywallPlanOption>(paywallPlanOption2);
		}
		else
		{
			readOnlyList = new global::_003C_003Ez__ReadOnlyArray<PaywallPlanOption>(new PaywallPlanOption[2] { paywallPlanOption, paywallPlanOption2 });
		}
		bool flag = readOnlyList.Any((PaywallPlanOption p) => p.SupportsYearly);
		int value = ((currentTier == "free") ? num : num2);
		string title = ((currentTier == "free") ? "Upgrade HeyClicky to keep going." : "Upgrade to Max");
		return new PaywallPayload("quota_exceeded", "agents", title, $"{value} HeyClicky Agent messages per month", "", "", 0, 0, null, readOnlyList, flag ? "Save 20%" : null, null);
	}

	public static PaywallPayload LocalMessagesLimit(int currentUsage, int limit, string? resetAt, int? proAgentsCap, int? maxAgentsCap, bool proYearlyAvailable, bool maxYearlyAvailable)
	{
		int num = proAgentsCap ?? 150;
		int cap = maxAgentsCap ?? 1000;
		IReadOnlyList<PaywallPlanOption> readOnlyList = new global::_003C_003Ez__ReadOnlyArray<PaywallPlanOption>(new PaywallPlanOption[2]
		{
			Pro(num, recommended: true, proYearlyAvailable),
			Max(cap, recommended: false, maxYearlyAvailable)
		});
		return new PaywallPayload("quota_exceeded", "messages", "You're out of messages.", $"{num} HeyClicky Agent messages per month", "Get HeyClicky Pro for $20/mo", "$20/mo", currentUsage, limit, resetAt, readOnlyList, readOnlyList.Any((PaywallPlanOption p) => p.SupportsYearly) ? "Save 20%" : null, null);
	}
}
