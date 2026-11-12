using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace HeyClicky.Agents.Runtime;

public static class IntegrationUse
{
	private static readonly HashSet<string> ServerKeys = new HashSet<string> { "server", "serverid", "servername", "mcpserver", "mcpserverid", "mcpservername" };

	private static readonly HashSet<string> ToolKeys = new HashSet<string> { "functionname", "name", "tool", "toolname", "toolslug", "toolslugs" };

	private static readonly HashSet<string> SkippedKeys = new HashSet<string> { "result", "output", "aggregatedoutput", "stdout", "stderr", "content", "encryptedcontent" };

	private static readonly Dictionary<string, string> GoogleAliases = new Dictionary<string, string>
	{
		["sheets"] = "googlesheets",
		["calendar"] = "googlecalendar",
		["drive"] = "googledrive",
		["docs"] = "googledocs"
	};

	private static readonly HashSet<string> Rejected = new HashSet<string> { "composio", "computer", "computeruse", "mcp", "tool", "tools", "function", "call", "unknown" };

	public static string? ProviderKey(JsonObject item)
	{
		bool flag;
		switch (item["type"]?.ToString())
		{
		case "mcpToolCall":
		case "mcp_tool_call":
		case "dynamicToolCall":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return null;
		}
		List<string> list = new List<string>();
		Collect(item, ServerKeys, list);
		string text = list.FirstOrDefault((string s) => s.StartsWith("custom_", StringComparison.Ordinal));
		if (text != null)
		{
			return text;
		}
		List<string> list2 = new List<string>(list);
		Collect(item, ToolKeys, list2);
		if (!list2.Any((string v) => Normalized(v).Contains("composio", StringComparison.Ordinal)))
		{
			return null;
		}
		foreach (string item2 in list2)
		{
			string text2 = ToolkitSlug(item2);
			if (text2 != null)
			{
				return text2;
			}
		}
		return null;
	}

	private static string Normalized(string key)
	{
		return new string(key.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
	}

	private static void Collect(JsonNode? node, HashSet<string> keys, List<string> into)
	{
		if (!(node is JsonObject jsonObject))
		{
			if (!(node is JsonArray jsonArray))
			{
				return;
			}
			{
				foreach (JsonNode item2 in jsonArray)
				{
					Collect(item2, keys, into);
				}
				return;
			}
		}
		foreach (KeyValuePair<string, JsonNode> item3 in jsonObject)
		{
			item3.Deconstruct(out var key, out var value);
			string key2 = key;
			JsonNode node2 = value;
			string item = Normalized(key2);
			if (!SkippedKeys.Contains(item))
			{
				if (keys.Contains(item))
				{
					Leaves(node2, into);
				}
				Collect(node2, keys, into);
			}
		}
	}

	private static void Leaves(JsonNode? node, List<string> into)
	{
		if (!(node is JsonValue jsonValue))
		{
			if (node is JsonArray jsonArray)
			{
				{
					foreach (JsonNode item in jsonArray)
					{
						Leaves(item, into);
					}
					return;
				}
			}
			if (!(node is JsonObject jsonObject))
			{
				return;
			}
			{
				foreach (KeyValuePair<string, JsonNode> item2 in jsonObject)
				{
					item2.Deconstruct(out var _, out var value);
					Leaves(value, into);
				}
				return;
			}
		}
		if (jsonValue.TryGetValue<string>(out string value2) && value2 != null)
		{
			into.Add(value2);
		}
	}

	internal static string? ToolkitSlug(string raw)
	{
		string text = raw.Trim();
		if (text.Length == 0)
		{
			return null;
		}
		if (text.StartsWith("mcp__", StringComparison.OrdinalIgnoreCase))
		{
			string[] array = text.Split("__");
			if (array.Length >= 3)
			{
				text = string.Join("__", array.Skip(2));
			}
		}
		string text2 = new string((text.Split(new char[6] { '_', '.', '-', ' ', '/', ':' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "").ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
		if (text2.Length < 3 || Rejected.Contains(text2))
		{
			return null;
		}
		return GoogleAliases.GetValueOrDefault(text2, text2);
	}
}
