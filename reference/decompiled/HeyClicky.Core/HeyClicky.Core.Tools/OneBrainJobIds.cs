using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Tools;

public static class OneBrainJobIds
{
	private static readonly JsonSerializerOptions Relaxed = new JsonSerializerOptions
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public static string ArgumentsWithThreadIds(string argumentsJson, Func<string, (bool IsJob, string? ThreadId)> threadIdForJobId)
	{
		JsonObject jsonObject;
		try
		{
			jsonObject = JsonNode.Parse(argumentsJson, null) as JsonObject;
		}
		catch (JsonException)
		{
			return argumentsJson;
		}
		if (jsonObject == null)
		{
			return argumentsJson;
		}
		string text = null;
		if (jsonObject.TryGetPropertyValue("job_id", out JsonNode jsonNode))
		{
			jsonObject.Remove("job_id");
			text = Str(jsonNode);
		}
		if (text == null)
		{
			text = Str(jsonObject["codexThreadId"]);
		}
		text = text?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return argumentsJson;
		}
		var (flag, text2) = threadIdForJobId(text);
		if (!flag)
		{
			jsonObject["codexThreadId"] = text;
		}
		else if (text2 != null)
		{
			jsonObject["codexThreadId"] = text2;
		}
		else
		{
			jsonObject.Remove("codexThreadId");
		}
		return jsonObject.ToJsonString(Relaxed);
	}

	public static string OutputWithJobIds(string output, Func<string, string> jobIdForThreadId)
	{
		JsonNode jsonNode;
		try
		{
			jsonNode = JsonNode.Parse(output, null);
		}
		catch (JsonException)
		{
			return output.Replace("codexThreadId", "job_id", StringComparison.Ordinal);
		}
		if (jsonNode == null)
		{
			return output.Replace("codexThreadId", "job_id", StringComparison.Ordinal);
		}
		return Rewrite(jsonNode, jobIdForThreadId)?.ToJsonString(Relaxed) ?? output;
	}

	private static JsonNode? Rewrite(JsonNode? node, Func<string, string> jobIdForThreadId)
	{
		if (!(node is JsonObject jsonObject))
		{
			if (!(node is JsonArray source))
			{
				if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value))
				{
					return JsonValue.Create(value.Replace("codexThreadId", "job_id", StringComparison.Ordinal), (JsonNodeOptions?)null);
				}
				return node?.DeepClone();
			}
			return new JsonArray(source.Select((JsonNode item) => Rewrite(item, jobIdForThreadId)).ToArray());
		}
		JsonObject jsonObject2 = jsonObject;
		JsonObject jsonObject3 = new JsonObject((JsonNodeOptions?)null);
		foreach (var (text2, node2) in jsonObject2)
		{
			if (text2 == "codexThreadId")
			{
				string text3 = Str(node2);
				if (text3 != null)
				{
					if (!jsonObject2.ContainsKey("job_id"))
					{
						jsonObject3["job_id"] = jobIdForThreadId(text3);
					}
					continue;
				}
			}
			jsonObject3[text2] = Rewrite(node2, jobIdForThreadId);
		}
		return jsonObject3;
	}

	private static string? Str(JsonNode? node)
	{
		if (!(node is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
		{
			return null;
		}
		return value;
	}
}
