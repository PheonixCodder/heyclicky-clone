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
using HeyClicky.Agents.Runtime;

namespace HeyClicky.Agents.Backend;

internal sealed class AgentWorkerApi
{
	public enum ComposioFetchKind
	{
		Connected,
		NoConnectedApps,
		Unreachable
	}

	public Uri WorkerBaseUrl { get; }

	private readonly HttpClient http;

	public AgentWorkerApi(Uri workerBaseUrl, HttpClient http)
	{
		WorkerBaseUrl = workerBaseUrl;
		this.http = http;
	}

	private Uri Route(string path)
	{
		return new Uri(WorkerBaseUrl, path.TrimStart('/'));
	}

	private async Task<(int Status, string Body)> SendAsync(HttpMethod method, string path, string bearer, JsonObject? body, CancellationToken ct, TimeSpan timeout, IDictionary<string, string>? headers = null)
	{
		using HttpRequestMessage request = new HttpRequestMessage(method, Route(path));
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		if (headers != null)
		{
			foreach (var (name, value) in headers)
			{
				request.Headers.TryAddWithoutValidation(name, value);
			}
		}
		if (body != null || method == HttpMethod.Post)
		{
			request.Content = new StringContent((body ?? new JsonObject((JsonNodeOptions?)null)).ToJsonString(), Encoding.UTF8, "application/json");
		}
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeout);
		using HttpResponseMessage response = await http.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		string item = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		return (Status: (int)response.StatusCode, Body: item);
	}

	private static JsonObject? ParseObject(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return null;
		}
		try
		{
			return JsonNode.Parse(body, null) as JsonObject;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	public async Task<(string Token, DateTimeOffset ExpiresAt, bool DeveloperModelSelection)> MintSessionTokenAsync(string supabaseAccessToken, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Post, "/agent/session-token", supabaseAccessToken, new JsonObject((JsonNodeOptions?)null), ct, TimeSpan.FromSeconds(30L)).ConfigureAwait(continueOnCapturedContext: false);
		if (num != 200)
		{
			throw new AgentBackendException("/agent/session-token", num, body);
		}
		JsonObject? jsonObject = ParseObject(body);
		string text = jsonObject?["token"]?.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new AgentBackendException("/agent/session-token", num, "empty token");
		}
		double value;
		DateTimeOffset item = ((jsonObject["expires_at"] is JsonValue jsonValue && jsonValue.TryGetValue<double>(out value)) ? DateTimeOffset.FromUnixTimeMilliseconds((long)(value * 1000.0)) : DateTimeOffset.UtcNow.AddMinutes(15.0));
		bool b = default(bool);
		bool item2 = jsonObject["capabilities"]?["developer_model_selection"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<bool>(out b) && b;
		return (Token: text, ExpiresAt: item, DeveloperModelSelection: item2);
	}

	public async Task<AgentIntegrationCatalog> ListIntegrationsAsync(string agentSessionToken, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Get, "/agent/integrations?limit=1000", agentSessionToken, null, ct, TimeSpan.FromSeconds(30L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException("/agent/integrations", num, body);
		}
		return AgentIntegrationsApi.ParseCatalog(ParseObject(body));
	}

	public async Task<string> ConnectIntegrationAsync(string agentSessionToken, string toolkit, CancellationToken ct)
	{
		string path = "/agent/integrations/" + Uri.EscapeDataString(toolkit) + "/connect";
		var (num, text) = await SendAsync(HttpMethod.Post, path, agentSessionToken, new JsonObject((JsonNodeOptions?)null), ct, TimeSpan.FromSeconds(30L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException(path, num, AgentIntegrationsApi.ErrorText(text, text));
		}
		string text2 = ParseObject(text)?["redirect_url"]?.ToString();
		if (text2 == null || text2.Length <= 0)
		{
			throw new AgentBackendException(path, num, "HeyClicky received an invalid integration connect URL.");
		}
		return text2;
	}

	public async Task DisconnectIntegrationAsync(string agentSessionToken, string toolkit, string? connectedAccountId, CancellationToken ct)
	{
		string path = "/agent/integrations/" + Uri.EscapeDataString(toolkit) + "/disconnect";
		JsonObject body = ((connectedAccountId == null) ? new JsonObject((JsonNodeOptions?)null) : new JsonObject((JsonNodeOptions?)null) { ["connected_account_id"] = connectedAccountId });
		var (num, text) = await SendAsync(HttpMethod.Post, path, agentSessionToken, body, ct, TimeSpan.FromSeconds(30L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException(path, num, AgentIntegrationsApi.ErrorText(text, text));
		}
	}

	public async Task LabelIntegrationAccountAsync(string agentSessionToken, string toolkit, string connectedAccountId, string label, CancellationToken ct)
	{
		string path = $"/agent/integrations/{Uri.EscapeDataString(toolkit)}/accounts/{Uri.EscapeDataString(connectedAccountId)}/label";
		var (num, text) = await SendAsync(HttpMethod.Post, path, agentSessionToken, new JsonObject((JsonNodeOptions?)null) { ["label"] = label }, ct, TimeSpan.FromSeconds(20L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException(path, num, AgentIntegrationsApi.ErrorText(text, text));
		}
	}

	public async Task<(ComposioFetchKind Kind, ComposioMcpConfiguration? Config, Exception? Error)> ComposioSessionAsync(string agentSessionToken, CancellationToken ct)
	{
		int[] retryDelays = new int[2] { 1, 3 };
		int attempt = 0;
		bool en = default(bool);
		while (true)
		{
			try
			{
				var (num, body) = await SendAsync(HttpMethod.Post, "/agent/composio/session", agentSessionToken, new JsonObject((JsonNodeOptions?)null), ct, TimeSpan.FromSeconds(20L)).ConfigureAwait(continueOnCapturedContext: false);
				if ((num < 200 || num >= 300) ? true : false)
				{
					throw new AgentBackendException("/agent/composio/session", num, body);
				}
				JsonObject jsonObject = ParseObject(body) ?? throw new AgentBackendException("/agent/composio/session", -1, body);
				bool num2 = jsonObject["enabled"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out en) && en;
				string text = jsonObject["proxy_url"]?.ToString();
				if (!num2 || string.IsNullOrEmpty(text))
				{
					return (Kind: ComposioFetchKind.NoConnectedApps, Config: null, Error: null);
				}
				List<string> toolkits = (jsonObject["toolkits"] as JsonArray)?.Select((JsonNode n) => n?.ToString()).OfType<string>().ToList() ?? new List<string>();
				List<string> list = new List<string>();
				foreach (JsonObject item in ((jsonObject["multi_account_toolkits"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>())
				{
					List<JsonObject> list2 = ((item["accounts"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>().ToList();
					IEnumerable<string> values = list2.Select(delegate(JsonObject a, int i)
					{
						string text2 = PromptSafe(a["account_name"]?.ToString());
						string text3 = PromptSafe(a["label"]?.ToString());
						return ((text2 != null && text3 != null) ? (text3 + " (" + text2 + ")") : (text2 ?? text3 ?? $"account {i + 1}")) + " [account id " + (PromptSafe(a["connected_account_id"]?.ToString()) ?? "unknown") + "]";
					});
					string text4 = item["toolkit"]?.ToString() ?? "";
					list.Add($"{PromptSafe(text4) ?? text4} has {list2.Count} connected accounts: {string.Join(", ", values)}.");
				}
				return (Kind: ComposioFetchKind.Connected, Config: new ComposioMcpConfiguration(text, toolkits, list), Error: null);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
			{
				bool flag = ((ex is HttpRequestException || ex is TaskCanceledException) ? true : false);
				bool flag2 = flag;
				if (!flag2)
				{
					bool flag3 = ((ex is AgentBackendException { StatusCode: var statusCode } && (statusCode >= 500 || statusCode == -1 || statusCode == 408 || statusCode == 429)) ? true : false);
					flag2 = flag3;
				}
				if (!flag2 || attempt >= retryDelays.Length)
				{
					Log.Warn("Agents", $"composio session fetch failed after {attempt + 1} attempt(s): {ex.Message}");
					return (Kind: ComposioFetchKind.Unreachable, Config: null, Error: ex);
				}
				await Task.Delay(TimeSpan.FromSeconds(retryDelays[attempt]), ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			attempt++;
		}
	}

	public async Task<AgentTurnLease?> RecordAgentLaunchAsync(string supabaseAccessToken, string? threadId, string turnId, Guid taskId, bool isFollowUp, string launchSource, bool autoApproveExtraUsage, string usageContext, CancellationToken ct)
	{
		JsonObject body = new JsonObject((JsonNodeOptions?)null)
		{
			["supports_agent_turn_lease"] = true,
			["thread_id"] = threadId,
			["turn_id"] = turnId,
			["task_id"] = taskId.ToString("D").ToUpperInvariant(),
			["is_follow_up"] = isFollowUp,
			["launch_source"] = launchSource,
			["idempotency_key"] = turnId,
			["extra_usage_auto_approve"] = autoApproveExtraUsage
		};
		var (num, body2) = await SendAsync(HttpMethod.Post, "/agent/record-agent-launch", supabaseAccessToken, body, ct, TimeSpan.FromSeconds(15L), new Dictionary<string, string> { ["X-Clicky-Usage-Context"] = usageContext }).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException("/agent/record-agent-launch", num, body2);
		}
		return AgentTurnLease.Parse(ParseObject(body2));
	}

	public async Task<AgentTurnLease?> LeaseStatusAsync(string supabaseAccessToken, string leaseId, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Get, "/agent/turn-lease/" + leaseId + "/status", supabaseAccessToken, null, ct, TimeSpan.FromSeconds(15L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException("/agent/turn-lease/status", num, body);
		}
		return AgentTurnLease.Parse(ParseObject(body));
	}

	public async Task<(bool Continued, AgentTurnLease? Lease, string? Reason)?> ContinueAfterComputerUseConsentAsync(string supabaseAccessToken, string leaseId, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Post, "/agent/turn-lease/" + leaseId + "/continue-after-computer-use-consent", supabaseAccessToken, new JsonObject((JsonNodeOptions?)null), ct, TimeSpan.FromSeconds(15L)).ConfigureAwait(continueOnCapturedContext: false);
		if (num >= 500 || num < 0)
		{
			return null;
		}
		if ((num < 200 || num >= 300) ? true : false)
		{
			return (false, null, $"status_{num}");
		}
		JsonObject jsonObject = ParseObject(body);
		if (!(jsonObject?["continued"] is JsonValue jsonValue) || !jsonValue.TryGetValue<bool>(out var value))
		{
			return null;
		}
		if (!value)
		{
			return (false, null, jsonObject["reason"]?.ToString() ?? "unknown");
		}
		AgentTurnLease agentTurnLease = AgentTurnLease.Parse(jsonObject);
		return ((object)agentTurnLease == null) ? (((bool, AgentTurnLease, string)?)null) : new(bool, AgentTurnLease, string)?((true, agentTurnLease, null));
	}

	public async Task<AgentTurnLease?> ContinueLeaseAsync(string supabaseAccessToken, string leaseId, bool asManyAsNeeded, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Post, "/agent/turn-lease/" + leaseId + "/continue", supabaseAccessToken, new JsonObject((JsonNodeOptions?)null) { ["extra_usage_approval"] = (asManyAsNeeded ? "as_many_as_needed" : "one_time") }, ct, TimeSpan.FromSeconds(15L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException("/agent/turn-lease/continue", num, body);
		}
		return AgentTurnLease.Parse(ParseObject(body));
	}

	public async Task CompleteLeaseAsync(string supabaseAccessToken, string leaseId, string leaseStatus, string? threadId, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Post, "/agent/turn-lease/" + leaseId + "/complete", supabaseAccessToken, new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = leaseStatus,
			["thread_id"] = threadId
		}, ct, TimeSpan.FromSeconds(15L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException("/agent/turn-lease/complete", num, body);
		}
	}

	public async Task<bool> RefundEarlyStopAsync(string supabaseAccessToken, string leaseId, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Post, "/agent/turn-lease/" + leaseId + "/refund-early-stop", supabaseAccessToken, new JsonObject((JsonNodeOptions?)null), ct, TimeSpan.FromSeconds(8L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException("/agent/turn-lease/refund-early-stop", num, body);
		}
		bool b = default(bool);
		return ParseObject(body)?["refunded"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out b) && b;
	}

	public async Task<string?> ThreadTitleAsync(string supabaseAccessToken, string userPrompt, CancellationToken ct, TimeSpan? timeout = null, IReadOnlyList<string>? namesInUse = null)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["user_prompt"] = userPrompt };
		if (namesInUse != null && namesInUse.Count > 0)
		{
			jsonObject["names_in_use"] = new JsonArray(namesInUse.Take(30).Select((Func<string, JsonNode>)((string name) => JsonValue.Create(name, (JsonNodeOptions?)null))).ToArray());
		}
		var (num, body) = await SendAsync(HttpMethod.Post, "/codex-thread-title", supabaseAccessToken, jsonObject, ct, timeout ?? TimeSpan.FromSeconds(10L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException("/codex-thread-title", num, body);
		}
		string text = ParseObject(body)?["title"]?.ToString()?.Trim();
		return string.IsNullOrEmpty(text) ? null : text;
	}

	public async Task<string?> TextStartCueAsync(string supabaseAccessToken, string userPrompt, bool isFreshThread, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Post, "/codex-thread-launch", supabaseAccessToken, new JsonObject((JsonNodeOptions?)null)
		{
			["user_prompt"] = userPrompt,
			["is_fresh_thread"] = isFreshThread,
			["launch_source"] = "text"
		}, ct, TimeSpan.FromSeconds(10L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			throw new AgentBackendException("/codex-thread-launch", num, body);
		}
		string text = ParseObject(body)?["text_start_cue"]?.ToString()?.Trim();
		return string.IsNullOrEmpty(text) ? null : text;
	}

	public async Task LogAgentMessageAsync(string supabaseAccessToken, string? threadId, string role, string content, bool isFollowUp, bool isDemo, long? proactiveSuggestionId, CancellationToken ct)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["role"] = role,
			["content"] = content,
			["is_follow_up"] = isFollowUp,
			["is_demo"] = isDemo
		};
		if (!string.IsNullOrWhiteSpace(threadId))
		{
			jsonObject["thread_id"] = threadId.Trim();
		}
		if (proactiveSuggestionId.HasValue)
		{
			long valueOrDefault = proactiveSuggestionId.GetValueOrDefault();
			jsonObject["is_proactive"] = true;
			jsonObject["proactive_suggestion_id"] = valueOrDefault;
		}
		var (num, body) = await SendAsync(HttpMethod.Post, "/agent-messages", supabaseAccessToken, jsonObject, ct, TimeSpan.FromSeconds(15L)).ConfigureAwait(continueOnCapturedContext: false);
		if (num != 200)
		{
			throw new AgentBackendException("/agent-messages", num, body);
		}
	}

	public async Task<(UserMemoryFetchStatus Status, string? Markdown)> UserMemoryAsync(string supabaseAccessToken, CancellationToken ct)
	{
		var (num, body) = await SendAsync(HttpMethod.Get, "/me/memory", supabaseAccessToken, null, ct, TimeSpan.FromSeconds(4L)).ConfigureAwait(continueOnCapturedContext: false);
		if ((num < 200 || num >= 300) ? true : false)
		{
			return (Status: UserMemoryFetchStatus.Failed, Markdown: null);
		}
		JsonObject jsonObject = ParseObject(body);
		if (jsonObject == null)
		{
			return (Status: UserMemoryFetchStatus.Failed, Markdown: null);
		}
		bool b = default(bool);
		bool num2 = jsonObject["has_profile"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out b) && b;
		string text = jsonObject["markdown"]?.ToString();
		return (num2 && !string.IsNullOrWhiteSpace(text)) ? (Status: UserMemoryFetchStatus.Profile, Markdown: text) : (Status: UserMemoryFetchStatus.NoProfile, Markdown: null);
	}

	private static string? PromptSafe(string? value)
	{
		if (value == null)
		{
			return null;
		}
		string text = new string(value.Where((char c) => !char.IsControl(c) && c != '<' && c != '>' && c != '[' && c != ']').ToArray()).Trim();
		if (text.Length != 0)
		{
			if (text.Length <= 80)
			{
				return text;
			}
			return text.Substring(0, 80);
		}
		return null;
	}

	public static bool IsUnauthorized(Exception ex)
	{
		if (ex is AgentBackendException ex2)
		{
			return ex2.StatusCode == 401;
		}
		return false;
	}
}
