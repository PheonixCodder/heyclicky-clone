using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using HeyClicky.Core.Configuration;

namespace HeyClicky.Core.Telemetry;

public static class ClickyAITracing
{
	public enum Mode
	{
		Normal,
		Agent
	}

	public const string DistinctIdHeader = "X-Clicky-Distinct-Id";

	public const string SessionIdHeader = "X-Clicky-Session-Id";

	public const string TraceIdHeader = "X-Clicky-Trace-Id";

	public const string ModeHeader = "X-Clicky-Mode";

	public const string AgentThreadIdHeader = "X-Clicky-Agent-Thread-Id";

	private static readonly object Gate = new object();

	private static string sessionId = NewId();

	private static string? normalModeTraceId;

	private static Dictionary<string, string>? agentThreadTraceIds;

	private static readonly HashSet<string> TraceIdsWithEmittedStart = new HashSet<string>();

	private static string storePath = Path.Combine(AppBundleConfiguration.AppDataDirectory, "ai-trace-ids.json");

	public static string SessionId
	{
		get
		{
			lock (Gate)
			{
				return sessionId;
			}
		}
	}

	public static void UseStore(string path)
	{
		lock (Gate)
		{
			storePath = path;
			agentThreadTraceIds = null;
		}
	}

	private static string NewId()
	{
		return Guid.NewGuid().ToString().ToLowerInvariant();
	}

	public static string EnsureNormalModeTraceId()
	{
		lock (Gate)
		{
			return normalModeTraceId ?? (normalModeTraceId = NewId());
		}
	}

	public static string TraceIdForAgentThread(string agentThreadId)
	{
		lock (Gate)
		{
			Dictionary<string, string> dictionary = LoadMap();
			if (dictionary.TryGetValue(agentThreadId, out var value))
			{
				return value;
			}
			string result = (dictionary[agentThreadId] = NewId());
			SaveMap(dictionary);
			return result;
		}
	}

	public static IReadOnlyDictionary<string, string> HttpHeaders(Mode mode = Mode.Normal, string? agentThreadId = null)
	{
		if (mode == Mode.Agent && !string.IsNullOrEmpty(agentThreadId))
		{
			return BaseHeaders(TraceIdForAgentThread(agentThreadId), Mode.Agent, agentThreadId);
		}
		return BaseHeaders(EnsureNormalModeTraceId(), Mode.Normal, null);
	}

	private static Dictionary<string, string> BaseHeaders(string traceId, Mode mode, string? agentThreadId)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			["X-Clicky-Distinct-Id"] = ClickyAnalytics.DistinctId,
			["X-Clicky-Session-Id"] = SessionId,
			["X-Clicky-Trace-Id"] = traceId,
			["X-Clicky-Mode"] = ModeName(mode)
		};
		if (!string.IsNullOrEmpty(agentThreadId))
		{
			dictionary["X-Clicky-Agent-Thread-Id"] = agentThreadId;
		}
		return dictionary;
	}

	private static string ModeName(Mode mode)
	{
		if (mode != Mode.Agent)
		{
			return "normal";
		}
		return "agent";
	}

	public static void Apply(HttpRequestHeaders headers)
	{
		if (headers.Contains("X-Clicky-Trace-Id"))
		{
			return;
		}
		IEnumerable<string> values;
		string text = (headers.TryGetValues("X-Clicky-Agent-Thread-Id", out values) ? values.FirstOrDefault() : null);
		foreach (var (name, value) in HttpHeaders((!string.IsNullOrEmpty(text)) ? Mode.Agent : Mode.Normal, text))
		{
			if (!headers.Contains(name))
			{
				headers.TryAddWithoutValidation(name, value);
			}
		}
	}

	public static void EmitTraceStartIfNeeded(Mode mode, string? agentThreadId, JsonObject? extraProperties = null)
	{
		string text;
		if (mode == Mode.Normal)
		{
			text = EnsureNormalModeTraceId();
		}
		else
		{
			if (string.IsNullOrEmpty(agentThreadId))
			{
				return;
			}
			text = TraceIdForAgentThread(agentThreadId);
		}
		lock (Gate)
		{
			if (!TraceIdsWithEmittedStart.Add(text))
			{
				return;
			}
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["$ai_trace_id"] = text,
			["$ai_session_id"] = SessionId,
			["$ai_span_name"] = ((mode == Mode.Normal) ? "normal_companion" : "agent_thread"),
			["clicky_mode"] = ModeName(mode),
			["clicky_app_version"] = ClickyAnalytics.AppVersion
		};
		if (!string.IsNullOrEmpty(agentThreadId))
		{
			jsonObject["clicky_agent_thread_id"] = agentThreadId;
		}
		Merge(jsonObject, extraProperties);
		ClickyAnalytics.Capture("$ai_trace", jsonObject);
	}

	public static void EmitAgentGeneration(string agentThreadId, string spanName, string? model, string userMessage, string assistantMessage, long? inputTokens, long? outputTokens, double? latencySeconds, bool isError, string? error, JsonObject? extraProperties = null)
	{
		if (!string.IsNullOrEmpty(agentThreadId))
		{
			string text = TraceIdForAgentThread(agentThreadId);
			JsonNode reference = new JsonObject((JsonNodeOptions?)null)
			{
				["role"] = "user",
				["content"] = Cap(userMessage, 8192)
			};
			JsonArray jsonArray = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
			JsonNode reference2 = new JsonObject((JsonNodeOptions?)null)
			{
				["role"] = "assistant",
				["content"] = Cap(assistantMessage, 8192)
			};
			JsonArray jsonArray2 = new JsonArray(new ReadOnlySpan<JsonNode>(in reference2));
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
			{
				["$ai_trace_id"] = text,
				["$ai_session_id"] = SessionId,
				["$ai_span_name"] = spanName,
				["$ai_provider"] = "openai",
				["$ai_input"] = jsonArray,
				["$ai_input_state"] = jsonArray.DeepClone(),
				["$ai_output_choices"] = jsonArray2,
				["$ai_output_state"] = jsonArray2.DeepClone(),
				["clicky_mode"] = "agent",
				["clicky_agent_thread_id"] = agentThreadId
			};
			if (!string.IsNullOrEmpty(model))
			{
				jsonObject["$ai_model"] = model;
			}
			if (inputTokens.HasValue)
			{
				long valueOrDefault = inputTokens.GetValueOrDefault();
				jsonObject["$ai_input_tokens"] = valueOrDefault;
			}
			if (outputTokens.HasValue)
			{
				long valueOrDefault2 = outputTokens.GetValueOrDefault();
				jsonObject["$ai_output_tokens"] = valueOrDefault2;
			}
			if (latencySeconds.HasValue)
			{
				double valueOrDefault3 = latencySeconds.GetValueOrDefault();
				jsonObject["$ai_latency"] = valueOrDefault3;
			}
			if (isError)
			{
				jsonObject["$ai_is_error"] = true;
			}
			if (error != null)
			{
				jsonObject["$ai_error"] = Cap(error, 1024);
			}
			Merge(jsonObject, extraProperties);
			ClickyAnalytics.Capture("$ai_generation", jsonObject);
		}
	}

	public static void EmitClientErrorSpan(Mode mode, string? agentThreadId, string stage, string error)
	{
		if (mode == Mode.Agent && string.IsNullOrEmpty(agentThreadId))
		{
			mode = Mode.Normal;
		}
		string text = ((mode == Mode.Normal) ? EnsureNormalModeTraceId() : TraceIdForAgentThread(agentThreadId));
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["$ai_trace_id"] = text,
			["$ai_session_id"] = SessionId,
			["$ai_span_name"] = "client_error",
			["$ai_is_error"] = true,
			["$ai_error"] = Cap(error, 1024),
			["clicky_mode"] = ModeName(mode),
			["clicky_stage"] = stage
		};
		if (mode == Mode.Agent)
		{
			jsonObject["clicky_agent_thread_id"] = agentThreadId;
		}
		ClickyAnalytics.Capture("$ai_span", jsonObject);
	}

	private static string Cap(string text, int limit)
	{
		if (text.Length > limit)
		{
			return text.Substring(0, limit) + "…";
		}
		return text;
	}

	private static void Merge(JsonObject target, JsonObject? extra)
	{
		if (extra == null)
		{
			return;
		}
		foreach (KeyValuePair<string, JsonNode> item in extra)
		{
			item.Deconstruct(out var key, out var value);
			string propertyName = key;
			target[propertyName] = value?.DeepClone();
		}
	}

	private static Dictionary<string, string> LoadMap()
	{
		if (agentThreadTraceIds != null)
		{
			return agentThreadTraceIds;
		}
		agentThreadTraceIds = new Dictionary<string, string>();
		try
		{
			if (File.Exists(storePath) && JsonNode.Parse(File.ReadAllText(storePath), null) is JsonObject jsonObject)
			{
				foreach (var (key, jsonNode2) in jsonObject)
				{
					if (jsonNode2 is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value) && !string.IsNullOrEmpty(value))
					{
						agentThreadTraceIds[key] = value;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("ai-tracing", "trace id map ignored: " + ex.Message);
		}
		return agentThreadTraceIds;
	}

	private static void SaveMap(Dictionary<string, string> map)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(storePath));
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
			foreach (KeyValuePair<string, string> item in map)
			{
				item.Deconstruct(out var key, out var value);
				string propertyName = key;
				string text = value;
				jsonObject[propertyName] = text;
			}
			File.WriteAllText(storePath, jsonObject.ToJsonString());
		}
		catch (Exception ex)
		{
			Log.Warn("ai-tracing", "trace id map not saved: " + ex.Message);
		}
	}
}
