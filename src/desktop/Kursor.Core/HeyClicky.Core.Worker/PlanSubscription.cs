namespace HeyClicky.Core.Worker;

public sealed record PlanSubscription(string Status, bool CancelAtPeriodEnd, string? CurrentPeriodStart = null, string? CurrentPeriodEnd = null, string? ProductId = null, string? CancelAt = null, string? BillingInterval = null);
