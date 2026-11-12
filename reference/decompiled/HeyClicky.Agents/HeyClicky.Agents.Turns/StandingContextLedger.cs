using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HeyClicky.Agents.Turns;

internal static class StandingContextLedger
{
	public enum Section
	{
		UserMemory,
		AgentInstructions,
		ActiveSkills,
		LearnedHowTos
	}

	private sealed class Ledger
	{
		public Dictionary<string, Dictionary<string, string>> FingerprintsByThreadId { get; set; } = new Dictionary<string, Dictionary<string, string>>();

		public List<string> ThreadOrder { get; set; } = new List<string>();
	}

	private const string DefaultsKey = "clicky.codexAgent.standingContextCarried.v1";

	private const int MaximumRememberedThreadCount = 2000;

	private static readonly object Gate = new object();

	private static string Fingerprint(string text)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
	}

	private static string Key(Section section)
	{
		return section switch
		{
			Section.UserMemory => "userMemory", 
			Section.AgentInstructions => "agentInstructions", 
			Section.ActiveSkills => "activeSkills", 
			_ => "learnedHowTos", 
		};
	}

	public static bool NeedsCarrying(Section section, string? text, string? threadId)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (threadId == null)
		{
			return true;
		}
		lock (Gate)
		{
			Dictionary<string, string> value;
			string value2;
			return !(AgentDefaults.Get<Ledger>("clicky.codexAgent.standingContextCarried.v1") ?? new Ledger()).FingerprintsByThreadId.TryGetValue(threadId, out value) || !value.TryGetValue(Key(section), out value2) || !(value2 == Fingerprint(text));
		}
	}

	public static void RecordCarried(IReadOnlyDictionary<Section, string> carried, string threadId)
	{
		if (carried.Count == 0)
		{
			return;
		}
		lock (Gate)
		{
			Ledger ledger = AgentDefaults.Get<Ledger>("clicky.codexAgent.standingContextCarried.v1") ?? new Ledger();
			if (!ledger.FingerprintsByThreadId.TryGetValue(threadId, out Dictionary<string, string> value))
			{
				value = (ledger.FingerprintsByThreadId[threadId] = new Dictionary<string, string>());
			}
			foreach (var (section2, text2) in carried)
			{
				value[Key(section2)] = Fingerprint(text2);
			}
			ledger.ThreadOrder.Remove(threadId);
			ledger.ThreadOrder.Add(threadId);
			while (ledger.ThreadOrder.Count > 2000)
			{
				ledger.FingerprintsByThreadId.Remove(ledger.ThreadOrder[0]);
				ledger.ThreadOrder.RemoveAt(0);
			}
			AgentDefaults.Set("clicky.codexAgent.standingContextCarried.v1", ledger);
		}
	}

	public static void ForgetThread(string threadId)
	{
		lock (Gate)
		{
			Ledger ledger = AgentDefaults.Get<Ledger>("clicky.codexAgent.standingContextCarried.v1") ?? new Ledger();
			if (ledger.FingerprintsByThreadId.Remove(threadId))
			{
				ledger.ThreadOrder.Remove(threadId);
				AgentDefaults.Set("clicky.codexAgent.standingContextCarried.v1", ledger);
			}
		}
	}
}
