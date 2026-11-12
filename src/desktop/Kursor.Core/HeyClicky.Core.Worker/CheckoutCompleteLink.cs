namespace HeyClicky.Core.Worker;

public sealed record CheckoutCompleteLink(string? CheckoutSessionId, bool IsCanceled, bool IsPortal);
