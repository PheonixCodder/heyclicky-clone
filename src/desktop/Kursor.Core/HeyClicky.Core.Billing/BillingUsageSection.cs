namespace HeyClicky.Core.Billing;

public sealed record BillingUsageSection(string Title, string CountText, string? CapText, double Progress, bool IsSpent);
