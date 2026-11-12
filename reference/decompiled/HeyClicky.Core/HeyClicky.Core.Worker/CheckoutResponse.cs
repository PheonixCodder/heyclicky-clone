namespace HeyClicky.Core.Worker;

public sealed record CheckoutResponse(string Url, string? DiscountCodeApplied);
