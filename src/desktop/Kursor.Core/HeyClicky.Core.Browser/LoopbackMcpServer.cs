using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HeyClicky.Core.Browser;

public sealed class LoopbackMcpServer : IDisposable
{
	public sealed record ToolDefinition(string Name, string Description, JsonObject InputSchema);

	public delegate Task<JsonObject> ToolCallHandler(string toolName, JsonObject arguments, JsonObject meta);

	public const string ContentItemsResultKey = "mcp_content";

	public const string ServerVersion = "1";

	private readonly string serverName;

	private readonly IReadOnlyList<ToolDefinition> tools;

	private readonly ToolCallHandler handler;

	private readonly LoopbackHttpServer http;

	public string BearerToken => http.BearerToken;

	public int? Port => http.Port;

	public string? Url => http.Url;

	public bool IsListening => http.IsListening;

	public LoopbackMcpServer(string serverName, IReadOnlyList<ToolDefinition> tools, ToolCallHandler handler)
	{
		this.serverName = serverName;
		this.tools = tools;
		this.handler = handler;
		http = new LoopbackHttpServer(HandleAsync);
	}

	public void Start()
	{
		http.Start();
	}

	public void Stop()
	{
		http.Stop();
	}

	public void Dispose()
	{
		http.Dispose();
	}

	private async Task<LoopbackHttpServer.Response> HandleAsync(LoopbackHttpServer.Request request)
	{
		Dictionary<string, string> sessionHeaders = new Dictionary<string, string> { ["Mcp-Session-Id"] = BearerToken.Substring(0, 16) };
		string method = request.Method;
		if (!(method == "POST"))
		{
			if (method == "DELETE")
			{
				return LoopbackHttpServer.Response.Json(200, new JsonObject((JsonNodeOptions?)null), sessionHeaders);
			}
			return LoopbackHttpServer.Response.Json(405, new JsonObject((JsonNodeOptions?)null) { ["error"] = "method_not_allowed" });
		}
		JsonObject jsonObject;
		try
		{
			jsonObject = JsonNode.Parse(request.Body, null) as JsonObject;
		}
		catch
		{
			jsonObject = null;
		}
		if (jsonObject == null)
		{
			return LoopbackHttpServer.Response.Json(400, new JsonObject((JsonNodeOptions?)null)
			{
				["jsonrpc"] = "2.0",
				["id"] = null,
				["error"] = new JsonObject((JsonNodeOptions?)null)
				{
					["code"] = -32700,
					["message"] = "Parse error"
				}
			});
		}
		JsonNode? jsonNode = jsonObject["method"];
		string text = ((jsonNode != null && jsonNode.GetValueKind() == JsonValueKind.String) ? jsonObject["method"].GetValue<string>() : "");
		if (!jsonObject.ContainsKey("id") || jsonObject["id"] == null)
		{
			return LoopbackHttpServer.Response.Json(202, null, sessionHeaders);
		}
		JsonNode requestId = jsonObject["id"].DeepClone();
		JsonObject jsonObject2 = (jsonObject["params"] as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		switch (text)
		{
		case "initialize":
		{
			string value2;
			string text2 = ((jsonObject2["protocolVersion"] is JsonValue jsonValue3 && jsonValue3.TryGetValue<string>(out value2)) ? value2 : "2025-03-26");
			return Reply(new JsonObject((JsonNodeOptions?)null) { ["result"] = new JsonObject((JsonNodeOptions?)null)
			{
				["protocolVersion"] = text2,
				["capabilities"] = new JsonObject((JsonNodeOptions?)null) { ["tools"] = new JsonObject((JsonNodeOptions?)null) },
				["serverInfo"] = new JsonObject((JsonNodeOptions?)null)
				{
					["name"] = serverName,
					["version"] = "1"
				}
			} });
		}
		case "ping":
			return Reply(new JsonObject((JsonNodeOptions?)null) { ["result"] = new JsonObject((JsonNodeOptions?)null) });
		case "tools/list":
		{
			JsonArray jsonArray2 = new JsonArray((JsonNodeOptions?)null);
			foreach (ToolDefinition tool in tools)
			{
				jsonArray2.Add(new JsonObject((JsonNodeOptions?)null)
				{
					["name"] = tool.Name,
					["description"] = tool.Description,
					["inputSchema"] = tool.InputSchema.DeepClone()
				});
			}
			return Reply(new JsonObject((JsonNodeOptions?)null) { ["result"] = new JsonObject((JsonNodeOptions?)null) { ["tools"] = jsonArray2 } });
		}
		case "tools/call":
		{
			string value;
			string toolName = ((jsonObject2["name"] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out value)) ? value : null);
			if (toolName == null || !tools.Any((ToolDefinition t) => t.Name == toolName))
			{
				return Reply(new JsonObject((JsonNodeOptions?)null) { ["error"] = new JsonObject((JsonNodeOptions?)null)
				{
					["code"] = -32602,
					["message"] = "Unknown tool"
				} });
			}
			JsonObject jsonObject3 = (jsonObject2["arguments"] as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
			JsonObject jsonObject4 = (jsonObject2["_meta"] as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
			JsonObject jsonObject5;
			try
			{
				jsonObject5 = await handler(toolName, (JsonObject)jsonObject3.DeepClone(), (JsonObject)jsonObject4.DeepClone()).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				jsonObject5 = new JsonObject((JsonNodeOptions?)null)
				{
					["is_error"] = true,
					["error"] = ex.Message
				};
			}
			bool b = default(bool);
			bool flag = jsonObject5["is_error"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<bool>(out b) && b;
			if (jsonObject5["mcp_content"] is JsonArray jsonArray)
			{
				jsonObject5.Remove("mcp_content");
				return Reply(new JsonObject((JsonNodeOptions?)null) { ["result"] = new JsonObject((JsonNodeOptions?)null)
				{
					["content"] = jsonArray.DeepClone(),
					["isError"] = flag
				} });
			}
			JsonObject jsonObject6 = new JsonObject((JsonNodeOptions?)null);
			JsonObject jsonObject7 = new JsonObject((JsonNodeOptions?)null);
			JsonNode reference = new JsonObject((JsonNodeOptions?)null)
			{
				["type"] = "text",
				["text"] = JsonText(jsonObject5)
			};
			jsonObject7["content"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
			jsonObject7["structuredContent"] = jsonObject5.DeepClone();
			jsonObject7["isError"] = flag;
			jsonObject6["result"] = jsonObject7;
			return Reply(jsonObject6);
		}
		default:
			return Reply(new JsonObject((JsonNodeOptions?)null) { ["error"] = new JsonObject((JsonNodeOptions?)null)
			{
				["code"] = -32601,
				["message"] = "Method not found"
			} });
		}
		LoopbackHttpServer.Response Reply(JsonObject payload)
		{
			payload["jsonrpc"] = "2.0";
			payload["id"] = requestId;
			return LoopbackHttpServer.Response.Json(200, payload, sessionHeaders);
		}
	}

	public static string JsonText(JsonObject value)
	{
		return Sorted(value).ToJsonString();
	}

	private static JsonNode? Sorted(JsonNode? node)
	{
		if (!(node is JsonObject source))
		{
			if (!(node is JsonArray source2))
			{
				return node?.DeepClone();
			}
			return new JsonArray(source2.Select(Sorted).ToArray());
		}
		return new JsonObject(from kv in source.OrderBy<KeyValuePair<string, JsonNode>, string>((KeyValuePair<string, JsonNode> kv) => kv.Key, StringComparer.Ordinal)
			select KeyValuePair.Create(kv.Key, Sorted(kv.Value)), null);
	}
}
