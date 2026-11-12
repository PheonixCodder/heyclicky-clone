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
using HeyClicky.Core.Net;

namespace HeyClicky.Core.Proactive;

public sealed class AgentBackendClient
{
	private readonly ClickyHttp http;

	private readonly Func<bool, CancellationToken, Task<string?>> agentToken;

	public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60L);

	public const string FixInstruction = "Fix this remote task. Use the run error, logs, result, current source, and HeyClicky scheduled cron APIs to update the task so the next run succeeds. Show the proposed source/scope changes before deploying if a destructive or permission-expanding change is needed.";

	public const string FixLaunchContext = "The user tapped Fix the Task with HeyClicky Agent on a failed Remote Task notification, so HeyClicky is launching a Codex agent with the task source, run logs, and failure result.";

	public Func<byte[]?>? TestResponse { get; set; }

	public Action<string>? TestMarkRead { get; set; }

	public AgentBackendClient(ClickyHttp http, Func<bool, CancellationToken, Task<string?>> agentToken)
	{
		this.http = http;
		this.agentToken = agentToken;
	}

	public async Task<IReadOnlyList<AgentNotification>> FetchUnreadAsync(CancellationToken ct = default(CancellationToken))
	{
		byte[] array = TestResponse?.Invoke();
		if (array != null)
		{
			return AgentNotification.ParseList(array);
		}
		return AgentNotification.ParseList(await SendAsync(HttpMethod.Get, "/agent/notifications?limit=20", ct).ConfigureAwait(continueOnCapturedContext: false));
	}

	public Task MarkReadAsync(string id, CancellationToken ct = default(CancellationToken))
	{
		if (TestResponse != null)
		{
			TestMarkRead?.Invoke(id);
			return Task.CompletedTask;
		}
		return SendAsync(HttpMethod.Post, "/agent/notifications/" + Uri.EscapeDataString(id) + "/read", ct);
	}

	public Task MarkAllReadAsync(CancellationToken ct = default(CancellationToken))
	{
		if (TestResponse != null)
		{
			TestMarkRead?.Invoke("*");
			return Task.CompletedTask;
		}
		return SendAsync(HttpMethod.Post, "/agent/notifications/read-all", ct);
	}

	public async Task<JsonObject?> FetchCronDetailAsync(string cronId, CancellationToken ct = default(CancellationToken))
	{
		if (TestResponse != null)
		{
			return null;
		}
		try
		{
			return JsonNode.Parse(await SendAsync(HttpMethod.Get, "/agent/cron/" + Uri.EscapeDataString(cronId), ct).ConfigureAwait(continueOnCapturedContext: false), null) as JsonObject;
		}
		catch (Exception ex)
		{
			Log.Info("Notifications", "cron detail " + cronId + ": " + ex.Message);
			return null;
		}
	}

	public async Task<IReadOnlyList<AgentIntegrationInfo>> FetchIntegrationsAsync(CancellationToken ct = default(CancellationToken))
	{
		return AgentIntegrationInfo.ParseList(await SendAsync(HttpMethod.Get, "/agent/integrations?limit=1000", ct).ConfigureAwait(continueOnCapturedContext: false));
	}

	public async Task<Uri?> ConnectIntegrationAsync(string toolkitSlug, CancellationToken ct = default(CancellationToken))
	{
		string text = JsonNode.Parse(await SendAsync(HttpMethod.Post, "/agent/integrations/" + Uri.EscapeDataString(toolkitSlug) + "/connect", ct).ConfigureAwait(continueOnCapturedContext: false), null)?["redirect_url"]?.GetValue<string>();
		Uri result;
		return (text != null && Uri.TryCreate(text, UriKind.Absolute, out result)) ? result : null;
	}

	public async Task<byte[]> SendAsync(HttpMethod method, string path, CancellationToken ct)
	{
		int attempt = 0;
		while (true)
		{
			string obj = await agentToken(attempt > 0, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (obj == null)
			{
				break;
			}
			string parameter = obj;
			using (HttpRequestMessage request = new HttpRequestMessage(method, http.WorkerUrl(path)))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", parameter);
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				if (method == HttpMethod.Post)
				{
					request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"));
					request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				}
				using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
				cts.CancelAfter(TimeSpan.FromSeconds(20L));
				using HttpResponseMessage response = await http.Client.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				byte[] array = await response.Content.ReadAsByteArrayAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				int statusCode = (int)response.StatusCode;
				if (statusCode >= 200 && statusCode < 300)
				{
					return array;
				}
				if (statusCode != 401 || attempt != 0)
				{
					string message;
					try
					{
						message = JsonNode.Parse(array, null)?["error"]?.GetValue<string>() ?? $"HTTP {statusCode}";
					}
					catch (JsonException)
					{
						message = $"HTTP {statusCode}";
					}
					throw new HttpRequestException(message, null, response.StatusCode);
				}
			}
			attempt++;
		}
		throw new InvalidOperationException("Signed out.");
	}

	public static string BuildContext(AgentNotification n, JsonObject? detail)
	{
		List<string> list = new List<string> { $"Notification:\n- title: {n.Title}\n- severity: {n.Severity.ToString().ToLowerInvariant()}\n- created_at: {n.CreatedAt}\n- summary: {n.Summary}\n- source_type: {n.SourceType}\n- source_id: {n.SourceId ?? "none"}\n- run_id: {n.RunId ?? "none"}" };
		JsonNode jsonNode = detail?["cron"];
		JsonObject cron = jsonNode as JsonObject;
		if (cron != null)
		{
			list.Add($"Remote Task:\n- id: {C("id")}\n- name: {C("name")}\n- description: {C("description")}\n- enabled: {C("enabled")}\n- schedule: {C("schedule_kind")} {C("schedule_expr")} {C("timezone")}\n- next_run_at: {cron["next_run_at"]?.ToString() ?? "none"}\n- last_run_status: {cron["last_run_status"]?.ToString() ?? "none"}\n\nCurrent source:\n```ts\n{Trim(C("source"), 12000)}\n```");
		}
		if (detail?["runs"] is JsonArray { Count: >0 } jsonArray)
		{
			string text = string.Join("\n\n", from r in jsonArray.OfType<JsonObject>().Take(5)
				select $"Run {r["id"]}:\n- status: {r["status"]}\n- attempt: {r["attempt"]}\n- created_at: {r["created_at"]?.ToString() ?? "none"}\n- started_at: {r["started_at"]?.ToString() ?? "none"}\n- finished_at: {r["finished_at"]?.ToString() ?? "none"}\n- duration_ms: {r["duration_ms"]?.ToString() ?? "none"}\n- result_summary: {r["result_summary"]}\n- error: {Trim(r["error"]?.ToString(), 2000)}\n- log:\n{Trim(r["stdout_log"]?.ToString(), 5000)}");
			list.Add("Recent runs:\n" + text);
		}
		return string.Join("\n\n", list);
		string C(string k)
		{
			return cron[k]?.ToString() ?? "";
		}
		static string Trim(string? value, int limit)
		{
			if (value != null)
			{
				if (value.Length > limit)
				{
					return value.Substring(0, limit) + "\n...[truncated]";
				}
				return value;
			}
			return "";
		}
	}

	public static string ComposePrompt(string? context, string instruction)
	{
		string text = instruction.Trim();
		if (string.IsNullOrWhiteSpace(context))
		{
			return text;
		}
		return $"HeyClicky Agent remote task modification request.\n\n{context}\n\nUser requested change:\n{text}\n\nUse the clicky-crons tools to inspect, edit, run, or fix this Remote Task.";
	}
}
