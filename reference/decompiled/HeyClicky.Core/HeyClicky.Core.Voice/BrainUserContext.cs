using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Voice;

public sealed record BrainUserContext
{
	public string RosterNote { get; init; } = "";

	public IReadOnlyList<string> ConnectedApps { get; init; } = Array.Empty<string>();

	public IReadOnlyList<string> NeedsReconnect { get; init; } = Array.Empty<string>();

	public IReadOnlyList<CustomConnectorDescription> CustomConnectors { get; init; } = Array.Empty<CustomConnectorDescription>();

	public IReadOnlyList<IntegrationAccounts> ConnectedAccounts { get; init; } = Array.Empty<IntegrationAccounts>();

	public static readonly BrainUserContext Empty = new BrainUserContext();

	public static BrainUserContext FromIntegrations(JsonObject body)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		List<IntegrationAccounts> list3 = new List<IntegrationAccounts>();
		foreach (JsonObject item in ((body["integrations"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>())
		{
			string text = Str(item["id"]);
			string text2 = Str(item["display_name"]) ?? text;
			string text3 = Str(item["status"]);
			if (text != null && text2 != null)
			{
				if ((text3 == "connected" || text3 == "partial") ? true : false)
				{
					list.Add(text);
				}
				else if (text3 == "needs_reconnect")
				{
					list2.Add(text2);
				}
				List<IntegrationAccount> list4 = (from a in ((item["accounts"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>().Where(delegate(JsonObject a)
					{
						string text4 = Str(a["status"]);
						return ((text4 == null || text4 == "connected") ? true : false) && Str(a["connected_account_id"]) != null;
					})
					select new IntegrationAccount(Str(a["connected_account_id"]), Str(a["account_name"]), Str(a["label"]))).ToList();
				if (list4.Count > 1)
				{
					list3.Add(new IntegrationAccounts(text2, list4));
				}
			}
		}
		return new BrainUserContext
		{
			ConnectedApps = list,
			NeedsReconnect = list2.Order<string>(StringComparer.OrdinalIgnoreCase).ToList(),
			ConnectedAccounts = list3
		};
	}

	private static string? Str(JsonNode? node)
	{
		if (!(node is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value) || string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		return value.Trim();
	}

	internal void Write(Utf8JsonWriter writer, string timeZone)
	{
		writer.WriteStartObject("user_context");
		writer.WriteStartArray("needs_reconnect");
		foreach (string item in NeedsReconnect)
		{
			writer.WriteStringValue(item);
		}
		writer.WriteEndArray();
		if (CustomConnectors.Count > 0)
		{
			writer.WriteStartArray("custom_connectors");
			foreach (CustomConnectorDescription customConnector in CustomConnectors)
			{
				writer.WriteStartObject();
				writer.WriteString("name", customConnector.Name);
				writer.WriteString("description", customConnector.Description);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}
		if (ConnectedAccounts.Count > 0)
		{
			writer.WriteStartArray("connected_accounts");
			foreach (IntegrationAccounts connectedAccount in ConnectedAccounts)
			{
				writer.WriteStartObject();
				writer.WriteString("integration", connectedAccount.Integration);
				writer.WriteStartArray("accounts");
				foreach (IntegrationAccount account in connectedAccount.Accounts)
				{
					writer.WriteStartObject();
					writer.WriteString("connected_account_id", account.ConnectedAccountId);
					string accountName = account.AccountName;
					if (accountName != null)
					{
						writer.WriteString("account_name", accountName);
					}
					string label = account.Label;
					if (label != null)
					{
						writer.WriteString("label", label);
					}
					writer.WriteEndObject();
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}
		writer.WriteString("roster_note", RosterNote);
		writer.WriteStartArray("connected_apps");
		foreach (string connectedApp in ConnectedApps)
		{
			writer.WriteStringValue(connectedApp);
		}
		writer.WriteEndArray();
		writer.WriteString("time_zone", timeZone);
		writer.WriteEndObject();
	}

	[CompilerGenerated]
	private BrainUserContext(BrainUserContext original)
	{
		RosterNote = original.RosterNote;
		ConnectedApps = original.ConnectedApps;
		NeedsReconnect = original.NeedsReconnect;
		CustomConnectors = original.CustomConnectors;
		ConnectedAccounts = original.ConnectedAccounts;
	}

	public BrainUserContext()
	{
	}
}
