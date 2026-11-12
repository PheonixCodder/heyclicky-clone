using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Billing;

internal sealed record PlanBillingFingerprint(string Plan, string Tier, string? Status, string? ProductId, bool? CancelsAtPeriodEnd, string? BillingInterval)
{
	public static PlanBillingFingerprint? From(PlanSnapshot? s)
	{
		if ((object)s != null && s.IsFailOpenFallback != true)
		{
			return new PlanBillingFingerprint(s.Plan, s.EffectiveTier, s.Subscription?.Status, s.Subscription?.ProductId, s.Subscription?.CancelAtPeriodEnd, s.Subscription?.BillingInterval);
		}
		return null;
	}
}
