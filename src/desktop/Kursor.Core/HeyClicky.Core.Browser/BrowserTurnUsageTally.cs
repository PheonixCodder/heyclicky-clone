using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HeyClicky.Core.Browser;

public sealed class BrowserTurnUsageTally
{
	public const int MaxTurns = 64;

	public static readonly TimeSpan DefaultTidyWait = TimeSpan.FromSeconds(20L);

	private readonly object gate = new object();

	private readonly Action<BrowserTurnUsage, int?> send;

	private readonly TimeSpan tidyWait;

	private readonly Func<DateTime> now;

	private readonly Dictionary<string, BrowserTurnUsage> turns = new Dictionary<string, BrowserTurnUsage>();

	private readonly Dictionary<string, BrowserTurnUsage> awaitingTidy = new Dictionary<string, BrowserTurnUsage>();

	private readonly Dictionary<string, string> tidyCommandTurns = new Dictionary<string, string>();

	private readonly Dictionary<string, (int Remaining, int TabsClosed)> tidyRepliesAwaited = new Dictionary<string, (int, int)>();

	public BrowserTurnUsageTally(Action<BrowserTurnUsage, int?> send, TimeSpan? tidyWait = null, Func<DateTime>? now = null)
	{
		this.send = send;
		this.tidyWait = tidyWait ?? DefaultTidyWait;
		this.now = now ?? ((Func<DateTime>)(() => DateTime.UtcNow));
	}

	public void NoteCommandSent(string task, string thread)
	{
		lock (gate)
		{
			Usage(task, thread).CommandsSentToBrowser++;
		}
	}

	public void NoteTimeout(string task, string operation)
	{
		lock (gate)
		{
			if (turns.TryGetValue(task, out BrowserTurnUsage value))
			{
				value.TimeoutCount++;
				value.TimeoutOperations[operation] = value.TimeoutOperations.GetValueOrDefault(operation) + 1;
			}
		}
	}

	public void NoteAccessEndedByTimeout(string task)
	{
		lock (gate)
		{
			if (turns.TryGetValue(task, out BrowserTurnUsage value))
			{
				value.AccessEndedByTimeout = true;
			}
		}
	}

	public void NoteStoppedByUser(IEnumerable<string> tasks)
	{
		lock (gate)
		{
			foreach (string task in tasks)
			{
				if (turns.TryGetValue(task, out BrowserTurnUsage value))
				{
					value.WasStoppedByUser = true;
				}
			}
		}
	}

	public string? NoteCall(string task, string thread, JsonObject arguments, JsonObject result)
	{
		string text = Str(arguments["operation"]) ?? ((arguments["steps"] is JsonArray) ? "batch" : "");
		string text2 = (ClickyBrowserBridge.AgentOperations.Contains(text) ? text : "other");
		lock (gate)
		{
			if (!turns.ContainsKey(task) && turns.Count >= 64)
			{
				return null;
			}
			BrowserTurnUsage browserTurnUsage = Usage(task, thread);
			browserTurnUsage.OperationCount++;
			browserTurnUsage.OperationCounts[text2] = browserTurnUsage.OperationCounts.GetValueOrDefault(text2) + 1;
			if (text2 == "batch")
			{
				browserTurnUsage.BatchStepCount += (arguments["steps"] as JsonArray)?.Count ?? 0;
			}
			string result2 = null;
			if (result.ContainsKey("error") && result["error"] != null)
			{
				browserTurnUsage.ErrorCount++;
				string? message = Str(result["error"]) ?? "";
				string key = BrowserErrorKinds.Kind(message);
				browserTurnUsage.ErrorKinds[key] = browserTurnUsage.ErrorKinds.GetValueOrDefault(key) + 1;
				string text3 = BrowserErrorKinds.DetachReason(message);
				if (text3 != null && browserTurnUsage.DetachReasons.Add(text3))
				{
					result2 = text3;
				}
			}
			bool value = default(bool);
			if (text2 == "keep" && result["kept"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value) && value && !result.ContainsKey("note"))
			{
				browserTurnUsage.TabsKept++;
			}
			if (Str(result["saved"]) != null)
			{
				browserTurnUsage.ScreenshotsSaved++;
			}
			return result2;
		}
	}

	public void Finish(string task, IReadOnlyList<string> answerableTidyCommandIds, bool hasPeers)
	{
		BrowserTurnUsage browserTurnUsage = null;
		int? arg = null;
		lock (gate)
		{
			if (!turns.Remove(task, out BrowserTurnUsage value))
			{
				return;
			}
			value.EndedAt = now();
			if (value.CommandsSentToBrowser == 0 || answerableTidyCommandIds.Count == 0 || !hasPeers)
			{
				browserTurnUsage = value;
				arg = ((value.CommandsSentToBrowser > 0) ? ((int?)null) : new int?(0));
			}
			else
			{
				awaitingTidy[task] = value;
				tidyRepliesAwaited[task] = (answerableTidyCommandIds.Count, 0);
				foreach (string answerableTidyCommandId in answerableTidyCommandIds)
				{
					tidyCommandTurns[answerableTidyCommandId] = task;
				}
			}
		}
		if (browserTurnUsage != null)
		{
			send(browserTurnUsage, arg);
			return;
		}
		List<string> ids = answerableTidyCommandIds.ToList();
		ExpireAsync();
		async Task ExpireAsync()
		{
			await Task.Delay(tidyWait).ConfigureAwait(continueOnCapturedContext: false);
			lock (gate)
			{
				foreach (string item in ids)
				{
					tidyCommandTurns.Remove(item);
				}
			}
			SendAwaitingTidy(task, null);
		}
	}

	public bool NoteTidyReply(string commandId, int tabsClosed)
	{
		int? tabsClosed2 = null;
		string value;
		lock (gate)
		{
			if (!tidyCommandTurns.Remove(commandId, out value))
			{
				return false;
			}
			if (!tidyRepliesAwaited.TryGetValue(value, out (int, int) value2))
			{
				return true;
			}
			value2 = (value2.Item1 - 1, value2.Item2 + tabsClosed);
			tidyRepliesAwaited[value] = value2;
			if (value2.Item1 <= 0)
			{
				tabsClosed2 = value2.Item2;
			}
		}
		if (tabsClosed2.HasValue)
		{
			SendAwaitingTidy(value, tabsClosed2);
		}
		return true;
	}

	public static int TabsClosed(JsonNode? result)
	{
		if (!((result as JsonObject)?["closed"] is JsonArray jsonArray))
		{
			return 0;
		}
		return jsonArray.Count;
	}

	private void SendAwaitingTidy(string task, int? tabsClosed)
	{
		BrowserTurnUsage value;
		lock (gate)
		{
			tidyRepliesAwaited.Remove(task);
			if (!awaitingTidy.Remove(task, out value))
			{
				return;
			}
		}
		send(value, tabsClosed);
	}

	private BrowserTurnUsage Usage(string task, string thread)
	{
		if (!turns.TryGetValue(task, out BrowserTurnUsage value))
		{
			value = (turns[task] = new BrowserTurnUsage(thread, now()));
		}
		return value;
	}

	public static JsonObject Properties(BrowserTurnUsage usage, int? tabsClosed, DateTime now)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["operation_count"] = usage.OperationCount,
			["batch_step_count"] = usage.BatchStepCount,
			["commands_sent_to_browser"] = usage.CommandsSentToBrowser,
			["reached_browser"] = usage.CommandsSentToBrowser > 0,
			["error_count"] = usage.ErrorCount,
			["timeout_count"] = usage.TimeoutCount,
			["tabs_kept"] = usage.TabsKept,
			["screenshots_saved"] = usage.ScreenshotsSaved,
			["was_stopped_by_user"] = usage.WasStoppedByUser,
			["access_ended_by_timeout"] = usage.AccessEndedByTimeout,
			["browser_seconds"] = (int)Math.Round(((usage.EndedAt ?? now) - usage.FirstCallAt).TotalSeconds, MidpointRounding.AwayFromZero),
			["tabs_closed_known"] = tabsClosed.HasValue
		};
		if (tabsClosed.HasValue)
		{
			int valueOrDefault = tabsClosed.GetValueOrDefault();
			jsonObject["tabs_closed"] = valueOrDefault;
		}
		string key;
		int value;
		foreach (KeyValuePair<string, int> operationCount in usage.OperationCounts)
		{
			operationCount.Deconstruct(out key, out value);
			string text = key;
			int num = value;
			jsonObject["op_" + text] = num;
		}
		foreach (KeyValuePair<string, int> errorKind in usage.ErrorKinds)
		{
			errorKind.Deconstruct(out key, out value);
			string text2 = key;
			int num2 = value;
			jsonObject["error_" + text2] = num2;
		}
		foreach (KeyValuePair<string, int> timeoutOperation in usage.TimeoutOperations)
		{
			timeoutOperation.Deconstruct(out key, out value);
			string text3 = key;
			int num3 = value;
			jsonObject["timeout_" + text3] = num3;
		}
		if (usage.DetachReasons.Count > 0)
		{
			jsonObject["detach_reasons"] = string.Join(",", usage.DetachReasons);
		}
		return jsonObject;
	}

	private static string? Str(JsonNode? node)
	{
		if (!(node is JsonValue jsonValue) || jsonValue.GetValueKind() != JsonValueKind.String)
		{
			return null;
		}
		return jsonValue.GetValue<string>();
	}
}
