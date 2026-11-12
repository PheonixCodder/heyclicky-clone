using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeyClicky.Core.Billing;

public sealed record PaywallPayload(string? Reason, string Surface, string Title, string Body, string CtaLabel, string PriceDisplay, int CurrentUsage, int Limit, string? ResetAt, IReadOnlyList<PaywallPlanOption>? Plans, string? YearlySavingsLabel, string? DefaultInterval)
{
	[JsonIgnore]
	public bool IsPaymentProblem => Reason == "payment_problem";

	[JsonIgnore]
	public IReadOnlyList<PaywallPlanOption> UpgradeOptions => Plans ?? Array.Empty<PaywallPlanOption>();

	[JsonIgnore]
	public bool SupportsYearly => UpgradeOptions.Any((PaywallPlanOption p) => p.SupportsYearly);

	public const string ErrorSentinelValue = "paywall";

	public static PaywallPayload? DecodeFromBody(ReadOnlySpan<byte> body)
	{
		if (!TryDecode(body, out PaywallPayload payload, out JsonElement _))
		{
			return null;
		}
		return payload;
	}

	public static bool TryDecode(ReadOnlySpan<byte> body, out PaywallPayload? payload, out JsonElement raw)
	{
		payload = null;
		raw = default(JsonElement);
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(body.ToArray());
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object || !rootElement.TryGetProperty("error", out var value) || value.ValueKind != JsonValueKind.String || value.GetString() != "paywall")
			{
				return false;
			}
			payload = rootElement.Deserialize(CoreJsonContext.Default.PaywallPayload);
			if ((object)payload == null)
			{
				return false;
			}
			raw = rootElement.Clone();
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	[CompilerGenerated]
	private PaywallPayload(PaywallPayload original)
	{
		Reason = original.Reason;
		Surface = original.Surface;
		Title = original.Title;
		Body = original.Body;
		CtaLabel = original.CtaLabel;
		PriceDisplay = original.PriceDisplay;
		CurrentUsage = original.CurrentUsage;
		Limit = original.Limit;
		ResetAt = original.ResetAt;
		Plans = original.Plans;
		YearlySavingsLabel = original.YearlySavingsLabel;
		DefaultInterval = original.DefaultInterval;
	}
}
