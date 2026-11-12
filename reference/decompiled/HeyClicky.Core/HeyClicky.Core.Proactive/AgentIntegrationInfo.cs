using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Proactive;

public sealed record AgentIntegrationInfo(string Id, string DisplayName, IReadOnlyList<string> UseCases, string Status, int ConnectedCount, IReadOnlyList<string> ToolkitSlugs, string? LogoUrl)
{
	public string PrimaryToolkitSlug
	{
		get
		{
			if (ToolkitSlugs.Count <= 0)
			{
				return Id;
			}
			return ToolkitSlugs[0];
		}
	}

	public bool IsActiveConnection
	{
		get
		{
			string status = Status;
			if (status == "connected" || status == "partial")
			{
				return true;
			}
			return false;
		}
	}

	public bool IsEligibleForConnectionSuggestion
	{
		get
		{
			bool flag = ConnectedCount == 0;
			if (flag)
			{
				string status = Status;
				bool flag2 = ((status == "not_connected" || status == "needs_reconnect") ? true : false);
				flag = flag2;
			}
			return flag;
		}
	}

	public bool IsLocal
	{
		get
		{
			if (!Id.StartsWith("local", StringComparison.OrdinalIgnoreCase))
			{
				return Id.Equals("obsidian", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
	}

	public static IReadOnlyList<AgentIntegrationInfo> ParseList(byte[] bytes)
	{
		List<AgentIntegrationInfo> list = new List<AgentIntegrationInfo>();
		if (!(JsonNode.Parse(bytes, null)?["integrations"] is JsonArray source))
		{
			return list;
		}
		foreach (JsonObject item in source.OfType<JsonObject>())
		{
			string text = Str(item, "id");
			if (text != null)
			{
				List<JsonObject> source2 = ((item["toolkits"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>().ToList();
				string value;
				List<string> useCases = (from u in (item["use_cases"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)
					select (!(u is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out value)) ? null : value.Trim() into s
					where !string.IsNullOrEmpty(s)
					select s).Cast<string>().ToList();
				list.Add(new AgentIntegrationInfo(text, Str(item, "display_name") ?? text, useCases, Str(item, "status") ?? "not_connected", (item["connected_count"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<int>(out var value2)) ? value2 : 0, (from t in source2
					select Str(t, "slug") into s
					where s != null
					select s).Cast<string>().ToList(), source2.Select((JsonObject t) => Str(t, "logo")).FirstOrDefault((string l) => !string.IsNullOrWhiteSpace(l))));
			}
		}
		return list;
		static string? Str(JsonObject o, string k)
		{
			if (!(o[k] is JsonValue jsonValue3) || !jsonValue3.TryGetValue<string>(out string value3))
			{
				return null;
			}
			return value3;
		}
	}
}
