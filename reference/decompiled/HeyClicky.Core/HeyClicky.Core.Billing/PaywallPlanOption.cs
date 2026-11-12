using System.Text.Json.Serialization;

namespace HeyClicky.Core.Billing;

public sealed record PaywallPlanOption(string Key, string Name, string PriceDisplay, int AgentsCap, string Tagline, string CtaLabel, bool Recommended, string? YearlyPriceDisplay, string? YearlyBilledSummary)
{
	[JsonIgnore]
	public bool SupportsYearly => YearlyPriceDisplay != null;
}
