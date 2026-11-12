using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.Agents.Harness;

public sealed class AgentHarnessRunnerRuntime(AgentHarness harness, AgentHarnessPlace place) : IAgentHarnessRuntime
{
	private static readonly HttpClient Http = new HttpClient(new SocketsHttpHandler
	{
		UseProxy = false,
		PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2L)
	})
	{
		Timeout = Timeout.InfiniteTimeSpan
	};

	public AgentHarness Harness { get; } = harness;

	public AgentHarnessPlace Place { get; } = place;

	public async Task RunTurnAsync(AgentHarnessTurn turn, Action<AgentHarnessEvent> emit, CancellationToken ct)
	{
		AgentHarnessRunnerProcess.Endpoint endpoint = await AgentHarnessRunnerProcess.EnsureRunningAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		string job = turn.HarnessThreadId;
		JsonObject jsonObject = await RequestAsync(endpoint, HttpMethod.Get, "/events?job=" + Escaped(job) + "&after=0&wait=0", null, ct).ConfigureAwait(continueOnCapturedContext: false);
		long cursor = ((jsonObject["latest"] != null) ? Long(jsonObject["latest"]) : (from e in ((jsonObject["events"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
			select Long(e["seq"])).DefaultIfEmpty(0L).Max());
		JsonObject jsonObject3 = new JsonObject((JsonNodeOptions?)null)
		{
			["job"] = job,
			["harness"] = Harness.RawValue(),
			["place"] = Place.RawValue(),
			["model"] = turn.Model.RawValue(),
			["workspace"] = turn.Workspace,
			["instructions"] = turn.Context.Instructions,
			["mcp"] = new JsonArray(((IEnumerable<AgentHarnessMcpServer>)turn.Context.McpServers).Select((Func<AgentHarnessMcpServer, JsonNode>)delegate(AgentHarnessMcpServer server)
			{
				AgentHarnessLocalMcpCommand localCommand = server.LocalCommand;
				if ((object)localCommand != null)
				{
					return new JsonObject((JsonNodeOptions?)null)
					{
						["name"] = server.Name,
						["command"] = localCommand.Command,
						["args"] = new JsonArray(((IEnumerable<string>)localCommand.Arguments).Select((Func<string, JsonNode>)((string a) => a)).ToArray()),
						["env"] = new JsonObject(localCommand.Environment.Select<KeyValuePair<string, string>, KeyValuePair<string, JsonNode>>((KeyValuePair<string, string> kv) => KeyValuePair.Create(kv.Key, (JsonNode?)kv.Value)), null)
					};
				}
				JsonObject jsonObject2 = new JsonObject((JsonNodeOptions?)null)
				{
					["name"] = server.Name,
					["url"] = server.Url
				};
				string bearerToken = server.BearerToken;
				if (bearerToken != null)
				{
					jsonObject2["token"] = bearerToken;
				}
				if (server.Headers.Count > 0)
				{
					jsonObject2["headers"] = new JsonObject(server.Headers.Select<KeyValuePair<string, string>, KeyValuePair<string, JsonNode>>((KeyValuePair<string, string> h) => KeyValuePair.Create(h.Key, (JsonNode?)h.Value)), null);
				}
				if (server.SendsTurnIdentity)
				{
					string turnId = turn.TurnId;
					if (turnId != null)
					{
						jsonObject2["meta"] = new JsonObject((JsonNodeOptions?)null) { ["x-codex-turn-metadata"] = new JsonObject((JsonNodeOptions?)null)
						{
							["thread_id"] = turn.AppThreadId,
							["turn_id"] = turnId
						} };
					}
				}
				return jsonObject2;
			}).ToArray()),
			["skills"] = new JsonArray(((IEnumerable<string>)turn.Context.SkillDirectories).Select((Func<string, JsonNode>)((string d) => d)).ToArray()),
			["userSkills"] = new JsonArray(((IEnumerable<string>)turn.Context.UserSkillDirectories).Select((Func<string, JsonNode>)((string d) => d)).ToArray()),
			["leaseMetadata"] = new JsonObject((turn.LeaseMetadata ?? new Dictionary<string, string>()).Select<KeyValuePair<string, string>, KeyValuePair<string, JsonNode>>((KeyValuePair<string, string> kv) => KeyValuePair.Create(kv.Key, (JsonNode?)kv.Value)), null),
			["deviceHelper"] = AgentHarnessPreferences.UsesDeviceHelper,
			["worker"] = new JsonObject((JsonNodeOptions?)null)
			{
				["base"] = turn.Context.WorkerBaseUrl,
				["sessionToken"] = turn.Context.SessionToken
			},
			["text"] = turn.Text,
			["images"] = new JsonArray(((IEnumerable<string>)turn.ImagePaths).Select((Func<string, JsonNode>)((string p) => p)).ToArray())
		};
		if (turn.Context.UsesPlanTool)
		{
			jsonObject3["planTool"] = true;
		}
		string notePath = turn.Context.NotePath;
		if (notePath != null)
		{
			jsonObject3["note"] = new JsonObject((JsonNodeOptions?)null)
			{
				["path"] = notePath,
				["kept"] = turn.Context.IsKeptThing,
				["tools"] = turn.Context.WritesNoteWithTools
			};
		}
		JsonObject jsonObject4 = await RequestAsync(endpoint, HttpMethod.Post, "/turn", jsonObject3, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (jsonObject4["after"] != null)
		{
			cursor = Math.Max(cursor, Long(jsonObject4["after"]));
		}
		string thisTurnRef = jsonObject4["turnRef"]?.GetValue<string>();
		while (true)
		{
			foreach (JsonObject item in (((await RequestAsync(endpoint, HttpMethod.Get, $"/events?job={Escaped(job)}&after={cursor}&wait=20000", null, ct).ConfigureAwait(continueOnCapturedContext: false))["events"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>())
			{
				cursor = Math.Max(cursor, Long(item["seq"]));
				if (thisTurnRef != null)
				{
					string text = item["turnRef"]?.GetValue<string>();
					if (text != null && text != thisTurnRef)
					{
						continue;
					}
				}
				if (Deliver(item, emit))
				{
					return;
				}
			}
		}
	}

	internal static bool Deliver(JsonObject e, Action<AgentHarnessEvent> emit)
	{
		JsonNode? jsonNode = e["text"];
		string text = ((jsonNode != null && jsonNode.GetValueKind() == JsonValueKind.String) ? e["text"].ToString() : "");
		switch (e["kind"]?.ToString())
		{
		case "commentary":
			if (text.Trim().Length > 0)
			{
				emit(new AgentHarnessEvent.Commentary(text));
			}
			break;
		case "thinking":
			if (text.Trim().Length > 0)
			{
				emit(new AgentHarnessEvent.Thinking(text));
			}
			break;
		case "plan":
		{
			List<(string, string)> list = (from s in ((e["plan"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
				select (Text: s["step"]?.ToString()?.Trim() ?? "", Status: s["status"]?.ToString() ?? "pending") into s
				where s.Text.Length > 0
				select s).ToList();
			if (list.Count > 0)
			{
				emit(new AgentHarnessEvent.Plan(list));
			}
			break;
		}
		case "tool":
		{
			string id = e["id"]?.ToString() ?? Guid.NewGuid().ToString("D");
			string text2 = e["name"]?.ToString() ?? "tool";
			string server = e["server"]?.ToString();
			string command = e["command"]?.ToString();
			bool flag = e["phase"]?.ToString() == "start";
			string text3 = e["url"]?.ToString();
			string text4 = ((text3 != null && text3.Length > 0) ? text3 : null);
			if (text2 == "web_search")
			{
				if (flag)
				{
					emit(new AgentHarnessEvent.WebSearch(id, e["query"]?.ToString() ?? "", text4));
				}
			}
			else if (text2 == "fetch_url" && text4 != null)
			{
				if (flag)
				{
					emit(new AgentHarnessEvent.WebSearch(id, "", text4));
				}
			}
			else if (flag)
			{
				emit(new AgentHarnessEvent.ToolStarted(id, text2, server, command));
			}
			else
			{
				emit(new AgentHarnessEvent.ToolFinished(id, text2, server, command));
			}
			break;
		}
		case "batch":
		{
			ClickyProgramBatch clickyProgramBatch = ClickyProgramBatch.FromEvent(e);
			if ((object)clickyProgramBatch != null)
			{
				emit(new AgentHarnessEvent.ProgramBatch(clickyProgramBatch));
			}
			break;
		}
		case "answer":
			if (text.Length > 0)
			{
				emit(new AgentHarnessEvent.AnswerDelta(text));
			}
			break;
		case "stats":
		{
			Dictionary<string, double> dictionary = new Dictionary<string, double>();
			foreach (var (text6, jsonNode3) in e)
			{
				if (text6 != "seq" && jsonNode3 is JsonValue jsonValue && jsonValue.GetValueKind() == JsonValueKind.Number && jsonValue.TryGetValue<double>(out var value))
				{
					dictionary[text6] = value;
				}
			}
			emit(new AgentHarnessEvent.TurnStats(dictionary));
			break;
		}
		case "turn_end":
			emit(new AgentHarnessEvent.Finished(e["status"]?.ToString() ?? "completed", e["error"]?.ToString()));
			return true;
		case "note":
		{
			string text9 = e["path"]?.ToString();
			if (text9 == null)
			{
				break;
			}
			if (e["phase"]?.ToString() == "preview")
			{
				if (text.Length > 0)
				{
					emit(new AgentHarnessEvent.NotePreview(text9, text));
				}
			}
			else
			{
				emit(new AgentHarnessEvent.NoteSaved(text9));
			}
			break;
		}
		case "apps_unreachable":
			emit(new AgentHarnessEvent.ConnectedAppsUnreachable());
			break;
		case "compacted":
			emit(new AgentHarnessEvent.Compacted());
			break;
		case "tools_listed":
			if (!(e["servers"] is JsonArray { Count: >0 } jsonArray))
			{
				break;
			}
			Log.Info("Harness", "a job's tools: " + string.Join(" · ", jsonArray.OfType<JsonObject>().Select(delegate(JsonObject listedServer)
			{
				string text7 = listedServer["server"]?.ToString() ?? "?";
				int value2;
				int value3 = ((listedServer["tools"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<int>(out value2)) ? value2 : 0);
				if (!(listedServer["ok"] is JsonValue jsonValue3) || !jsonValue3.TryGetValue<bool>(out var value4) || value4)
				{
					return $"{text7}: {value3} tools";
				}
				string text8 = listedServer["error"]?.ToString() ?? "no answer";
				return text7 + ": no tools (couldn't list them: " + ((text8.Length > 200) ? text8.Substring(0, 200) : text8) + ")";
			})));
			break;
		case "log":
			Log.Info("Harness", "[harness] " + ((text.Length > 600) ? text.Substring(0, 600) : text));
			break;
		}
		return false;
	}

	public async Task<bool> SteerAsync(string harnessThreadId, string text, bool informational, CancellationToken ct, IReadOnlyList<string>? imagePaths = null)
	{
		AgentHarnessRunnerProcess.Endpoint endpoint = await AgentHarnessRunnerProcess.EnsureRunningAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["job"] = harnessThreadId,
			["text"] = text
		};
		if (informational)
		{
			jsonObject["info"] = true;
		}
		if (imagePaths != null && imagePaths.Count > 0)
		{
			jsonObject["images"] = new JsonArray(((IEnumerable<string>)imagePaths).Select((Func<string, JsonNode>)((string p) => p)).ToArray());
		}
		JsonObject jsonObject2 = await RequestAsync(endpoint, HttpMethod.Post, "/steer", jsonObject, ct).ConfigureAwait(continueOnCapturedContext: false);
		bool accepted = default(bool);
		if (jsonObject2["ok"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out accepted) && accepted)
		{
			return true;
		}
		if (jsonObject2["reason"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<string>(out string value) && value == "not running")
		{
			return false;
		}
		throw new AgentHarnessException("This agent can't take a message right now. Send it again once it finishes.");
	}

	public async Task InterruptAsync(string harnessThreadId)
	{
		_ = 1;
		try
		{
			await RequestAsync(await AgentHarnessRunnerProcess.EnsureRunningAsync().ConfigureAwait(continueOnCapturedContext: false), HttpMethod.Post, "/cancel", new JsonObject((JsonNodeOptions?)null) { ["job"] = harnessThreadId }, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Harness", "cancel " + harnessThreadId + ": " + ex.Message);
		}
	}

	private static string Escaped(string value)
	{
		return Uri.EscapeDataString(value);
	}

	private static long Long(JsonNode? n)
	{
		if (!(n is JsonValue jsonValue) || !jsonValue.TryGetValue<long>(out var value))
		{
			if (!(n is JsonValue jsonValue2) || !jsonValue2.TryGetValue<double>(out var value2))
			{
				return 0L;
			}
			return (long)value2;
		}
		return value;
	}

	private static async Task<JsonObject> RequestAsync(AgentHarnessRunnerProcess.Endpoint endpoint, HttpMethod method, string route, JsonObject? body, CancellationToken ct)
	{
		using HttpRequestMessage request = new HttpRequestMessage(method, $"http://127.0.0.1:{endpoint.Port}{route}");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Token);
		if (body != null)
		{
			request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
		}
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeout.CancelAfter(TimeSpan.FromSeconds(60L));
		using HttpResponseMessage response = await Http.SendAsync(request, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
		string json = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
		JsonObject jsonObject;
		try
		{
			jsonObject = (JsonNode.Parse(json, null) as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		}
		catch (JsonException)
		{
			jsonObject = new JsonObject((JsonNodeOptions?)null);
		}
		if (response.StatusCode != HttpStatusCode.OK)
		{
			throw new AgentHarnessException(jsonObject["error"]?.ToString() ?? $"The agent harness runner answered {response.StatusCode}.");
		}
		return jsonObject;
	}
}
