using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Connectors;

public static class McpProbe
{
	private sealed record Posted(int Status, string Body, bool IsEventStream, string? SessionId);

	private sealed class Usage
	{
		public int Count;

		public bool HasWrite;
	}

	private sealed class Family
	{
		public int Total;

		public bool HasWrite;

		public int BareCount;

		public readonly Dictionary<string, int> TrailingCounts = new Dictionary<string, int>();

		public readonly List<string> TrailingOrder = new List<string>();
	}

	public const int MaximumDescriptionLength = 240;

	private const int MaximumNounFamilies = 6;

	private const int MaximumToolListPages = 5;

	private const string ProtocolVersion = "2025-06-18";

	private static readonly HashSet<string> ReadVerbs = new HashSet<string>
	{
		"list", "get", "search", "find", "fetch", "read", "query", "retrieve", "describe", "check",
		"show", "count", "lookup", "browse", "view", "preview", "extract"
	};

	private static readonly HashSet<string> WriteVerbs = new HashSet<string>
	{
		"create", "save", "update", "delete", "remove", "add", "set", "send", "post", "put",
		"patch", "upload", "activate", "pause", "boost", "merge", "submit", "resolve", "retire", "restore",
		"share", "unshare", "mark", "connect", "disconnect", "archive", "move", "assign", "import", "export",
		"sync", "run", "execute", "trigger", "cancel", "start", "stop", "edit", "rename", "publish",
		"unpublish", "invite", "reply", "comment", "schedule", "prepare", "finalize", "log", "write", "insert",
		"append", "duplicate", "clone", "generate", "apply", "approve", "reject", "complete", "close", "open",
		"enable", "disable", "toggle", "attach", "detach", "link", "unlink"
	};

	private static readonly HashSet<string> NoiseTokens = new HashSet<string>
	{
		"by", "id", "ids", "from", "to", "for", "with", "and", "or", "a",
		"an", "the", "of", "in", "on", "all", "my", "me", "one", "single",
		"multiple", "batch", "v1", "v2", "api", "tool", "tools", "mcp", "local", "remote",
		"ui", "internal"
	};

	public static async Task<McpToolCatalog?> FetchToolCatalogAsync(HttpClient http, Uri serverUrl, string? bearerToken, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default(CancellationToken))
	{
		(JsonObject, string)? tuple = await SendRequestAsync(http, serverUrl, bearerToken, headers, null, 1, "initialize", InitializeParams(), ct).ConfigureAwait(continueOnCapturedContext: false);
		if (tuple.HasValue)
		{
			(JsonObject, string) valueOrDefault = tuple.GetValueOrDefault();
			string sessionId = valueOrDefault.Item2;
			JsonObject jsonObject = valueOrDefault.Item1["serverInfo"] as JsonObject;
			string title = (jsonObject?["title"] ?? jsonObject?["name"])?.ToString()?.Trim();
			await PostAsync(http, serverUrl, bearerToken, headers, sessionId, new JsonObject((JsonNodeOptions?)null)
			{
				["jsonrpc"] = "2.0",
				["method"] = "notifications/initialized"
			}, ct).ConfigureAwait(continueOnCapturedContext: false);
			List<string> names = new List<string>();
			string text = null;
			int id = 2;
			for (int page = 0; page < 5; page++)
			{
				JsonObject jsonObject2 = new JsonObject((JsonNodeOptions?)null);
				if (text != null)
				{
					jsonObject2["cursor"] = text;
				}
				(JsonObject, string)? tuple2 = await SendRequestAsync(http, serverUrl, bearerToken, headers, sessionId, id++, "tools/list", jsonObject2, ct).ConfigureAwait(continueOnCapturedContext: false);
				if (!tuple2.HasValue)
				{
					break;
				}
				(JsonObject, string) valueOrDefault2 = tuple2.GetValueOrDefault();
				names.AddRange((from t in ((valueOrDefault2.Item1["tools"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
					select t["name"]?.ToString()).OfType<string>());
				text = valueOrDefault2.Item1["nextCursor"]?.ToString();
				if (string.IsNullOrEmpty(text))
				{
					break;
				}
			}
			if (names.Count == 0)
			{
				return string.IsNullOrEmpty(title) ? null : new McpToolCatalog(title, Array.Empty<string>());
			}
			return new McpToolCatalog(string.IsNullOrEmpty(title) ? null : title, names);
		}
		return null;
	}

	public static async Task<McpVerification> VerifyConnectionAsync(HttpClient http, Uri serverUrl, string? bearerToken, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default(CancellationToken))
	{
		Posted posted = await PostAsync(http, serverUrl, bearerToken, headers, null, new JsonObject((JsonNodeOptions?)null)
		{
			["jsonrpc"] = "2.0",
			["id"] = 1,
			["method"] = "initialize",
			["params"] = InitializeParams()
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		McpVerification mcpVerification = McpVerification.Failure(posted?.Status);
		if ((object)mcpVerification != null)
		{
			return mcpVerification;
		}
		string sessionId = posted.SessionId;
		await PostAsync(http, serverUrl, bearerToken, headers, sessionId, new JsonObject((JsonNodeOptions?)null)
		{
			["jsonrpc"] = "2.0",
			["method"] = "notifications/initialized"
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		Posted posted2 = await PostAsync(http, serverUrl, bearerToken, headers, sessionId, new JsonObject((JsonNodeOptions?)null)
		{
			["jsonrpc"] = "2.0",
			["id"] = 2,
			["method"] = "tools/list",
			["params"] = new JsonObject((JsonNodeOptions?)null)
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		McpVerification mcpVerification2 = McpVerification.Failure(posted2?.Status);
		if ((object)mcpVerification2 != null)
		{
			return mcpVerification2;
		}
		return new McpVerification.Reachable((ParseJsonRpcResponse(posted2.Body, posted2.IsEventStream, 2)?["result"]?["tools"] as JsonArray)?.Count ?? 0);
	}

	private static JsonObject InitializeParams()
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["protocolVersion"] = "2025-06-18",
			["capabilities"] = new JsonObject((JsonNodeOptions?)null),
			["clientInfo"] = new JsonObject((JsonNodeOptions?)null)
			{
				["name"] = "HeyClicky",
				["version"] = "1"
			}
		};
	}

	private static async Task<(JsonObject Result, string? SessionId)?> SendRequestAsync(HttpClient http, Uri serverUrl, string? bearerToken, IReadOnlyDictionary<string, string> headers, string? sessionId, int id, string method, JsonObject p, CancellationToken ct)
	{
		Posted posted = await PostAsync(http, serverUrl, bearerToken, headers, sessionId, new JsonObject((JsonNodeOptions?)null)
		{
			["jsonrpc"] = "2.0",
			["id"] = id,
			["method"] = method,
			["params"] = p
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		bool flag = (object)posted == null;
		if (!flag)
		{
			int status = posted.Status;
			bool flag2 = ((status < 200 || status >= 300) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return null;
		}
		if (!(ParseJsonRpcResponse(posted.Body, posted.IsEventStream, id)?["result"] is JsonObject item))
		{
			return null;
		}
		return (item, posted.SessionId ?? sessionId);
	}

	private static async Task<Posted?> PostAsync(HttpClient http, Uri serverUrl, string? bearerToken, IReadOnlyDictionary<string, string> headers, string? sessionId, JsonObject payload, CancellationToken ct)
	{
		_ = 1;
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, serverUrl)
			{
				Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
			};
			request.Headers.Accept.ParseAdd("application/json");
			request.Headers.Accept.ParseAdd("text/event-stream");
			request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", "2025-06-18");
			foreach (var (text3, value) in headers)
			{
				if (text3.Length > 0)
				{
					request.Headers.TryAddWithoutValidation(text3, value);
				}
			}
			if (!string.IsNullOrEmpty(bearerToken))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
			}
			if (!string.IsNullOrEmpty(sessionId))
			{
				request.Headers.TryAddWithoutValidation("Mcp-Session-Id", sessionId);
			}
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeout.CancelAfter(TimeSpan.FromSeconds(20L));
			using HttpResponseMessage response = await http.SendAsync(request, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			string body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			string text4 = response.Content.Headers.ContentType?.MediaType ?? "";
			IEnumerable<string> values;
			string sessionId2 = (response.Headers.TryGetValues("Mcp-Session-Id", out values) ? values.FirstOrDefault() : null);
			return new Posted((int)response.StatusCode, body, text4.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase), sessionId2);
		}
		catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException) ? true : false) && !ct.IsCancellationRequested)
		{
			return null;
		}
	}

	internal static JsonObject? ParseJsonRpcResponse(string body, bool isEventStream, int requestId)
	{
		try
		{
			if (!isEventStream)
			{
				return JsonNode.Parse(body, null) as JsonObject;
			}
			JsonObject result = null;
			string[] array = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].TrimEnd('\r');
				if (!text.StartsWith("data:", StringComparison.Ordinal))
				{
					continue;
				}
				string text2 = text;
				if (JsonNode.Parse(text2.Substring(5, text2.Length - 5).Trim(), null) is JsonObject jsonObject)
				{
					if (jsonObject["id"] is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var value) && value == requestId)
					{
						return jsonObject;
					}
					if (jsonObject["result"] != null)
					{
						result = jsonObject;
					}
				}
			}
			return result;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	public static string? DeriveCapabilityDescription(string displayName, IReadOnlyList<string> toolNames)
	{
		List<List<string>> list = (from t in toolNames.Select(Tokenize)
			where t.Count > 0
			select t).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		List<List<string>> list2 = list;
		string lead = list[0][0];
		if (!ReadVerbs.Contains(lead) && !WriteVerbs.Contains(lead) && list.Count((List<string> t) => t[0] == lead) * 10 >= list.Count * 6)
		{
			list2 = list.Select((List<string> t) => (!(t[0] == lead) || t.Count <= 1) ? t : t.Skip(1).ToList()).ToList();
		}
		Dictionary<(string, string), Usage> dictionary = new Dictionary<(string, string), Usage>();
		List<(string, string)> list3 = new List<(string, string)>();
		foreach (List<string> item2 in list2)
		{
			string text = null;
			List<string> list4 = new List<string>();
			foreach (string item3 in item2)
			{
				if (text == null && (ReadVerbs.Contains(item3) || WriteVerbs.Contains(item3)))
				{
					text = item3;
				}
				else
				{
					if (NoiseTokens.Contains(item3))
					{
						continue;
					}
					string text2 = Singularized(item3);
					if (list4.Count > 0)
					{
						if (list4[list4.Count - 1] == text2)
						{
							continue;
						}
					}
					list4.Add(text2);
				}
			}
			if (list4.Count != 0)
			{
				(string, string) tuple = (list4[0], string.Join(' ', list4));
				if (!dictionary.TryGetValue(tuple, out var value))
				{
					value = (dictionary[tuple] = new Usage());
					list3.Add(tuple);
				}
				value.Count++;
				if (text != null && WriteVerbs.Contains(text))
				{
					value.HasWrite = true;
				}
			}
		}
		if (dictionary.Count == 0)
		{
			return null;
		}
		Dictionary<string, Family> families = new Dictionary<string, Family>();
		List<string> list5 = new List<string>();
		foreach (var item4 in list3)
		{
			Usage usage2 = dictionary[item4];
			if (!families.TryGetValue(item4.Item1, out Family value2))
			{
				value2 = (families[item4.Item1] = new Family());
				list5.Add(item4.Item1);
			}
			value2.Total += usage2.Count;
			value2.HasWrite |= usage2.HasWrite;
			if (item4.Item2 == item4.Item1)
			{
				value2.BareCount += usage2.Count;
				continue;
			}
			string text3 = item4.Item2.Split(' ')[^1];
			if (!value2.TrailingCounts.ContainsKey(text3))
			{
				value2.TrailingOrder.Add(text3);
			}
			value2.TrailingCounts[text3] = value2.TrailingCounts.GetValueOrDefault(text3) + usage2.Count;
		}
		List<string> source = (from x in list5.Select((string name, int index) => (name: name, index: index))
			orderby families[x.name].Total descending, x.index
			select x.name).Take(6).ToList();
		List<string> readWrite = source.Where((string n) => families[n].HasWrite).Select(Label).ToList();
		List<string> readOnly = source.Where((string n) => !families[n].HasWrite).Select(Label).ToList();
		string text4 = displayName.Trim();
		string prefix = ((text4.Length == 0) ? "" : (text4 + ": "));
		while (true)
		{
			string text5 = Compose();
			if (text5 == null)
			{
				break;
			}
			if (text5.Length <= 240)
			{
				return text5;
			}
			if (readOnly.Count > 0)
			{
				readOnly.RemoveAt(readOnly.Count - 1);
				continue;
			}
			if (readWrite.Count <= 1)
			{
				break;
			}
			readWrite.RemoveAt(readWrite.Count - 1);
		}
		return null;
		string? Compose()
		{
			List<string> list6 = new List<string>();
			if (readWrite.Count > 0)
			{
				list6.Add("find, create, and update " + string.Join(", ", readWrite));
			}
			if (readOnly.Count > 0)
			{
				list6.Add("read " + string.Join(", ", readOnly));
			}
			if (list6.Count != 0)
			{
				return prefix + string.Join("; ", list6) + ".";
			}
			return null;
		}
		string Label(string name)
		{
			Family f = families[name];
			if (f.BareCount > 0 || f.TrailingOrder.Count == 0)
			{
				return Pluralized(name);
			}
			string item = (from x in f.TrailingOrder.Select((string t, int i) => (t: t, i: i))
				orderby f.TrailingCounts[x.t] descending, x.i
				select x).First().t;
			if (!(item == name))
			{
				return name + " " + Pluralized(item);
			}
			return Pluralized(name);
		}
	}

	private static List<string> Tokenize(string toolName)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		foreach (char c in toolName)
		{
			if (char.IsUpper(c) && flag)
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(char.IsLetterOrDigit(c) ? c : ' ');
			flag = char.IsLower(c);
		}
		return stringBuilder.ToString().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.ToList();
	}

	private static string Singularized(string word)
	{
		if (word.EndsWith("ies", StringComparison.Ordinal) && word.Length > 4)
		{
			string text = word;
			return text.Substring(0, text.Length - 3) + "y";
		}
		if ((word.EndsWith("ses", StringComparison.Ordinal) || word.EndsWith("xes", StringComparison.Ordinal) || word.EndsWith("shes", StringComparison.Ordinal) || word.EndsWith("ches", StringComparison.Ordinal)) && word.Length > 4)
		{
			string text = word;
			return text.Substring(0, text.Length - 2);
		}
		if (word.EndsWith('s') && !word.EndsWith("ss", StringComparison.Ordinal) && word.Length > 3)
		{
			string text = word;
			return text.Substring(0, text.Length - 1);
		}
		return word;
	}

	private static string Pluralized(string word)
	{
		if (word.EndsWith('y') && word.Length > 2)
		{
			if (!"aeiou".Contains(word[word.Length - 2]))
			{
				return word.Substring(0, word.Length - 1) + "ies";
			}
		}
		if (word.EndsWith('s') || word.EndsWith('x') || word.EndsWith("sh", StringComparison.Ordinal) || word.EndsWith("ch", StringComparison.Ordinal))
		{
			return word + "es";
		}
		return word + "s";
	}
}
