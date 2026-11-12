namespace HeyClicky.Core.Billing;

public sealed record BillingPlanAction(BillingPlanActionKind Kind, string Title, BillingActionStyle Style);
