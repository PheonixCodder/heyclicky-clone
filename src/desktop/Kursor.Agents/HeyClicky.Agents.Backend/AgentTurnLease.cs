using System;
using System.Globalization;
using System.Text.Json.Nodes;

namespace HeyClicky.Agents.Backend;

public sealed record AgentTurnLease(string LeaseId, string TurnId, DateTimeOffset? ExpiresAt, int CreditsUsed, int IncludedCredits, double? CostUsd, double? CostLimitUsd, bool RequiresExtraEffort, string? Status)
{
	internal static AgentTurnLease? Parse(JsonObject? o)
	{
		if (o != null)
		{
			string text = o["lease_id"]?.ToString();
			if (text != null && text.Length > 0)
			{
				DateTimeOffset result;
				bool value = default(bool);
				return new AgentTurnLease(text, o["turn_id"]?.ToString() ?? "", DateTimeOffset.TryParse(o["expires_at"]?.ToString(), out result) ? new DateTimeOffset?(result) : ((DateTimeOffset?)null), (int)Num(o["credits_used"]).GetValueOrDefault(), (int)(Num(o["included_credits"]) ?? 1.0), Num(o["cost_usd"]), Num(o["cost_limit_usd"]), o["requires_extra_effort"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value) && value, o["status"]?.ToString());
			}
		}
		return null;
		static double? Num(JsonNode? n)
		{
			if (n != null)
			{
				if (!double.TryParse(n.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
				{
					return null;
				}
				return result2;
			}
			return null;
		}
	}
}
