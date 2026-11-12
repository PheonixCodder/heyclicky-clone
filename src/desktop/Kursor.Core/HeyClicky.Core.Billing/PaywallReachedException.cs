using System;
using System.Text.Json;

namespace HeyClicky.Core.Billing;

public sealed class PaywallReachedException(PaywallPayload payload, JsonElement raw) : Exception("Paywall (" + payload.Surface + ")")
{
	public PaywallPayload Payload { get; } = payload;

	public JsonElement Raw { get; } = raw;
}
