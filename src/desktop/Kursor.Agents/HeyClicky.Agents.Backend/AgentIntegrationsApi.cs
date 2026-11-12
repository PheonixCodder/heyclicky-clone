using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Agents.Backend;

internal static class AgentIntegrationsApi
{
	public static AgentIntegrationCatalog ParseCatalog(JsonObject? o)
	{
		if (o == null)
		{
			return new AgentIntegrationCatalog(ComposioConfigured: false, Array.Empty<AgentIntegration>());
		}
		bool value = default(bool);
		bool composioConfigured = !(o["composio_configured"] is JsonValue jsonValue) || !jsonValue.TryGetValue<bool>(out value) || value;
		List<AgentIntegration> integrations = ((o["integrations"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>().Select(AgentIntegration.Parse).OfType<AgentIntegration>()
			.ToList();
		return new AgentIntegrationCatalog(composioConfigured, integrations);
	}

	public static string ErrorText(string body, string fallback)
	{
		try
		{
			if (JsonNode.Parse(body, null) is JsonObject jsonObject)
			{
				string text = jsonObject["error"]?.ToString();
				if (text != null && text.Length > 0)
				{
					return text;
				}
			}
		}
		catch (JsonException)
		{
		}
		return fallback;
	}
}
