using System;
using System.Text.Json;

namespace HeyClicky.Core.Billing;

public static class BillingPaywallDetector
{
	public static event Action<PaywallPayload, JsonElement>? PaywallDetected;

	public static bool HandleIfPaywall(int httpStatusCode, ReadOnlySpan<byte> responseBody, bool presentsPaywall = true)
	{
		PaywallPayload payload;
		JsonElement raw;
		return HandleIfPaywall(httpStatusCode, responseBody, presentsPaywall, out payload, out raw);
	}

	public static bool HandleIfPaywall(int httpStatusCode, ReadOnlySpan<byte> responseBody, bool presentsPaywall, out PaywallPayload? payload, out JsonElement raw)
	{
		payload = null;
		raw = default(JsonElement);
		if (httpStatusCode != 402 || !PaywallPayload.TryDecode(responseBody, out payload, out raw))
		{
			return false;
		}
		if (presentsPaywall)
		{
			Log.Info("Billing", $"paywall: surface={payload.Surface} reason={payload.Reason} usage={payload.CurrentUsage}/{payload.Limit}");
			try
			{
				BillingPaywallDetector.PaywallDetected?.Invoke(payload, raw);
			}
			catch (Exception ex)
			{
				Log.Error("Billing", "paywall handler threw", ex);
			}
		}
		return true;
	}

	public static void Present(PaywallPayload payload)
	{
		Log.Info("Billing", $"paywall (local): surface={payload.Surface} usage={payload.CurrentUsage}/{payload.Limit}");
		try
		{
			BillingPaywallDetector.PaywallDetected?.Invoke(payload, default(JsonElement));
		}
		catch (Exception ex)
		{
			Log.Error("Billing", "paywall handler threw", ex);
		}
	}
}
