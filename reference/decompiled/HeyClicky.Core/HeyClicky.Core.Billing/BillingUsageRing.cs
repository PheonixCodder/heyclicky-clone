using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Billing;

public sealed record BillingUsageRing(double Progress, bool IsSpent, BillingUsageSection Agents, BillingUsageSection Voice, string? ResetsLine, string AccessibilityValue)
{
	public const double MinimumDrawnProgress = 0.03;

	public static BillingUsageRing? From(PlanSnapshot? plan, string? resetsPhrase)
	{
		if ((object)plan == null || plan.Usage.Agents.Cap <= 0)
		{
			return null;
		}
		PlanUsageBucket agents = plan.Usage.Agents;
		PlanUsageBucket messages = plan.Usage.Messages;
		bool flag = plan.Plan == "unlimited";
		BillingUsageSection billingUsageSection = Section("AGENT MESSAGES", agents, isUnlimited: false);
		BillingUsageSection voice = Section("VOICE MESSAGES", messages, flag);
		string accessibilityValue = $"{Displayed(agents)} of {agents.Cap} agent messages. " + (flag ? "Unlimited voice messages." : $"{Displayed(messages)} of {messages.Cap} voice messages.");
		double progress = billingUsageSection.Progress;
		bool isSpent = billingUsageSection.IsSpent;
		object resetsLine;
		if (resetsPhrase != null && resetsPhrase.Length > 0)
		{
			resetsLine = "Resets " + resetsPhrase;
		}
		else
		{
			resetsLine = null;
		}
		return new BillingUsageRing(progress, isSpent, billingUsageSection, voice, (string?)resetsLine, accessibilityValue);
	}

	private static int Displayed(PlanUsageBucket b)
	{
		if (b.Cap <= 0)
		{
			return Math.Max(b.Used, 0);
		}
		return Math.Min(Math.Max(b.Used, 0), b.Cap);
	}

	private static BillingUsageSection Section(string title, PlanUsageBucket b, bool isUnlimited)
	{
		int num = Displayed(b);
		double progress = (isUnlimited ? 1.0 : ((b.Cap > 0) ? ((double)num / (double)b.Cap) : 0.0));
		bool isSpent = !isUnlimited && b.Cap > 0 && b.Used >= b.Cap;
		return new BillingUsageSection(title, isUnlimited ? "Unlimited" : num.ToString(CultureInfo.InvariantCulture), isUnlimited ? null : $"/ {b.Cap} this month", progress, isSpent);
	}

	[CompilerGenerated]
	private BillingUsageRing(BillingUsageRing original)
	{
		Progress = original.Progress;
		IsSpent = original.IsSpent;
		Agents = original.Agents;
		Voice = original.Voice;
		ResetsLine = original.ResetsLine;
		AccessibilityValue = original.AccessibilityValue;
	}
}
