using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HeyClicky.Agents.Harness;

namespace HeyClicky.Agents.Jobs;

public static class ClickyHarnessRecord
{
	public sealed class Turn
	{
		public int Number { get; init; }

		public string RawAsk { get; set; } = "";

		public List<string> ToolLines { get; } = new List<string>();

		public string? FinalText { get; set; }

		public string? Status { get; set; }

		public DateTimeOffset? EndedAt { get; set; }
	}

	public static string? RecordDirectory(string recordsRoot, string threadId)
	{
		if (!AgentHarnessPreferences.IsHarnessThread(threadId))
		{
			return null;
		}
		string text = Path.Combine(recordsRoot, threadId);
		if (!Directory.Exists(text))
		{
			return null;
		}
		return text;
	}

	public static IReadOnlyList<(string ThreadId, string Workspace)> AllRecords(string recordsRoot)
	{
		List<(string, string)> list = new List<(string, string)>();
		string[] array;
		try
		{
			array = (Directory.Exists(recordsRoot) ? Directory.GetDirectories(recordsRoot) : Array.Empty<string>());
		}
		catch (Exception)
		{
			return list;
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			try
			{
				string path = Path.Combine(text, "job.json");
				if (File.Exists(path) && JsonNode.Parse(File.ReadAllText(path), null) is JsonObject jsonObject && jsonObject["cfg"] is JsonObject jsonObject2 && jsonObject2["workspace"] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value) && value != null)
				{
					list.Add((Path.GetFileName(text), value));
				}
			}
			catch (Exception)
			{
			}
		}
		return list;
	}

	public static IReadOnlyList<Turn> Turns(string recordDirectory)
	{
		Dictionary<int, Turn> dictionary = new Dictionary<int, Turn>();
		try
		{
			string path = Path.Combine(recordDirectory, "turns.json");
			if (File.Exists(path) && JsonNode.Parse(File.ReadAllText(path), null) is JsonObject jsonObject)
			{
				foreach (var (s, jsonNode2) in jsonObject)
				{
					if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
					{
						string value;
						string rawAsk = ((jsonNode2 is JsonObject jsonObject2 && jsonObject2["text"] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out value)) ? (value ?? "") : "");
						dictionary[result] = new Turn
						{
							Number = result,
							RawAsk = rawAsk
						};
					}
				}
			}
		}
		catch (Exception)
		{
		}
		string[] array;
		try
		{
			string path2 = Path.Combine(recordDirectory, "events.jsonl");
			if (!File.Exists(path2))
			{
				return dictionary.Values.OrderBy((Turn t) => t.Number).ToList();
			}
			array = File.ReadAllLines(path2);
		}
		catch (Exception)
		{
			return dictionary.Values.OrderBy((Turn t) => t.Number).ToList();
		}
		int num = 1;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.Contains("\"kind\":\"tool_call\"", StringComparison.Ordinal) && !text2.Contains("\"kind\":\"message\"", StringComparison.Ordinal) && !text2.Contains("\"kind\":\"turn_end\"", StringComparison.Ordinal))
			{
				continue;
			}
			JsonObject jsonObject3;
			try
			{
				jsonObject3 = JsonNode.Parse(text2, null) as JsonObject;
			}
			catch (Exception)
			{
				continue;
			}
			if (jsonObject3 == null)
			{
				continue;
			}
			string text3 = Str(jsonObject3["kind"]);
			if (text3 == null)
			{
				continue;
			}
			int result2;
			int num2 = (int.TryParse(Str(jsonObject3["turn"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out result2) ? result2 : (Int(jsonObject3["turn"]) ?? num));
			if (!dictionary.TryGetValue(num2, out var value2))
			{
				value2 = new Turn
				{
					Number = num2
				};
			}
			switch (text3)
			{
			case "tool_call":
			{
				string text6 = ToolLine(jsonObject3);
				if (text6 != null)
				{
					value2.ToolLines.Add(text6);
				}
				break;
			}
			case "message":
				if (Str(jsonObject3["phase"]) == "final")
				{
					value2.FinalText = Str(jsonObject3["text"]);
				}
				break;
			case "turn_end":
			{
				value2.Status = Str(jsonObject3["status"]);
				Turn turn = value2;
				if (turn.FinalText == null)
				{
					string text5 = (turn.FinalText = Str(jsonObject3["finalText"]));
				}
				double result3;
				double? num3 = Double(jsonObject3["t"]) ?? (double.TryParse(Str(jsonObject3["t"]), NumberStyles.Float, CultureInfo.InvariantCulture, out result3) ? new double?(result3) : ((double?)null));
				if (num3.HasValue)
				{
					double valueOrDefault = num3.GetValueOrDefault();
					value2.EndedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)valueOrDefault).ToLocalTime();
				}
				num = num2 + 1;
				break;
			}
			}
			dictionary[num2] = value2;
		}
		return dictionary.Values.OrderBy((Turn t) => t.Number).ToList();
	}

	public static string? ToolLine(JsonObject record)
	{
		string text = Str(record["name"]);
		if (text == null || text == "update_plan")
		{
			return null;
		}
		JsonObject jsonObject = (record["args"] as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		string text2 = Str(jsonObject["command"]) ?? Str(jsonObject["code"]) ?? Str(jsonObject["query"]) ?? Str(jsonObject["url"]) ?? SortedJson(jsonObject);
		string text3 = "- " + text + ": " + ClickyWorkIndex.OneLine(text2, 220);
		if (!((record["ok"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var value)) ? value : (Str(record["ok"]) != "False")))
		{
			string text4 = Str(record["error"]);
			if (text4 != null && text4.Length > 0)
			{
				text3 = text3 + " → failed: " + ClickyWorkIndex.OneLine(text4, 220);
			}
		}
		return text3;
	}

	private static string SortedJson(JsonObject arguments)
	{
		try
		{
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
			foreach (KeyValuePair<string, JsonNode> item in arguments.OrderBy<KeyValuePair<string, JsonNode>, string>((KeyValuePair<string, JsonNode> p) => p.Key, StringComparer.Ordinal))
			{
				item.Deconstruct(out var key, out var value);
				string propertyName = key;
				jsonObject[propertyName] = value?.DeepClone();
			}
			return jsonObject.ToJsonString(new JsonSerializerOptions
			{
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			});
		}
		catch (Exception)
		{
			return "";
		}
	}

	private static string? Str(JsonNode? node)
	{
		if (!(node is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
		{
			return null;
		}
		return value;
	}

	private static int? Int(JsonNode? node)
	{
		if (!(node is JsonValue jsonValue) || !jsonValue.TryGetValue<int>(out var value))
		{
			return null;
		}
		return value;
	}

	private static double? Double(JsonNode? node)
	{
		if (!(node is JsonValue jsonValue) || !jsonValue.TryGetValue<double>(out var value))
		{
			return null;
		}
		return value;
	}
}
