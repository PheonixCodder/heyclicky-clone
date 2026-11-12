using System.Text.Json.Nodes;

namespace HeyClicky.App.Demo;

public static class ReferralFixture
{
	internal static JsonObject ReferralBody(bool earned)
	{
		JsonObject obj = new JsonObject((JsonNodeOptions?)null)
		{
			["enabled"] = true,
			["code"] = "nilesh",
			["share_url"] = "https://heyclicky.com/@nilesh",
			["share_display_text"] = "heyclicky.com/@nilesh",
			["share_display_prefix"] = "heyclicky.com/@",
			["is_code_disabled"] = false,
			["terms"] = new JsonObject((JsonNodeOptions?)null)
			{
				["commission_percent"] = 25,
				["commission_months"] = 12,
				["customer_discount_percent"] = 50,
				["customer_discount_months"] = 1,
				["commission_hold_days"] = 30,
				["minimum_payout_usd_cents"] = 2500,
				["payout_contact_email"] = "hi@heyclicky.com"
			},
			["stats"] = (earned ? new JsonObject((JsonNodeOptions?)null)
			{
				["referred_count"] = 2,
				["paying_count"] = 1,
				["earned_usd_cents"] = 6500,
				["available_usd_cents"] = 4000,
				["on_hold_usd_cents"] = 2500,
				["paid_out_usd_cents"] = 0,
				["earned_last_30_days_usd_cents"] = 2500
			} : new JsonObject((JsonNodeOptions?)null)
			{
				["referred_count"] = 0,
				["paying_count"] = 0,
				["earned_usd_cents"] = 0,
				["available_usd_cents"] = 0,
				["on_hold_usd_cents"] = 0,
				["paid_out_usd_cents"] = 0
			})
		};
		JsonObject obj2 = new JsonObject((JsonNodeOptions?)null)
		{
			["max_referrals_per_month"] = 50,
			["default_months_retained"] = 6
		};
		global::_003C_003Ey__InlineArray2<JsonNode> buffer = default(global::_003C_003Ey__InlineArray2<JsonNode>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer, 0) = new JsonObject((JsonNodeOptions?)null)
		{
			["key"] = "pro",
			["name"] = "Pro",
			["monthly_price_usd_cents"] = 2000,
			["default_referrals_per_month"] = 3
		};
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer, 1) = new JsonObject((JsonNodeOptions?)null)
		{
			["key"] = "max",
			["name"] = "Max",
			["monthly_price_usd_cents"] = 10000,
			["default_referrals_per_month"] = 1
		};
		obj2["plans"] = new JsonArray(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(in buffer, 2));
		obj["estimator"] = obj2;
		JsonArray value;
		if (!earned)
		{
			value = new JsonArray((JsonNodeOptions?)null);
		}
		else
		{
			global::_003C_003Ey__InlineArray2<JsonNode> buffer2 = default(global::_003C_003Ey__InlineArray2<JsonNode>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer2, 0) = new JsonObject((JsonNodeOptions?)null)
			{
				["display_name"] = "Sam",
				["plan"] = "max",
				["subscribed_months"] = 2,
				["earned_usd_cents"] = 5000,
				["joined_at"] = "2026-08-02T10:00:00Z"
			};
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer2, 1) = new JsonObject((JsonNodeOptions?)null)
			{
				["display_name"] = "Ada",
				["plan"] = "pro",
				["subscribed_months"] = 3,
				["earned_usd_cents"] = 1500,
				["joined_at"] = "2026-07-12T10:00:00Z"
			};
			value = new JsonArray(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(in buffer2, 2));
		}
		obj["invited_friends"] = value;
		obj["monthly_earnings"] = new JsonArray((JsonNodeOptions?)null);
		return obj;
	}
}
