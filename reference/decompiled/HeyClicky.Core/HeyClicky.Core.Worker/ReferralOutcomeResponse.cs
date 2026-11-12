namespace HeyClicky.Core.Worker;

public sealed record ReferralOutcomeResponse(string Outcome, string? Message, double? DiscountPercent, string? Code, string? ShareUrl, string? ShareDisplayText);
