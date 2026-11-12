using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Proactive;

public sealed class ProactiveAgentsClient
{
	private readonly WorkerClient worker;

	public ProactiveAgentsClient(WorkerClient worker)
	{
		this.worker = worker;
	}

	public async Task<ProactiveJobStatus> StartJobAsync(ProactiveRequestInputs inputs, DateTimeOffset now, CancellationToken ct = default(CancellationToken))
	{
		string json = BuildRequestBody(inputs, now).ToJsonString();
		try
		{
			return ParseJob((await worker.SendRawAsync(HttpMethod.Post, "/proactive-agents/jobs", Json(json), authenticated: true, ct, TimeSpan.FromSeconds(30L)).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Sign in to get suggestions."), inputs.CheckReason, now);
		}
		catch (WorkerHttpException ex) when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			int status = ex.Status;
			return ((status == 429 || status == 503) ? 1 : 0) != 0;
		}).Invoke())
		{
			throw new InvalidOperationException(ErrorMessage(ex.Body) ?? $"The suggestion request failed (HTTP {ex.Status}).");
		}
		catch (WorkerHttpException ex2)
		{
			throw new InvalidOperationException($"The suggestion request failed (HTTP {ex2.Status}).");
		}
	}

	public async Task<ProactiveJobStatus> PollJobAsync(string jobId, ProactiveCheckReason reason, DateTimeOffset now, CancellationToken ct = default(CancellationToken))
	{
		return ParseJob((await worker.SendRawAsync(HttpMethod.Get, "/proactive-agents/jobs/" + Uri.EscapeDataString(jobId), null, authenticated: true, ct, TimeSpan.FromSeconds(20L)).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Signed out."), reason, now);
	}

	public async Task ReportResolutionAsync(IReadOnlyList<int> suggestionIds, string status, string? denyReason = null)
	{
		if (suggestionIds.Count == 0)
		{
			return;
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["suggestion_ids"] = new JsonArray(((IEnumerable<int>)suggestionIds).Select((Func<int, JsonNode>)((int i) => i)).ToArray()),
			["status"] = status
		};
		if (denyReason != null)
		{
			jsonObject["deny_reason"] = denyReason;
		}
		try
		{
			WorkerClient workerClient = worker;
			HttpMethod post = HttpMethod.Post;
			ByteArrayContent body = Json(jsonObject.ToJsonString());
			TimeSpan? timeout = TimeSpan.FromSeconds(20L);
			await workerClient.SendRawAsync(post, "/proactive-agents/resolution", body, authenticated: true, default(CancellationToken), timeout).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Info("Proactive", "resolution " + status + " not recorded: " + ex.Message);
		}
	}

	public async Task<IReadOnlyDictionary<int, string>?> FetchStatusesAsync(IReadOnlyList<int> suggestionIds, CancellationToken ct = default(CancellationToken))
	{
		if (suggestionIds.Count == 0)
		{
			return null;
		}
		try
		{
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["suggestion_ids"] = new JsonArray(((IEnumerable<int>)suggestionIds).Select((Func<int, JsonNode>)((int i) => i)).ToArray()) };
			byte[] array = await worker.SendRawAsync(HttpMethod.Post, "/proactive-agents/suggestions/statuses", Json(jsonObject.ToJsonString()), authenticated: true, ct, TimeSpan.FromSeconds(15L)).ConfigureAwait(continueOnCapturedContext: false);
			if (array == null || !(JsonNode.Parse(array, null)?["statuses"] is JsonObject jsonObject2))
			{
				return null;
			}
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			foreach (var (s, jsonNode2) in jsonObject2)
			{
				if (int.TryParse(s, out var result))
				{
					string text2 = jsonNode2?.GetValue<string>();
					if (text2 != null)
					{
						dictionary[result] = text2;
					}
				}
			}
			return dictionary;
		}
		catch (Exception ex)
		{
			Log.Info("Proactive", "status sync: " + ex.Message);
			return null;
		}
	}

	public async Task<bool> ClaimMorningInvitationAsync(IReadOnlyList<int> suggestionIds, CancellationToken ct = default(CancellationToken))
	{
		if (suggestionIds.Count == 0)
		{
			return true;
		}
		try
		{
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["suggestion_ids"] = new JsonArray(((IEnumerable<int>)suggestionIds).Select((Func<int, JsonNode>)((int i) => i)).ToArray()) };
			byte[] array = await worker.SendRawAsync(HttpMethod.Post, "/proactive-agents/invitation-claim", Json(jsonObject.ToJsonString()), authenticated: true, ct, TimeSpan.FromSeconds(10L)).ConfigureAwait(continueOnCapturedContext: false);
			int result;
			if (array != null)
			{
				JsonNode? jsonNode = JsonNode.Parse(array, null);
				result = ((jsonNode == null || jsonNode["granted"]?.GetValue<bool>() != false) ? 1 : 0);
			}
			else
			{
				result = 1;
			}
			return (byte)result != 0;
		}
		catch (Exception)
		{
			return true;
		}
	}

	public static JsonObject BuildRequestBody(ProactiveRequestInputs inputs, DateTimeOffset now)
	{
		JsonObject jsonObject2 = new JsonObject((JsonNodeOptions?)null)
		{
			["connected_integrations"] = new JsonArray(((IEnumerable<string>)inputs.ConnectedIntegrationNames).Select((Func<string, JsonNode>)((string n) => n)).ToArray()),
			["check_reason"] = inputs.CheckReason.Wire(),
			["recent_agent_suggestions"] = new JsonArray(((IEnumerable<ProactiveHistoryEntry>)ProactiveHistory.WireEntries(inputs.RecentSuggestions, now)).Select((Func<ProactiveHistoryEntry, JsonNode>)delegate(ProactiveHistoryEntry e)
			{
				JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
				{
					["title"] = e.Title,
					["description"] = e.Description ?? "",
					["status"] = e.WireStatus,
					["hours_ago"] = Math.Max(0.0, (now - e.DecidedAt).TotalHours)
				};
				if (e.DenyReason != null)
				{
					jsonObject["deny_reason"] = e.DenyReason;
				}
				return jsonObject;
			}).ToArray()),
			["local_day"] = LocalDay(now),
			["jobs"] = true,
			["reports_suggestion_views"] = true
		};
		if (inputs.ResearchOnlyForGoodMorning)
		{
			jsonObject2["research_only"] = true;
		}
		if (inputs.ResearchOnlyForGoodMorning || inputs.ReadsForMorningPaper)
		{
			jsonObject2["brief_style"] = "good_morning";
		}
		if (inputs.ExistingRoutines.Count > 0)
		{
			jsonObject2["routines"] = new JsonArray(inputs.ExistingRoutines.Select<(string, int), JsonNode>(((string Title, int EveryMinutes) r) => new JsonObject((JsonNodeOptions?)null)
			{
				["title"] = r.Title,
				["every_minutes"] = r.EveryMinutes
			}).ToArray());
		}
		if (inputs.CheckReason == ProactiveCheckReason.Morning)
		{
			jsonObject2["supports_morning_notch"] = true;
		}
		if (inputs.Clickys.Count > 0)
		{
			jsonObject2["clickys"] = new JsonArray(((IEnumerable<ProactiveClickyDescriptor>)inputs.Clickys).Select((Func<ProactiveClickyDescriptor, JsonNode>)((ProactiveClickyDescriptor c) => new JsonObject((JsonNodeOptions?)null)
			{
				["slug"] = c.Slug,
				["name"] = c.Name,
				["role"] = c.Role,
				["description"] = c.Description,
				["routines"] = new JsonArray(c.Routines.Select<(string, int), JsonNode>(((string Title, int EveryMinutes) r) => new JsonObject((JsonNodeOptions?)null)
				{
					["title"] = r.Title,
					["every_minutes"] = r.EveryMinutes
				}).ToArray()),
				["thread_ids"] = new JsonArray(((IEnumerable<string>)c.ThreadIds).Select((Func<string, JsonNode>)((string t) => t)).ToArray())
			})).ToArray());
		}
		if (inputs.CustomConnectors.Count > 0)
		{
			jsonObject2["custom_connectors"] = new JsonArray(inputs.CustomConnectors.Select<(string, string), JsonNode>(((string Name, string Description) c) => new JsonObject((JsonNodeOptions?)null)
			{
				["name"] = c.Name,
				["description"] = c.Description
			}).ToArray());
		}
		return jsonObject2;
	}

	public static string LocalDay(DateTimeOffset now)
	{
		return now.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
	}

	public static ProactiveJobStatus ParseJob(byte[] bytes, ProactiveCheckReason reason, DateTimeOffset now)
	{
		JsonObject o = (JsonNode.Parse(bytes, null) as JsonObject) ?? throw new InvalidOperationException("The suggestion service returned an invalid response.");
		ProactiveJobState state = o["status"]?.GetValue<string>() switch
		{
			"running" => ProactiveJobState.Running, 
			"succeeded" => ProactiveJobState.Succeeded, 
			"failed" => ProactiveJobState.Failed, 
			_ => ProactiveJobState.Queued, 
		};
		IReadOnlyList<ProactiveSuggestionCard> cards = null;
		if (o["suggested_agents"] is JsonArray agents)
		{
			cards = ParseCards(agents, reason, now);
		}
		string text = o["break_until"]?.GetValue<string>();
		DateTimeOffset result;
		DateTimeOffset? breakUntil = ((text != null && DateTimeOffset.TryParse(text, out result)) ? new DateTimeOffset?(result) : ((DateTimeOffset?)null));
		return new ProactiveJobStatus(o["job_id"]?.GetValue<string>() ?? "", state, cards, o["error"]?.GetValue<string>(), breakUntil)
		{
			CuratedEvidence = Str("curated_evidence"),
			MorningHeadline = Str("morning_headline"),
			MorningHeadlineLine = Str("morning_headline_line")
		};
		string? Str(string key)
		{
			if (!(o[key] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
			{
				return null;
			}
			return value;
		}
	}

	public static IReadOnlyList<ProactiveSuggestionCard> ParseCards(JsonArray agents, ProactiveCheckReason reason, DateTimeOffset now)
	{
		List<ProactiveSuggestionCard> list = new List<ProactiveSuggestionCard>();
		foreach (JsonObject item in agents.OfType<JsonObject>())
		{
			JsonObject a = item;
			string text = Str("title");
			string text2 = Str("description");
			string text3 = Str("agent_prompt");
			if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(text3))
			{
				ProactiveNewClicky newClicky = ((a["new_clicky"] is JsonObject jsonObject) ? new ProactiveNewClicky(jsonObject["name"]?.GetValue<string>() ?? "", jsonObject["role"]?.GetValue<string>() ?? "", jsonObject["description"]?.GetValue<string>() ?? "") : null);
				int value;
				ProactiveRoutine routine = ((a["routine"] is JsonObject jsonObject2 && jsonObject2["every_minutes"] is JsonValue jsonValue && jsonValue.TryGetValue<int>(out value)) ? new ProactiveRoutine(value, jsonObject2["title"]?.GetValue<string>() ?? "") : null);
				int value2;
				int? serverSuggestionId = ((a["suggestion_id"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<int>(out value2)) ? new int?(value2) : ((int?)null));
				ProactiveSuggestionCard obj = new ProactiveSuggestionCard
				{
					Title = text,
					Description = text2,
					AgentPrompt = text3,
					CheckReason = reason,
					ServerSuggestionId = serverSuggestionId
				};
				string text4 = Str("clicky_slug");
				obj.ClickySlug = ((text4 != null && text4.Length > 0) ? text4 : null);
				obj.NewClicky = newClicky;
				obj.Routine = routine;
				string text5 = Str("integration");
				obj.IntegrationName = ((text5 != null && text5.Length > 0) ? text5 : null);
				obj.CreatedAt = now;
				list.Add(obj);
			}
			string? Str(string k)
			{
				if (!(a[k] is JsonValue jsonValue3) || !jsonValue3.TryGetValue<string>(out string value3))
				{
					return null;
				}
				return value3.Trim();
			}
		}
		return list;
	}

	private static ByteArrayContent Json(string json)
	{
		ByteArrayContent byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		return byteArrayContent;
	}

	private static string? ErrorMessage(string body)
	{
		try
		{
			return JsonNode.Parse(body, null)?["error"]?.GetValue<string>();
		}
		catch (JsonException)
		{
			return null;
		}
	}
}
