using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using HeyClicky.Core.Auth;

namespace HeyClicky.Core.Billing;

public sealed class AgentTurnCostLedger
{
	private const int MaximumRecords = 500;

	private readonly string? path;

	private readonly Lock gate = new Lock();

	private readonly List<AgentTurnUsageRecord> records = new List<AgentTurnUsageRecord>();

	public string CurrentLaneModel { get; private set; }

	public string CurrentLaneReasoningEffort { get; private set; }

	public bool HasReadCurrentLane { get; private set; }

	public IReadOnlyList<AgentTurnUsageRecord> Records
	{
		get
		{
			using (gate.EnterScope())
			{
				return records.ToList();
			}
		}
	}

	public event Action? Changed;

	public AgentTurnCostLedger(string? path, string defaultModel, string defaultReasoningEffort)
	{
		this.path = path;
		CurrentLaneModel = defaultModel;
		CurrentLaneReasoningEffort = defaultReasoningEffort;
		Load();
	}

	public bool ApplyModelPolicy(JsonObject? policy)
	{
		if (policy?["codexAgentLane"] is JsonObject jsonObject)
		{
			string text = jsonObject["model"]?.GetValue<string>();
			if (text != null && text.Length > 0)
			{
				using (gate.EnterScope())
				{
					CurrentLaneModel = text;
					string text2 = jsonObject["reasoningEffort"]?.GetValue<string>();
					if (text2 != null && text2.Length > 0)
					{
						CurrentLaneReasoningEffort = text2;
					}
					HasReadCurrentLane = true;
				}
				Log.Info("agent cost", "proxy lane is " + CurrentLaneModel + "/" + CurrentLaneReasoningEffort);
				return true;
			}
		}
		return false;
	}

	public bool Record(AgentTurnUsageRecord record)
	{
		if (record.InputTokens <= 0 && record.OutputTokens <= 0)
		{
			return false;
		}
		using (gate.EnterScope())
		{
			records.Add(record);
			if (records.Count > 500)
			{
				records.RemoveRange(0, records.Count - 500);
			}
			Persist();
		}
		string[] array = CostLine(record).Split('\n');
		foreach (string message in array)
		{
			Log.Info("agent cost", message);
		}
		this.Changed?.Invoke();
		return true;
	}

	public bool RecordBilledCost(string leaseId, double billedCostUsd)
	{
		bool flag = false;
		using (gate.EnterScope())
		{
			for (int i = 0; i < records.Count; i++)
			{
				if (records[i].LeaseId == leaseId)
				{
					records[i] = records[i]with
					{
						BilledCostUsd = billedCostUsd
					};
					flag = true;
				}
			}
			if (flag)
			{
				Persist();
			}
		}
		if (!flag)
		{
			return false;
		}
		Log.Info("agent cost", "billed by the Worker for lease " + leaseId.Substring(0, Math.Min(8, leaseId.Length)) + ": " + Usd(billedCostUsd));
		this.Changed?.Invoke();
		return true;
	}

	public AgentTurnUsageRecord? RecordFor(string? threadId, string? turnId)
	{
		if (threadId == null || turnId == null)
		{
			return null;
		}
		using (gate.EnterScope())
		{
			return records.LastOrDefault((AgentTurnUsageRecord r) => r.ThreadId == threadId && r.TurnId == turnId);
		}
	}

	public string? CostLineFor(string? threadId, string? turnId)
	{
		AgentTurnUsageRecord agentTurnUsageRecord = RecordFor(threadId, turnId);
		if ((object)agentTurnUsageRecord == null)
		{
			return null;
		}
		return CostLine(agentTurnUsageRecord);
	}

	public string CostLine(AgentTurnUsageRecord record)
	{
		List<string> list = new List<string>();
		double? billedCostUsd = record.BilledCostUsd;
		if (billedCostUsd.HasValue)
		{
			double valueOrDefault = billedCostUsd.GetValueOrDefault();
			list.Add("billed " + Usd(valueOrDefault) + " by the Worker (incl. cache writes + web searches, whole agent message)");
		}
		string value = $"{Tokens(record.InputTokens)} in ({Tokens(record.CachedInputTokens)} cached), {Tokens(record.OutputTokens)} out";
		AgentModelPricing? recordPricing = AgentModelPricing.For(record.Model);
		object item;
		if (recordPricing.HasValue)
		{
			AgentModelPricing valueOrDefault2 = recordPricing.GetValueOrDefault();
			item = $"≈ {Usd(valueOrDefault2.CostUsd(record.InputTokens, record.CachedInputTokens, record.OutputTokens))} this turn on {record.Model}/{record.ReasoningEffort} · {value}";
		}
		else
		{
			item = $"this turn on {record.Model}/{record.ReasoningEffort} (no price on file) · {value}";
		}
		list.Add((string)item);
		List<(string, AgentModelPricing)> list2 = AgentModelPricing.ComparisonTiers.Where(delegate((string Label, AgentModelPricing Pricing) t)
		{
			AgentModelPricing item2 = t.Pricing;
			AgentModelPricing? agentModelPricing = recordPricing;
			return item2 != agentModelPricing;
		}).ToList();
		if (list2.Count > 0)
		{
			list.Add(string.Join(" · ", list2.Select<(string, AgentModelPricing), string>(((string Label, AgentModelPricing Pricing) t) => t.Label + " ≈ " + Usd(t.Pricing.CostUsd(record.InputTokens, record.CachedInputTokens, record.OutputTokens)))));
		}
		string slug = record.ClickySlug;
		if (slug != null)
		{
			List<AgentTurnUsageRecord> day;
			using (gate.EnterScope())
			{
				day = records.Where((AgentTurnUsageRecord r) => r.ClickySlug == slug && r.FinishedAt <= record.FinishedAt && record.FinishedAt - r.FinishedAt <= TimeSpan.FromHours(24)).ToList();
			}
			double value2 = day.Sum((AgentTurnUsageRecord r) => AgentModelPricing.For(r.Model)?.CostUsd(r.InputTokens, r.CachedInputTokens, r.OutputTokens) ?? 0.0);
			IEnumerable<string> second = list2.Select<(string, AgentModelPricing), string>(((string Label, AgentModelPricing Pricing) t) => t.Label + " " + Usd(day.Sum((AgentTurnUsageRecord r) => t.Pricing.CostUsd(r.InputTokens, r.CachedInputTokens, r.OutputTokens))));
			list.Add(string.Join(" · ", new string[1] { $"{record.ClickyName ?? slug}, last 24h ({day.Count} turns): {Usd(value2)}" }.Concat(second)));
		}
		return string.Join("\n", list);
	}

	public static string Usd(double value)
	{
		if (!(value < 0.01))
		{
			if (!(value < 1.0))
			{
				return "$" + value.ToString("0.00", CultureInfo.InvariantCulture);
			}
			return "$" + value.ToString("0.000", CultureInfo.InvariantCulture);
		}
		return "$" + value.ToString("0.0000", CultureInfo.InvariantCulture);
	}

	public static string Tokens(long count)
	{
		if (count < 1000000)
		{
			if (count < 1000)
			{
				return count.ToString(CultureInfo.InvariantCulture);
			}
			return ((double)count / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + "k";
		}
		return ((double)count / 1000000.0).ToString("0.0", CultureInfo.InvariantCulture) + "M";
	}

	private void Load()
	{
		if (path == null || !File.Exists(path))
		{
			return;
		}
		try
		{
			if (!(JsonNode.Parse(File.ReadAllText(path), null) is JsonArray source))
			{
				return;
			}
			foreach (JsonObject item in source.OfType<JsonObject>())
			{
				string text = item["task_id"]?.GetValue<string>();
				if (text != null)
				{
					records.Add(new AgentTurnUsageRecord(text, Str(item, "thread_id"), Str(item, "turn_id"), Str(item, "clicky_slug"), Str(item, "clicky_name"), DateTimeOffset.FromUnixTimeMilliseconds(item["finished_at_ms"]?.GetValue<long>() ?? 0), Str(item, "model") ?? "unknown", Str(item, "reasoning_effort") ?? "", item["input_tokens"]?.GetValue<long>() ?? 0, item["cached_input_tokens"]?.GetValue<long>() ?? 0, item["output_tokens"]?.GetValue<long>() ?? 0, Str(item, "lease_id"), item["billed_cost_usd"]?.GetValue<double>()));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("agent cost", "ledger unreadable, starting empty: " + ex.Message);
			records.Clear();
		}
		static string? Str(JsonObject o, string key)
		{
			return o[key]?.GetValue<string>();
		}
	}

	private void Persist()
	{
		if (path == null)
		{
			return;
		}
		try
		{
			JsonArray jsonArray = new JsonArray((JsonNodeOptions?)null);
			foreach (AgentTurnUsageRecord record in records)
			{
				jsonArray.Add((JsonNode?)new JsonObject((JsonNodeOptions?)null)
				{
					["task_id"] = record.TaskId,
					["thread_id"] = record.ThreadId,
					["turn_id"] = record.TurnId,
					["clicky_slug"] = record.ClickySlug,
					["clicky_name"] = record.ClickyName,
					["finished_at_ms"] = record.FinishedAt.ToUnixTimeMilliseconds(),
					["model"] = record.Model,
					["reasoning_effort"] = record.ReasoningEffort,
					["input_tokens"] = record.InputTokens,
					["cached_input_tokens"] = record.CachedInputTokens,
					["output_tokens"] = record.OutputTokens,
					["lease_id"] = record.LeaseId,
					["billed_cost_usd"] = record.BilledCostUsd
				});
			}
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			AtomicFile.WriteAllBytes(path, Encoding.UTF8.GetBytes(jsonArray.ToJsonString()));
		}
		catch (Exception ex)
		{
			Log.Warn("agent cost", "ledger write failed: " + ex.Message);
		}
	}
}
