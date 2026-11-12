using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents.Harness;
using HeyClicky.Agents.Jobs;
using HeyClicky.Agents.Turns;

namespace HeyClicky.Agents.Runtime;

internal sealed class CodexSession
{
	private readonly ConcurrentDictionary<string, AgentTask> activeByThread = new ConcurrentDictionary<string, AgentTask>();

	private readonly ConcurrentDictionary<string, byte> finishedTurnIds = new ConcurrentDictionary<string, byte>();

	public Action<AgentTask, AgentTaskEventKind, string?>? OnTaskEvent;

	public Action<AgentTask, TurnOutcome, string?>? OnTurnEnded;

	public Action<AgentTask, string>? OnIntegrationUsed;

	public Action<AgentTask, IReadOnlyList<AgentPlanStep>>? OnTaskPlanUpdated;

	public Action<AgentTask, string>? OnTaskToolStep;

	public Action<AgentTask, ClickyProgramBatch>? OnTaskProgramBatch;

	public Action<AgentTask, string, string, string>? OnTaskFilePatchStreaming;

	public Action<AgentTask, IReadOnlyList<string>>? OnTaskFileChangeCompleted;

	public const string HarnessTurnIdPrefix = "harness-turn-";

	private readonly ConcurrentDictionary<string, string> harnessThreadIdByAppThreadId = new ConcurrentDictionary<string, string>();

	public Action<AgentHarness, AgentHarnessPlace, string, double, IReadOnlyDictionary<string, double>>? OnHarnessTurnEnded;

	public Action<string, string, string>? OnHarnessInternalError;

	public Action? OnHarnessConnectedAppsUnreachable;

	public int ActiveTurnCount => activeByThread.Count;

	public bool IsThreadActive(string threadId)
	{
		return activeByThread.ContainsKey(threadId);
	}

	public bool Interrupt(AgentTask task, TurnOutcome outcome, string summary)
	{
		string threadId;
		lock (task.Gate)
		{
			threadId = task.ThreadId;
		}
		if (threadId == null || !activeByThread.TryGetValue(threadId, out AgentTask value) || value != task)
		{
			return false;
		}
		InterruptHarnessTurn(threadId);
		EndTurn(task, outcome, summary);
		return true;
	}

	private void EndTurn(AgentTask task, TurnOutcome outcome, string? message)
	{
		string threadId;
		string turnId;
		lock (task.Gate)
		{
			threadId = task.ThreadId;
			turnId = task.TurnId;
		}
		if (threadId != null)
		{
			activeByThread.TryRemove(new KeyValuePair<string, AgentTask>(threadId, task));
		}
		if (turnId != null)
		{
			RememberFinishedTurn(turnId);
		}
		OnTurnEnded?.Invoke(task, outcome, message);
	}

	private void RememberFinishedTurn(string turnId)
	{
		finishedTurnIds[turnId] = 0;
		if (finishedTurnIds.Count <= 256)
		{
			return;
		}
		foreach (string item in finishedTurnIds.Keys.Take(64))
		{
			finishedTurnIds.TryRemove(item, out var _);
		}
	}

	private void HandleNotification(string method, JsonObject? p)
	{
		string text = p?["threadId"]?.ToString();
		AgentTask value;
		AgentTask agentTask = ((text != null && activeByThread.TryGetValue(text, out value)) ? value : null);
		string text2 = p?["turnId"]?.ToString() ?? p?["turn"]?["id"]?.ToString();
		if (text2 != null)
		{
			if (finishedTurnIds.ContainsKey(text2))
			{
				return;
			}
			if (agentTask != null)
			{
				string turnId;
				lock (agentTask.Gate)
				{
					turnId = agentTask.TurnId;
				}
				if (turnId != null && turnId != text2)
				{
					return;
				}
			}
		}
		if (method == null)
		{
			return;
		}
		switch (method.Length)
		{
		case 12:
			switch (method[0])
			{
			case 't':
				if (!(method == "turn/started") || agentTask == null)
				{
					break;
				}
				lock (agentTask.Gate)
				{
					string text13 = p?["turn"]?["id"]?.ToString();
					if (text13 != null)
					{
						agentTask.TurnId = text13;
					}
					agentTask.ProgressSummary = "Thinking…";
				}
				OnTaskEvent?.Invoke(agentTask, AgentTaskEventKind.ProgressChanged, "Thinking…");
				break;
			case 'i':
				if (method == "item/started" && agentTask != null && p?["item"] is JsonObject item)
				{
					OnItemStarted(agentTask, item);
				}
				break;
			}
			break;
		case 14:
			switch (method[0])
			{
			case 'i':
				if (method == "item/completed" && agentTask != null && p?["item"] is JsonObject item2)
				{
					OnItemCompleted(agentTask, item2);
				}
				break;
			case 't':
			{
				if (!(method == "turn/completed"))
				{
					break;
				}
				if (text2 != null)
				{
					RememberFinishedTurn(text2);
				}
				if (agentTask == null)
				{
					break;
				}
				string text14 = p?["turn"]?["status"]?.ToString() ?? "completed";
				JsonNode jsonNode = (p?["turn"] as JsonObject)?["error"];
				string text15 = ((!(jsonNode is JsonObject jsonObject3)) ? jsonNode?.ToString() : (jsonObject3["message"]?.ToString() ?? jsonObject3.ToJsonString()));
				lock (agentTask.Gate)
				{
					string text16 = p?["turn"]?["id"]?.ToString();
					if (text16 != null)
					{
						agentTask.TurnId = text16;
					}
				}
				if (!(text14 == "interrupted"))
				{
					if (text14 == "failed")
					{
						EndTurn(agentTask, (!IsExtraEffortError(text15)) ? TurnOutcome.Failed : TurnOutcome.ExtraEffortRequired, text15 ?? "Agent thread failed.");
					}
					else
					{
						EndTurn(agentTask, TurnOutcome.Completed, null);
					}
					break;
				}
				bool requiresExtraUsageApproval;
				lock (agentTask.Gate)
				{
					requiresExtraUsageApproval = agentTask.RequiresExtraUsageApproval;
					if (!requiresExtraUsageApproval && !IsExtraEffortError(text15) && agentTask.IsActive)
					{
						agentTask.InterruptedUnexpectedly = true;
					}
				}
				EndTurn(agentTask, (requiresExtraUsageApproval || IsExtraEffortError(text15)) ? TurnOutcome.ExtraEffortRequired : TurnOutcome.Interrupted, text15 ?? "Agent thread was interrupted.");
				break;
			}
			}
			break;
		case 28:
			if (!(method == "item/fileChange/patchUpdated") || agentTask == null || !(p?["changes"] is JsonArray source))
			{
				break;
			}
			{
				foreach (JsonObject item3 in source.OfType<JsonObject>())
				{
					string text11 = item3["path"]?.ToString();
					if (text11 != null)
					{
						string text12 = item3["diff"]?.ToString();
						if (text12 != null)
						{
							string arg = (item3["kind"] as JsonObject)?["type"]?.ToString() ?? "update";
							OnTaskFilePatchStreaming?.Invoke(agentTask, text11, arg, text12);
						}
					}
				}
				break;
			}
		case 17:
		{
			if (!(method == "turn/plan/updated") || agentTask == null)
			{
				break;
			}
			List<AgentPlanStep> list = (from s in ((p?["plan"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
				select new AgentPlanStep(s["step"]?.ToString()?.Trim() ?? "", s["status"]?.ToString() ?? "pending") into s
				where s.Text.Length > 0
				select s).ToList();
			if (list.Count != 0)
			{
				lock (agentTask.Gate)
				{
					agentTask.Plan = list;
				}
				OnTaskPlanUpdated?.Invoke(agentTask, list);
			}
			break;
		}
		case 23:
		{
			if (!(method == "item/agentMessage/delta") || agentTask == null)
			{
				break;
			}
			string text8 = p?["delta"]?.ToString();
			if (text8 == null)
			{
				break;
			}
			bool flag2;
			lock (agentTask.Gate)
			{
				flag2 = agentTask.CurrentMessagePhase == "commentary";
				if (flag2)
				{
					agentTask.StreamingCommentary.Append(text8);
				}
				else
				{
					agentTask.ResponseAccumulator.Append(text8);
				}
				if (agentTask.FirstTokenTimestamp == 0L)
				{
					agentTask.FirstTokenTimestamp = Stopwatch.GetTimestamp();
				}
			}
			OnTaskEvent?.Invoke(agentTask, flag2 ? AgentTaskEventKind.CommentaryDelta : AgentTaskEventKind.ResponseDelta, text8);
			break;
		}
		case 31:
		{
			if (!(method == "item/reasoning/summaryTextDelta") || agentTask == null)
			{
				break;
			}
			string text6 = p?["delta"]?.ToString();
			if (text6 == null)
			{
				break;
			}
			string text7;
			lock (agentTask.Gate)
			{
				int value3;
				int num = ((p["summaryIndex"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<int>(out value3)) ? value3 : 0);
				if (agentTask.ReasoningSummaryIndex != num)
				{
					agentTask.ReasoningSummaryText.Clear();
				}
				agentTask.ReasoningSummaryIndex = num;
				agentTask.ReasoningSummaryText.Append(text6);
				if (agentTask.FirstTokenTimestamp == 0L)
				{
					agentTask.FirstTokenTimestamp = Stopwatch.GetTimestamp();
				}
				text7 = ReasoningHeading(agentTask.ReasoningSummaryText.ToString());
				if (text7 != null && text7 != agentTask.ThinkingHeading)
				{
					agentTask.ThinkingHeading = text7;
				}
				else
				{
					text7 = null;
				}
			}
			if (text7 != null)
			{
				OnTaskEvent?.Invoke(agentTask, AgentTaskEventKind.ThinkingHeading, text7);
			}
			break;
		}
		case 33:
		{
			if (!(method == "item/commandExecution/outputDelta") || agentTask == null)
			{
				break;
			}
			string text9 = p?["delta"]?.ToString();
			if (text9 == null)
			{
				break;
			}
			string text10 = string.Join('\n', text9.Split('\n').Take(3));
			if (text10.Trim().Length <= 0)
			{
				break;
			}
			lock (agentTask.Gate)
			{
				agentTask.AddActivity(AgentActivityKind.ShellOutput, (text10.Length > 200) ? text10.Substring(0, 200) : text10);
				break;
			}
		}
		case 27:
		{
			if (!(method == "item/fileChange/outputDelta") || agentTask == null)
			{
				break;
			}
			string text17 = p?["delta"]?.ToString();
			if (text17 == null)
			{
				break;
			}
			lock (agentTask.Gate)
			{
				agentTask.AddActivity(AgentActivityKind.FileChange, (text17.Length > 150) ? text17.Substring(0, 150) : text17);
				break;
			}
		}
		case 25:
			if (!(method == "thread/tokenUsage/updated") || agentTask == null || !(p?["tokenUsage"]?["total"] is JsonObject jsonObject2))
			{
				break;
			}
			lock (agentTask.Gate)
			{
				agentTask.InputTokens = Long(jsonObject2["inputTokens"]);
				agentTask.CachedInputTokens = Long(jsonObject2["cachedInputTokens"]);
				agentTask.OutputTokens = Long(jsonObject2["outputTokens"]);
				break;
			}
		case 5:
		{
			if (!(method == "error") || !(p?["error"] is JsonObject jsonObject))
			{
				break;
			}
			string text3 = jsonObject["message"]?.ToString() ?? "Unknown error";
			bool value2 = default(bool);
			bool flag = p["willRetry"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value2) && value2;
			Log.Warn("Codex", $"error notification thread={text} willRetry={flag}: {Truncate(text3, 500)}");
			if (agentTask == null)
			{
				break;
			}
			if (IsExtraEffortError(text3))
			{
				lock (agentTask.Gate)
				{
					agentTask.AddActivity(AgentActivityKind.Commentary, "This task requires extra effort");
				}
				Interrupt(agentTask, TurnOutcome.ExtraEffortRequired, "This task requires extra effort");
			}
			else if (flag)
			{
				string text4 = RetrySummary(text3);
				lock (agentTask.Gate)
				{
					agentTask.AddActivity(AgentActivityKind.Commentary, text4);
					agentTask.ProgressSummary = text4;
				}
				OnTaskEvent?.Invoke(agentTask, AgentTaskEventKind.ProgressChanged, text4);
			}
			else
			{
				string text5 = UserFacingError(text3);
				lock (agentTask.Gate)
				{
					agentTask.AddActivity(AgentActivityKind.Error, text5);
				}
				EndTurn(agentTask, TurnOutcome.Failed, text5);
			}
			break;
		}
		}
	}

	private void OnItemStarted(AgentTask task, JsonObject item)
	{
		string text = item["type"]?.ToString() ?? "unknown";
		string text2 = item["id"]?.ToString();
		Log.Info("Codex", $"item started: {text} {item["phase"]} {((text == "commandExecution") ? Truncate(item["command"]?.ToJsonString() ?? "", 400) : "")}");
		string text3 = PublicToolActivityText(item);
		string text4 = null;
		string text5;
		lock (task.Gate)
		{
			text5 = task.FinishStreamingCommentary(task.CurrentNarrationItemId);
			task.CurrentNarrationItemId = null;
			if (text3 != null)
			{
				task.RecordToolActivity(text2, text3, isRunning: true);
				text4 = text3;
			}
			switch (text)
			{
			case "reasoning":
				task.CurrentNarrationItemId = text2;
				task.ReasoningSummaryIndex = null;
				task.ReasoningSummaryText.Clear();
				text4 = "Thinking…";
				if (task.Activity.Count != 0)
				{
					List<ActivityRow> activity = task.Activity;
					if (activity[activity.Count - 1].Kind == AgentActivityKind.Thinking)
					{
						break;
					}
				}
				task.AddActivity(AgentActivityKind.Thinking, "Reasoning...");
				break;
			case "commandExecution":
			{
				string text7 = ShellCommandDisplay(item["command"]);
				task.RecordToolActivity(text2, text7, isRunning: true);
				text4 = "Running command: " + text7;
				text3 = text7;
				break;
			}
			case "agentMessage":
			{
				string text6 = item["phase"]?.ToString() ?? "final_answer";
				if (text6 == "commentary")
				{
					task.CurrentNarrationItemId = text2;
					text4 = "Sharing an update…";
				}
				else
				{
					if (task.ResponseAccumulator.Length > 0)
					{
						task.CompletedReplies.Add(task.ResponseAccumulator.ToString());
					}
					task.ResponseAccumulator.Clear();
					text4 = "Writing…";
				}
				task.CurrentMessagePhase = text6;
				break;
			}
			}
			if (text4 != null)
			{
				task.ProgressSummary = text4;
			}
		}
		if (text5 != null)
		{
			OnTaskEvent?.Invoke(task, AgentTaskEventKind.Commentary, text5);
		}
		if (text3 != null)
		{
			OnTaskEvent?.Invoke(task, AgentTaskEventKind.ToolActivity, text3);
		}
		string text8 = PublicToolActivityText(item);
		if (text8 != null)
		{
			OnTaskToolStep?.Invoke(task, text8);
		}
		if (text4 != null)
		{
			OnTaskEvent?.Invoke(task, AgentTaskEventKind.ProgressChanged, text4);
		}
		string text9 = IntegrationUse.ProviderKey(item);
		if (text9 != null)
		{
			OnIntegrationUsed?.Invoke(task, text9);
		}
	}

	private void OnItemCompleted(AgentTask task, JsonObject item)
	{
		string text = item["type"]?.ToString() ?? "unknown";
		string itemId = item["id"]?.ToString();
		string text2 = null;
		string text3 = null;
		string text4 = PublicToolActivityText(item);
		if (text == "fileChange" && item["changes"] is JsonArray source)
		{
			List<string> list = (from c in source.OfType<JsonObject>()
				select c["path"]?.ToString()).OfType<string>().ToList();
			if (list != null && list.Count > 0)
			{
				OnTaskFileChangeCompleted?.Invoke(task, list);
			}
		}
		if (text4 != null)
		{
			OnTaskToolStep?.Invoke(task, text4);
		}
		lock (task.Gate)
		{
			if (text4 != null)
			{
				string text5 = text4;
				task.RecordToolActivity(itemId, text5, isRunning: false);
				task.ProgressSummary = "Working on it…";
			}
			else if (text == "commandExecution")
			{
				task.RecordToolActivity(itemId, ShellCommandDisplay(item["command"]), isRunning: false);
				task.ProgressSummary = "Working on it…";
			}
			if (text == "agentMessage" && item["phase"]?.ToString() == "commentary")
			{
				text2 = task.FinishStreamingCommentary(itemId, ItemText(item));
			}
			else
			{
				if (!(text == "agentMessage") || task.ResponseAccumulator.Length != 0)
				{
					goto IL_01b8;
				}
				string text6 = ItemText(item);
				if (text6 == null)
				{
					goto IL_01b8;
				}
				task.ResponseAccumulator.Append(text6);
			}
			goto end_IL_00e4;
			IL_01b8:
			if (text == "reasoning")
			{
				string text7 = ((item["summary"] as JsonArray)?.Select((JsonNode n) => (!(n is JsonObject jsonObject)) ? n?.ToString() : jsonObject["text"]?.ToString()).OfType<string>().ToList())?.LastOrDefault();
				if (text7 != null)
				{
					string text8 = ReasoningHeading(text7);
					if (text8 != null && text8 != task.ThinkingHeading)
					{
						text3 = (task.ThinkingHeading = text8);
					}
				}
			}
			end_IL_00e4:;
		}
		if (text2 != null)
		{
			OnTaskEvent?.Invoke(task, AgentTaskEventKind.Commentary, text2);
		}
		if (text3 != null)
		{
			OnTaskEvent?.Invoke(task, AgentTaskEventKind.ThinkingHeading, text3);
		}
	}

	internal static string? ItemText(JsonObject item)
	{
		string[] array = new string[2] { "text", "message" };
		foreach (string propertyName in array)
		{
			JsonNode? jsonNode = item[propertyName];
			if (jsonNode != null && jsonNode.GetValueKind() == JsonValueKind.String && item[propertyName].ToString().Trim().Length > 0)
			{
				return item[propertyName].ToString();
			}
		}
		if (item["content"] is JsonArray source)
		{
			string text = string.Join("\n", (from o in source.OfType<JsonObject>()
				select o["text"]?.ToString()).OfType<string>());
			if (text.Trim().Length > 0)
			{
				return text;
			}
		}
		return null;
	}

	internal static string? PublicToolActivityText(JsonObject item)
	{
		switch (item["type"]?.ToString())
		{
		case "mcpToolCall":
		case "mcp_tool_call":
		case "dynamicToolCall":
			return "Using: " + (item["tool"]?.ToString() ?? "tool").Replace('_', ' ');
		case "webSearch":
		{
			JsonObject jsonObject = item["action"] as JsonObject;
			string text = jsonObject?["type"]?.ToString();
			if (text == "openPage")
			{
				string text2 = jsonObject?["url"]?.ToString();
				Uri result;
				if (text2 != null)
				{
					return "Reading: " + (Uri.TryCreate(text2, UriKind.Absolute, out result) ? result.Host : text2);
				}
			}
			if (text == "findInPage")
			{
				string text3 = jsonObject?["pattern"]?.ToString();
				if (text3 != null)
				{
					return "Finding: " + text3;
				}
			}
			string text4 = item["query"]?.ToString() ?? jsonObject?["query"]?.ToString() ?? "";
			return "Search: " + ((text4.Length == 0) ? "the web" : text4);
		}
		case "fileChange":
		{
			List<string> list = (from c in (item["changes"] as JsonArray)?.OfType<JsonObject>()
				select c["path"]?.ToString()).OfType<string>().Select(Path.GetFileName).ToList() ?? new List<string>();
			return "Editing: " + ((list.Count == 0) ? "files" : string.Join(", ", list));
		}
		default:
			return null;
		}
	}

	internal static string ShellCommandDisplay(JsonNode? commandNode)
	{
		string text = ((commandNode is JsonArray source) ? string.Join(' ', source.Select((JsonNode n) => n?.ToString())) : ((commandNode != null) ? commandNode.ToString() : "running command"));
		string text2 = text;
		string[] array = new string[3] { " -Command ", " -lc ", " -c " };
		foreach (string text3 in array)
		{
			int num = text2.IndexOf(text3, StringComparison.OrdinalIgnoreCase);
			if (num >= 0 && num < 160)
			{
				text = text2;
				int num2 = num + text3.Length;
				text2 = FirstPosixWord(text.Substring(num2, text.Length - num2).TrimStart());
				break;
			}
		}
		if (text2.Length <= 300)
		{
			return text2;
		}
		return text2.Substring(0, 300) + "…";
	}

	private static string FirstPosixWord(string s)
	{
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		int num = 0;
		while (num < s.Length && !char.IsWhiteSpace(s[num]))
		{
			char c = s[num];
			if (c == '\'')
			{
				int num2 = s.IndexOf('\'', num + 1);
				if (num2 < 0)
				{
					stringBuilder.Append(s, num + 1, s.Length - num - 1);
					break;
				}
				stringBuilder.Append(s, num + 1, num2 - num - 1);
				num = num2 + 1;
				continue;
			}
			switch (c)
			{
			case '"':
				num++;
				while (num < s.Length && s[num] != '"')
				{
					bool flag = s[num] == '\\' && num + 1 < s.Length;
					if (flag)
					{
						bool flag2;
						switch (s[num + 1])
						{
						case '"':
						case '$':
						case '\\':
						case '`':
							flag2 = true;
							break;
						default:
							flag2 = false;
							break;
						}
						flag = flag2;
					}
					if (flag)
					{
						num++;
					}
					stringBuilder.Append(s[num++]);
				}
				num++;
				continue;
			case '\\':
				if (num + 1 < s.Length)
				{
					stringBuilder.Append(s[num + 1]);
					num += 2;
					continue;
				}
				break;
			}
			stringBuilder.Append(c);
			num++;
		}
		if (stringBuilder.Length != 0)
		{
			return stringBuilder.ToString();
		}
		return s;
	}

	internal static string? ReasoningHeading(string summary)
	{
		string text = summary.Trim();
		if (!text.StartsWith("**", StringComparison.Ordinal))
		{
			return null;
		}
		int num = text.IndexOf("**", 2, StringComparison.Ordinal);
		if (num < 0)
		{
			return null;
		}
		string text2 = text.Substring(2, num - 2).Trim();
		int length = text2.Length;
		if (length <= 0 || length > 80 || text2.Contains('\n'))
		{
			return null;
		}
		return text2;
	}

	internal static bool IsExtraEffortError(string? text)
	{
		if (text != null)
		{
			if (!text.Contains("agent_extra_effort_required", StringComparison.OrdinalIgnoreCase) && !text.Contains("agent_turn_limit_exceeded", StringComparison.OrdinalIgnoreCase))
			{
				return text.Contains("this task requires extra effort", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	private static string RetrySummary(string raw)
	{
		if (!raw.Contains("stream disconnected", StringComparison.OrdinalIgnoreCase) && !raw.Contains("reconnect", StringComparison.OrdinalIgnoreCase))
		{
			return "Retrying after a hiccup…";
		}
		return "Reconnecting to the model…";
	}

	internal static string UserFacingError(string raw)
	{
		int num = raw.IndexOf('{');
		if (num >= 0)
		{
			try
			{
				int num2 = num;
				JsonObject jsonObject = JsonNode.Parse(raw.Substring(num2, raw.Length - num2), null) as JsonObject;
				string text = jsonObject?["message"]?.ToString() ?? jsonObject?["error"]?["message"]?.ToString() ?? jsonObject?["error"]?.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
			catch (JsonException)
			{
			}
		}
		if (raw.Length <= 500)
		{
			return raw;
		}
		return raw.Substring(0, 500);
	}

	private static long Long(JsonNode? n)
	{
		if (!(n is JsonValue jsonValue) || !jsonValue.TryGetValue<long>(out var value))
		{
			return 0L;
		}
		return value;
	}

	private static string Truncate(string s, int n)
	{
		if (s.Length <= n)
		{
			return s;
		}
		return s.Substring(0, n) + "…";
	}

	public void StartHarnessTurn((AgentHarness Harness, AgentHarnessPlace Place) choice, AgentTask task, string appThreadId, bool isNewAppThread, string userMessage, IReadOnlyList<string> imagePaths, string workingDirectory, IReadOnlyDictionary<string, string>? leaseMetadata, Func<string, AgentTask, string, CancellationToken, Task<AgentHarnessTurnContext>> contextProvider, CancellationToken ct)
	{
		IAgentHarnessRuntime runtime = AgentHarnessRuntimes.Runtime(choice.Harness, choice.Place) ?? throw new AgentHarnessException(choice.Harness.DisplayName() + " " + choice.Place.DisplayName().ToLowerInvariant() + " isn't available in this build.", notAvailable: true);
		if (!activeByThread.TryAdd(appThreadId, task) && activeByThread[appThreadId] != task)
		{
			throw new InvalidOperationException("This job is still working on its current task.");
		}
		string turnId = "harness-turn-" + Guid.NewGuid().ToString("D").ToLowerInvariant();
		lock (task.Gate)
		{
			task.TurnId = turnId;
			task.ProgressSummary = "Starting " + choice.Harness.DisplayName() + "…";
		}
		OnTaskEvent?.Invoke(task, AgentTaskEventKind.ProgressChanged, "Starting " + choice.Harness.DisplayName() + "…");
		string harnessThreadId = AgentHarnessThreads.HarnessThreadId(appThreadId, choice.Harness, choice.Place);
		harnessThreadIdByAppThreadId[appThreadId] = harnessThreadId;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 4);
		defaultInterpolatedStringHandler.AppendLiteral("[harness] ");
		defaultInterpolatedStringHandler.AppendFormatted(choice.Harness.DisplayName());
		defaultInterpolatedStringHandler.AppendLiteral(" ");
		defaultInterpolatedStringHandler.AppendFormatted(choice.Place.RawValue());
		defaultInterpolatedStringHandler.AppendLiteral(" turn on ");
		defaultInterpolatedStringHandler.AppendFormatted(harnessThreadId);
		defaultInterpolatedStringHandler.AppendLiteral(" (app thread ");
		string text = appThreadId;
		int num = Math.Min(8, appThreadId.Length);
		int length = text.Length;
		int num2 = length - num;
		defaultInterpolatedStringHandler.AppendFormatted(text.Substring(num2, length - num2));
		defaultInterpolatedStringHandler.AppendLiteral(")");
		Log.Info("Harness", defaultInterpolatedStringHandler.ToStringAndClear());
		Note("turn/started", new JsonObject((JsonNodeOptions?)null) { ["turn"] = new JsonObject((JsonNodeOptions?)null) { ["id"] = turnId } });
		string answerItemId = null;
		StringBuilder answerText = new StringBuilder();
		bool finished = false;
		bool runnerDidNotStart = false;
		IReadOnlyDictionary<string, double> turnStats = new Dictionary<string, double>();
		Stopwatch turnStartedAt = Stopwatch.StartNew();
		object emitGate = new object();
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				AgentHarnessTurnContext agentHarnessTurnContext = await contextProvider(appThreadId, task, workingDirectory, ct).ConfigureAwait(continueOnCapturedContext: false);
				AgentHarnessTurn turn = new AgentHarnessTurn
				{
					HarnessThreadId = harnessThreadId,
					AppThreadId = appThreadId,
					IsFirstTurn = isNewAppThread,
					Text = userMessage,
					ImagePaths = imagePaths,
					Workspace = workingDirectory,
					Model = agentHarnessTurnContext.Model,
					Context = agentHarnessTurnContext,
					LeaseMetadata = leaseMetadata,
					TurnId = turnId
				};
				await runtime.RunTurnAsync(turn, Emit, ct).ConfigureAwait(continueOnCapturedContext: false);
				Emit(new AgentHarnessEvent.Finished("completed", null));
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				Emit(new AgentHarnessEvent.Finished("interrupted", null));
			}
			catch (AgentHarnessException ex2) when (ex2.IsRunnerStartFailure)
			{
				lock (emitGate)
				{
					runnerDidNotStart = true;
				}
				Emit(new AgentHarnessEvent.Finished("failed", ex2.Message));
			}
			catch (Exception error)
			{
				lock (task.Gate)
				{
					task.FailureDiagnostic = AgentFailureDescription.Diagnostic(error);
				}
				Emit(new AgentHarnessEvent.Finished("failed", AgentFailureDescription.UserMessage(error)));
			}
		}, CancellationToken.None);
		void Emit(AgentHarnessEvent e)
		{
			lock (emitGate)
			{
				AgentHarnessEvent.WebSearch webSearch;
				if (!finished)
				{
					if (!(e is AgentHarnessEvent.TurnStats turnStats2))
					{
						AgentTask value3;
						if (!(e is AgentHarnessEvent.NotePreview notePreview))
						{
							AgentTask value2;
							if (!(e is AgentHarnessEvent.NoteSaved noteSaved))
							{
								AgentTask value;
								if (!(e is AgentHarnessEvent.ProgramBatch programBatch))
								{
									if (!(e is AgentHarnessEvent.ConnectedAppsUnreachable))
									{
										if (!(e is AgentHarnessEvent.Compacted))
										{
											if (!(e is AgentHarnessEvent.Commentary commentary))
											{
												if (!(e is AgentHarnessEvent.Thinking thinking))
												{
													if (!(e is AgentHarnessEvent.Plan plan))
													{
														if (!(e is AgentHarnessEvent.ToolStarted toolStarted))
														{
															if (!(e is AgentHarnessEvent.ToolFinished toolFinished))
															{
																webSearch = e as AgentHarnessEvent.WebSearch;
																if ((object)webSearch == null)
																{
																	if (!(e is AgentHarnessEvent.AnswerDelta answerDelta))
																	{
																		if (e is AgentHarnessEvent.Finished finished2)
																		{
																			finished = true;
																			if (answerItemId != null)
																			{
																				Note("item/completed", new JsonObject((JsonNodeOptions?)null) { ["item"] = new JsonObject((JsonNodeOptions?)null)
																				{
																					["type"] = "agentMessage",
																					["phase"] = "final_answer",
																					["id"] = answerItemId,
																					["text"] = answerText.ToString()
																				} });
																			}
																			bool flag = finished2.Status == "failed" && (finished2.Error ?? "").Contains("agent_extra_effort_required", StringComparison.OrdinalIgnoreCase);
																			try
																			{
																				OnHarnessTurnEnded?.Invoke(choice.Harness, choice.Place, flag ? "out_of_budget" : finished2.Status, turnStartedAt.Elapsed.TotalSeconds, turnStats);
																				if (finished2.Status == "failed" && !flag && !runnerDidNotStart)
																				{
																					string failureDiagnostic;
																					lock (task.Gate)
																					{
																						failureDiagnostic = task.FailureDiagnostic;
																					}
																					OnHarnessInternalError?.Invoke("turn_failed", $"{choice.Harness.RawValue()} {choice.Place.RawValue()}: {Cap(failureDiagnostic ?? finished2.Error ?? "no reason", 500)}", choice.Harness.RawValue() + "_" + choice.Place.RawValue());
																				}
																			}
																			catch (Exception ex3)
																			{
																				Log.Warn("Harness", "turn-ended handler threw: " + ex3.Message);
																			}
																			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
																			{
																				["id"] = turnId,
																				["status"] = (flag ? "interrupted" : finished2.Status)
																			};
																			string error2 = finished2.Error;
																			if (error2 != null)
																			{
																				jsonObject["error"] = new JsonObject((JsonNodeOptions?)null) { ["message"] = error2 };
																			}
																			Log.Info("Harness", $"[harness] turn on {harnessThreadId} ended: {finished2.Status}{((finished2.Error == null) ? "" : (" (" + Cap(finished2.Error, 300) + ")"))}");
																			Note("turn/completed", new JsonObject((JsonNodeOptions?)null) { ["turn"] = jsonObject });
																		}
																	}
																	else
																	{
																		if (answerItemId == null)
																		{
																			answerItemId = "harness-answer-" + Guid.NewGuid().ToString("D").ToLowerInvariant();
																			Note("item/started", new JsonObject((JsonNodeOptions?)null) { ["item"] = new JsonObject((JsonNodeOptions?)null)
																			{
																				["type"] = "agentMessage",
																				["phase"] = "final_answer",
																				["id"] = answerItemId
																			} });
																		}
																		answerText.Append(answerDelta.Text);
																		Note("item/agentMessage/delta", new JsonObject((JsonNodeOptions?)null)
																		{
																			["delta"] = answerDelta.Text,
																			["itemId"] = answerItemId
																		});
																	}
																}
																else
																{
																	Note("item/started", new JsonObject((JsonNodeOptions?)null) { ["item"] = SearchItem() });
																	Note("item/completed", new JsonObject((JsonNodeOptions?)null) { ["item"] = SearchItem() });
																}
															}
															else
															{
																Note("item/completed", new JsonObject((JsonNodeOptions?)null) { ["item"] = HarnessToolItem(toolFinished.Id, toolFinished.Tool, toolFinished.Server, toolFinished.Command) });
															}
														}
														else
														{
															Note("item/started", new JsonObject((JsonNodeOptions?)null) { ["item"] = HarnessToolItem(toolStarted.Id, toolStarted.Tool, toolStarted.Server, toolStarted.Command) });
														}
													}
													else
													{
														Note("turn/plan/updated", new JsonObject((JsonNodeOptions?)null) { ["plan"] = new JsonArray(plan.Steps.Select<(string, string), JsonNode>(((string Text, string Status) s) => new JsonObject((JsonNodeOptions?)null)
														{
															["step"] = s.Text,
															["status"] = ((s.Status == "in_progress") ? "inProgress" : s.Status)
														}).ToArray()) });
													}
												}
												else
												{
													JsonObject jsonObject2 = new JsonObject((JsonNodeOptions?)null);
													JsonObject obj = new JsonObject((JsonNodeOptions?)null)
													{
														["type"] = "reasoning",
														["id"] = "harness-thinking-" + Guid.NewGuid().ToString("D").ToLowerInvariant()
													};
													JsonNode reference = "**" + thinking.Heading + "**";
													obj["summary"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
													jsonObject2["item"] = obj;
													Note("item/completed", jsonObject2);
												}
											}
											else
											{
												string text2 = "harness-commentary-" + Guid.NewGuid().ToString("D").ToLowerInvariant();
												Note("item/started", new JsonObject((JsonNodeOptions?)null) { ["item"] = new JsonObject((JsonNodeOptions?)null)
												{
													["type"] = "agentMessage",
													["phase"] = "commentary",
													["id"] = text2
												} });
												Note("item/agentMessage/delta", new JsonObject((JsonNodeOptions?)null)
												{
													["delta"] = commentary.Text,
													["itemId"] = text2
												});
												Note("item/completed", new JsonObject((JsonNodeOptions?)null) { ["item"] = new JsonObject((JsonNodeOptions?)null)
												{
													["type"] = "agentMessage",
													["phase"] = "commentary",
													["id"] = text2,
													["text"] = commentary.Text
												} });
											}
										}
										else
										{
											StandingContextLedger.ForgetThread(appThreadId);
										}
									}
									else
									{
										OnHarnessConnectedAppsUnreachable?.Invoke();
									}
								}
								else if (activeByThread.TryGetValue(appThreadId, out value) && value == task)
								{
									OnTaskProgramBatch?.Invoke(task, programBatch.Batch);
								}
							}
							else if (activeByThread.TryGetValue(appThreadId, out value2) && value2 == task)
							{
								OnTaskFileChangeCompleted?.Invoke(task, new _003C_003Ez__ReadOnlySingleElementList<string>(noteSaved.Path));
							}
						}
						else if (activeByThread.TryGetValue(appThreadId, out value3) && value3 == task)
						{
							OnTaskFilePatchStreaming?.Invoke(task, notePreview.Path, "add", notePreview.Text);
						}
					}
					else
					{
						turnStats = turnStats2.Stats;
					}
				}
				JsonObject SearchItem()
				{
					JsonObject jsonObject3 = new JsonObject((JsonNodeOptions?)null)
					{
						["type"] = "webSearch",
						["id"] = webSearch.Id,
						["query"] = webSearch.Query
					};
					string url = webSearch.Url;
					if (url != null)
					{
						jsonObject3["action"] = new JsonObject((JsonNodeOptions?)null)
						{
							["type"] = "openPage",
							["url"] = url
						};
					}
					return jsonObject3;
				}
			}
		}
		void Note(string method, JsonObject p)
		{
			p["threadId"] = appThreadId;
			p["turnId"] = turnId;
			HandleNotification(method, p);
		}
	}

	public bool IsHarnessTurnRunning(string? threadId)
	{
		if (threadId == null || !activeByThread.TryGetValue(threadId, out AgentTask value))
		{
			return false;
		}
		lock (value.Gate)
		{
			return value.TurnId?.StartsWith("harness-turn-", StringComparison.Ordinal) ?? false;
		}
	}

	public async Task<bool> SteerHarnessTurnAsync(string appThreadId, string text, CancellationToken ct, bool informational = false, IReadOnlyList<string>? imagePaths = null)
	{
		if (harnessThreadIdByAppThreadId.TryGetValue(appThreadId, out string value) && activeByThread.ContainsKey(appThreadId))
		{
			(AgentHarness, AgentHarnessPlace)? tuple = AgentHarnessPreferences.RuntimeChoice(value);
			if (tuple.HasValue)
			{
				(AgentHarness, AgentHarnessPlace) valueOrDefault = tuple.GetValueOrDefault();
				IAgentHarnessRuntime agentHarnessRuntime = AgentHarnessRuntimes.Runtime(valueOrDefault.Item1, valueOrDefault.Item2);
				if (agentHarnessRuntime != null)
				{
					return await agentHarnessRuntime.SteerAsync(value, text, informational, ct, imagePaths).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		return false;
	}

	private bool InterruptHarnessTurn(string appThreadId)
	{
		if (harnessThreadIdByAppThreadId.TryGetValue(appThreadId, out string value) && activeByThread.ContainsKey(appThreadId))
		{
			(AgentHarness, AgentHarnessPlace)? tuple = AgentHarnessPreferences.RuntimeChoice(value);
			if (tuple.HasValue)
			{
				(AgentHarness, AgentHarnessPlace) valueOrDefault = tuple.GetValueOrDefault();
				IAgentHarnessRuntime agentHarnessRuntime = AgentHarnessRuntimes.Runtime(valueOrDefault.Item1, valueOrDefault.Item2);
				if (agentHarnessRuntime != null)
				{
					agentHarnessRuntime.InterruptAsync(value);
					return true;
				}
			}
		}
		return false;
	}

	private static JsonObject HarnessToolItem(string id, string tool, string? server, string? command)
	{
		if (command != null)
		{
			return new JsonObject((JsonNodeOptions?)null)
			{
				["type"] = "commandExecution",
				["id"] = id,
				["command"] = command
			};
		}
		return new JsonObject((JsonNodeOptions?)null)
		{
			["type"] = "mcpToolCall",
			["id"] = id,
			["tool"] = tool,
			["server"] = server ?? "files"
		};
	}

	private static string Cap(string s, int n)
	{
		if (s.Length <= n)
		{
			return s;
		}
		return s.Substring(0, n);
	}
}
