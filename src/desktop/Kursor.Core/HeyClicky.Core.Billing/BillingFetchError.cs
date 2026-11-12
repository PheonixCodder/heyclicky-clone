namespace HeyClicky.Core.Billing;

public sealed record BillingFetchError(BillingFetchErrorKind Kind, int Status, string Detail);
