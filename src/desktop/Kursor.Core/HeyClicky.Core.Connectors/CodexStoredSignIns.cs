using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Connectors;

public static class CodexStoredSignIns
{
	public static JsonObject? ReadFile(string codexHome)
	{
		try
		{
			string path = Path.Combine(codexHome, ".credentials.json");
			return File.Exists(path) ? (JsonNode.Parse(File.ReadAllText(path), null) as JsonObject) : null;
		}
		catch (Exception ex)
		{
			Log.Warn("connectors", "codex credentials unreadable: " + ex.Message);
			return null;
		}
	}

	public static CodexStoredSignIn? Find(JsonObject entries, Uri serverUrl, string serverName, DateTimeOffset now)
	{
		CodexStoredSignIn codexStoredSignIn = null;
		foreach (JsonObject item in Candidates(entries, serverUrl, serverName))
		{
			string text = item["access_token"]?.ToString();
			string text2 = ((text != null && text.Length > 0) ? text : null);
			string text3 = item["refresh_token"]?.ToString();
			string text4 = ((text3 != null && text3.Length > 0) ? text3 : null);
			string text5 = item["client_id"]?.ToString();
			string text6 = ((text5 != null && text5.Length > 0) ? text5 : null);
			string text7 = item["client_secret"]?.ToString();
			string clientSecret = ((text7 != null && text7.Length > 0) ? text7 : null);
			double? num = Number(item["expires_at"]);
			DateTimeOffset? obj;
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				if (valueOrDefault > 0.0)
				{
					obj = ((valueOrDefault > 1000000000000.0) ? DateTimeOffset.FromUnixTimeMilliseconds((long)valueOrDefault) : DateTimeOffset.FromUnixTimeSeconds((long)valueOrDefault));
					goto IL_012f;
				}
			}
			obj = null;
			goto IL_012f;
			IL_012f:
			DateTimeOffset? dateTimeOffset = obj;
			if (text2 != null && (!dateTimeOffset.HasValue || dateTimeOffset > now))
			{
				return new CodexStoredSignIn(text2, text4, dateTimeOffset, text6, clientSecret);
			}
			if ((object)codexStoredSignIn == null && text4 != null && text6 != null)
			{
				codexStoredSignIn = new CodexStoredSignIn(text2 ?? "", text4, dateTimeOffset ?? now, text6, clientSecret);
			}
		}
		return codexStoredSignIn;
	}

	internal static List<JsonObject> Candidates(JsonObject entries, Uri serverUrl, string serverName)
	{
		string wanted = NormalizedUrl(serverUrl.AbsoluteUri);
		List<JsonObject> list = (from e in entries.OrderBy<KeyValuePair<string, JsonNode>, string>((KeyValuePair<string, JsonNode> e) => e.Key, StringComparer.Ordinal)
			select e.Value).OfType<JsonObject>().Where(delegate(JsonObject r)
		{
			string text = r["server_url"]?.ToString();
			return text != null && NormalizedUrl(text) == wanted;
		}).ToList();
		List<JsonObject> list2 = list.Where(delegate(JsonObject r)
		{
			string text2 = r["server_name"]?.ToString();
			return text2 != null && (text2 == serverName || text2 == "local:" + serverName);
		}).ToList();
		if (list2.Count > 0)
		{
			return list2;
		}
		if (!list.All((JsonObject r) => string.IsNullOrWhiteSpace(r["server_name"]?.ToString())))
		{
			return new List<JsonObject>();
		}
		return list;
	}

	private static double? Number(JsonNode? node)
	{
		if (!(node is JsonValue jsonValue))
		{
			return null;
		}
		if (!jsonValue.TryGetValue<double>(out var value))
		{
			if (!jsonValue.TryGetValue<long>(out var value2))
			{
				if (!jsonValue.TryGetValue<int>(out var value3))
				{
					return null;
				}
				return value3;
			}
			return value2;
		}
		return value;
	}

	private static string NormalizedUrl(string value)
	{
		return value.Trim().ToLowerInvariant().TrimEnd('/');
	}
}
