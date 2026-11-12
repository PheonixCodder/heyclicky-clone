using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Agents;

public static class AgentDefaults
{
	private static readonly object Gate = new object();

	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = false
	};

	private static string? path;

	private static JsonObject values = new JsonObject((JsonNodeOptions?)null);

	public static void Configure(string stateDirectory)
	{
		lock (Gate)
		{
			Directory.CreateDirectory(stateDirectory);
			string text = Path.Combine(stateDirectory, "defaults.json");
			if (!string.Equals(text, path, StringComparison.OrdinalIgnoreCase))
			{
				path = text;
				values = Load(text);
			}
		}
	}

	private static JsonObject Load(string file)
	{
		try
		{
			if (File.Exists(file) && JsonNode.Parse(File.ReadAllText(file), null) is JsonObject result)
			{
				return result;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "defaults.json unreadable, starting fresh: " + ex.Message);
		}
		return new JsonObject((JsonNodeOptions?)null);
	}

	private static void SaveLocked()
	{
		if (path == null)
		{
			return;
		}
		try
		{
			string sourceFileName = path + ".tmp";
			File.WriteAllText(sourceFileName, values.ToJsonString(Json));
			File.Move(sourceFileName, path, overwrite: true);
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "defaults.json not saved: " + ex.Message);
		}
	}

	public static string? String(string key)
	{
		lock (Gate)
		{
			string value;
			return (values[key] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out value)) ? value : null;
		}
	}

	public static bool Bool(string key)
	{
		lock (Gate)
		{
			bool value = default(bool);
			return values[key] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value) && value;
		}
	}

	public static T? Get<T>(string key)
	{
		lock (Gate)
		{
			JsonNode jsonNode = values[key];
			if (jsonNode == null)
			{
				return default(T);
			}
			try
			{
				return jsonNode.Deserialize<T>();
			}
			catch (JsonException)
			{
				return default(T);
			}
		}
	}

	public static void Set(string key, string? value)
	{
		SetNode(key, (value == null) ? null : JsonValue.Create(value, (JsonNodeOptions?)null));
	}

	public static void Set(string key, bool value)
	{
		SetNode(key, JsonValue.Create(value, (JsonNodeOptions?)null));
	}

	public static void Set<T>(string key, T value)
	{
		SetNode(key, (value == null) ? null : JsonSerializer.SerializeToNode(value));
	}

	public static void Remove(string key)
	{
		SetNode(key, null);
	}

	public static IReadOnlyList<string> Keys(string prefix)
	{
		lock (Gate)
		{
			return (from kv in values
				select kv.Key into k
				where k.StartsWith(prefix, StringComparison.Ordinal)
				select k).ToList();
		}
	}

	private static void SetNode(string key, JsonNode? node)
	{
		lock (Gate)
		{
			if (node == null)
			{
				if (!values.Remove(key))
				{
					return;
				}
			}
			else
			{
				JsonNode jsonNode = values[key];
				if (jsonNode != null && JsonNode.DeepEquals(jsonNode, node))
				{
					return;
				}
				values[key] = node;
			}
			SaveLocked();
		}
	}
}
