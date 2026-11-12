using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace HeyClicky.Agents.Backend;

public sealed record AgentIntegration(string Id, string DisplayName, string Description, string Status, int ConnectedCount, int TotalCount, IReadOnlyList<AgentIntegrationToolkit> Toolkits, IReadOnlyList<AgentIntegrationAccount> Accounts)
{
	public string PrimaryToolkitSlug
	{
		get
		{
			if (Toolkits.Count <= 0)
			{
				return Id;
			}
			return Toolkits[0].Slug;
		}
	}

	public string? LogoUrl => Toolkits.Select((AgentIntegrationToolkit t) => t.Logo).FirstOrDefault((string l) => !string.IsNullOrEmpty(l));

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

	public IReadOnlyList<AgentIntegrationAccount> ActiveAccounts => Accounts.Where((AgentIntegrationAccount a) => a.IsActive).ToList();

	public bool HasReachedActiveAccountLimit => ActiveAccounts.Count >= 5;

	public bool IsLocalIntegration => Id == "obsidian";

	public IReadOnlyList<string> UseCases { get; init; } = Array.Empty<string>();

	public const int MaximumActiveAccountsPerApp = 5;

	public static IReadOnlyList<string> NormalizedUseCases(IEnumerable<string> values)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		foreach (string value in values)
		{
			string text = Regex.Replace(value, "\\s+", " ").Trim().Trim('.', '。');
			if (text.Length > 0 && hashSet.Add(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	internal static AgentIntegration? Parse(JsonObject o)
	{
		string text = o["id"]?.ToString();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		List<AgentIntegrationToolkit> toolkits = (from t in ((o["toolkits"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
			select new AgentIntegrationToolkit(t["slug"]?.ToString() ?? "", t["name"]?.ToString() ?? "", t["logo"]?.ToString(), Bool(t["is_connected"])) into t
			where t.Slug.Length > 0
			select t).ToList();
		List<AgentIntegrationAccount> accounts = (from a in ((o["accounts"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
			select new AgentIntegrationAccount(a["connected_account_id"]?.ToString() ?? "", a["account_name"]?.ToString(), a["label"]?.ToString(), a["avatar_url"]?.ToString(), a["account_name_kind"]?.ToString(), a["status"]?.ToString() ?? "connected", a["connected_at"]?.ToString()) into a
			where a.ConnectedAccountId.Length > 0
			select a).ToList();
		string value;
		return new AgentIntegration(text, o["display_name"]?.ToString() ?? text, o["description"]?.ToString() ?? "", o["status"]?.ToString() ?? "not_connected", Int(o["connected_count"]), Int(o["total_count"]), toolkits, accounts)
		{
			UseCases = NormalizedUseCases(((o["use_cases"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).Select((JsonNode u) => (!(u is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out value)) ? null : value).OfType<string>())
		};
		static bool Bool(JsonNode? n)
		{
			bool value2 = default(bool);
			return n is JsonValue jsonValue2 && jsonValue2.TryGetValue<bool>(out value2) && value2;
		}
		static int Int(JsonNode? n)
		{
			if (!(n is JsonValue jsonValue3) || !jsonValue3.TryGetValue<int>(out var value3))
			{
				if (!int.TryParse(n?.ToString(), out var result))
				{
					return 0;
				}
				return result;
			}
			return value3;
		}
	}

	[CompilerGenerated]
	private AgentIntegration(AgentIntegration original)
	{
		Id = original.Id;
		DisplayName = original.DisplayName;
		Description = original.Description;
		Status = original.Status;
		ConnectedCount = original.ConnectedCount;
		TotalCount = original.TotalCount;
		Toolkits = original.Toolkits;
		Accounts = original.Accounts;
		UseCases = original.UseCases;
	}
}
