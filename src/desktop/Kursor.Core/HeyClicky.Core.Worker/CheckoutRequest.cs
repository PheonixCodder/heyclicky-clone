namespace HeyClicky.Core.Worker;

public sealed record CheckoutRequest(string Plan, string Interval, string CheckoutUi = "web", string? DiscountCode = null);
