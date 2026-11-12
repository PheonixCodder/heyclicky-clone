using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Billing;

public sealed record BillingPlanCard(string Title, string? Description, string? TeamLabel, string? ErrorText, string? CancellationLabel, IReadOnlyList<BillingUsageMeter> Meters, IReadOnlyList<BillingPlanAction> Actions)
{
	private const bool ShowsRealUsage = false;

	public const string TeamDashboardUrl = "https://team.heyclicky.com";

	public static BillingPlanCard From(BillingStateService billing)
	{
		PlanSnapshot plan = billing.Plan;
		bool isFetching = billing.IsFetching;
		bool requiresPaymentResolution = billing.RequiresPaymentResolution;
		string text = ((billing.CurrentTier == "max") ? "HeyClicky Max" : "HeyClicky Pro");
		string title = (((object)plan != null) ? (requiresPaymentResolution ? ("There's a problem with your " + text + " payment.") : (billing.IsUnlimited ? ("You're on " + text + ".") : "You're on the Free plan.")) : (isFetching ? "Checking your plan…" : "Couldn't load your plan."));
		string description = null;
		if ((object)plan != null)
		{
			if (requiresPaymentResolution)
			{
				description = "Update your payment method to keep using HeyClicky. Processing is paused until billing is fixed.";
			}
			else
			{
				string text2 = ((plan.Plan == "unlimited") ? $"Chat with HeyClicky as much as you want and send {plan.Usage.Agents.Cap} agent messages a month." : $"Talk to HeyClicky {plan.Usage.Messages.Cap} times a month and send {plan.Usage.Agents.Cap} agent messages.");
				string usageResetsLabel = billing.UsageResetsLabel;
				description = ((usageResetsLabel != null) ? (text2 + " " + usageResetsLabel) : text2);
			}
		}
		object obj;
		if (!billing.IsTeamManagedPlan)
		{
			obj = null;
		}
		else
		{
			string teamPlanTeamName = billing.TeamPlanTeamName;
			obj = ((teamPlanTeamName != null && teamPlanTeamName.Length > 0) ? ("Included with your " + teamPlanTeamName + " team plan.") : "Included with your team plan.");
		}
		string teamLabel = (string)obj;
		string errorText = null;
		BillingFetchError lastFetchError = billing.LastFetchError;
		if ((object)lastFetchError != null)
		{
			if (lastFetchError.Kind == BillingFetchErrorKind.SignedOut)
			{
				errorText = "it seems like you got logged out. can you log in again for me and try again?";
			}
			else if ((object)plan == null && !isFetching)
			{
				errorText = "HeyClicky couldn't reach the server. Your plan and usage will show once it's back.";
			}
		}
		List<BillingUsageMeter> list = new List<BillingUsageMeter>();
		if ((object)plan != null && !requiresPaymentResolution)
		{
			PlanUsageBucket b2 = billing.DisplayedMessagesUsage ?? plan.Usage.Messages;
			list.Add((plan.Plan == "unlimited") ? new BillingUsageMeter("Talk to HeyClicky", Unlimited(b2), 1.0) : new BillingUsageMeter("Talk to HeyClicky", Usage(b2), Progress(b2)));
			list.Add(new BillingUsageMeter("Agent messages", Usage(plan.Usage.Agents), Progress(plan.Usage.Agents)));
		}
		List<BillingPlanAction> list2 = new List<BillingPlanAction>();
		if ((object)plan == null)
		{
			if (!isFetching)
			{
				list2.Add(new BillingPlanAction(BillingPlanActionKind.TryAgain, "Try again", BillingActionStyle.SecondaryWhite));
			}
		}
		else if (requiresPaymentResolution)
		{
			list2.Add(new BillingPlanAction(BillingPlanActionKind.FixPayment, "Fix payment", BillingActionStyle.Primary));
		}
		else if (billing.IsUnlimited)
		{
			if (billing.IsTeamManagedPlan)
			{
				list2.Add(new BillingPlanAction(BillingPlanActionKind.ViewTeam, "View your team", BillingActionStyle.SecondaryWhite));
			}
			else
			{
				if (billing.IsBillingEnforcementOn && (object)billing.ManualUpgradePayload != null)
				{
					list2.Add(new BillingPlanAction(BillingPlanActionKind.Upgrade, (billing.CurrentTier == "pro") ? "Upgrade to Max" : "Upgrade HeyClicky", BillingActionStyle.Primary));
				}
				if (billing.IsBillingEnforcementOn)
				{
					string annualBillingSavingsCtaTitle = billing.AnnualBillingSavingsCtaTitle;
					if (annualBillingSavingsCtaTitle != null)
					{
						list2.Add(new BillingPlanAction(BillingPlanActionKind.SwitchToYearly, annualBillingSavingsCtaTitle, BillingActionStyle.SecondaryWhite));
					}
				}
				list2.Add(new BillingPlanAction(BillingPlanActionKind.ManageSubscription, "Manage subscription", BillingActionStyle.SecondaryOutline));
			}
		}
		else if (billing.IsBillingEnforcementOn)
		{
			list2.Add(new BillingPlanAction(BillingPlanActionKind.Upgrade, "Upgrade HeyClicky", BillingActionStyle.Primary));
		}
		return new BillingPlanCard(title, description, teamLabel, errorText, requiresPaymentResolution ? null : billing.SubscriptionCancellationLabel, list, list2);
		static int Clamped(PlanUsageBucket b)
		{
			if (b.Cap > 0)
			{
				return Math.Min(Math.Max(b.Used, 0), b.Cap);
			}
			return Math.Max(b.Used, 0);
		}
		static double Progress(PlanUsageBucket b)
		{
			if (b.Cap > 0)
			{
				return Math.Clamp((double)b.Used / (double)b.Cap, 0.0, 1.0);
			}
			return 0.0;
		}
		static string Unlimited(PlanUsageBucket b)
		{
			return "Unlimited";
		}
		static string Usage(PlanUsageBucket b)
		{
			return $"{Clamped(b)} / {b.Cap}";
		}
	}

	public static string ActionErrorText(bool signedOut)
	{
		if (!signedOut)
		{
			return "Oops, something's wrong. Email us at hi@heyclicky.com please.";
		}
		return "it seems like you got logged out. can you log in again for me and try again?";
	}

	[CompilerGenerated]
	private BillingPlanCard(BillingPlanCard original)
	{
		Title = original.Title;
		Description = original.Description;
		TeamLabel = original.TeamLabel;
		ErrorText = original.ErrorText;
		CancellationLabel = original.CancellationLabel;
		Meters = original.Meters;
		Actions = original.Actions;
	}
}
