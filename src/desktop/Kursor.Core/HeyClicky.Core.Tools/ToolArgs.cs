using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Tools;

public sealed class ToolArgs
{
	public JsonObject Json { get; }

	public string Raw { get; }

	public ToolArgs(string? arguments)
	{
		Raw = (string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments);
		try
		{
			Json = (JsonNode.Parse(Raw, null) as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		}
		catch (JsonException)
		{
			Json = new JsonObject((JsonNodeOptions?)null);
		}
	}

	public string? Str(params string[] keys)
	{
		foreach (string propertyName in keys)
		{
			if (Json[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value) && !string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return null;
	}

	public string? Exact(string key)
	{
		if (!(Json[key] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
		{
			return null;
		}
		return value;
	}

	public bool? Bool(string key)
	{
		if (!(Json[key] is JsonValue jsonValue))
		{
			return null;
		}
		if (jsonValue.TryGetValue<bool>(out var value))
		{
			return value;
		}
		if (jsonValue.TryGetValue<string>(out string value2) && bool.TryParse(value2, out var result))
		{
			return result;
		}
		return null;
	}

	public int? Int(string key)
	{
		if (!(Json[key] is JsonValue jsonValue))
		{
			return null;
		}
		if (jsonValue.TryGetValue<int>(out var value))
		{
			return value;
		}
		if (jsonValue.TryGetValue<long>(out var value2))
		{
			return (int)Math.Clamp(value2, -2147483648L, 2147483647L);
		}
		if (jsonValue.TryGetValue<double>(out var value3))
		{
			return (int)value3;
		}
		if (jsonValue.TryGetValue<string>(out string value4) && int.TryParse(value4, out var result))
		{
			return result;
		}
		return null;
	}

	public JsonObject? Object(string key)
	{
		return Json[key] as JsonObject;
	}
}
