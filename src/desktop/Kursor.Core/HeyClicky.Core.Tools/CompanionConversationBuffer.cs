using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeyClicky.Core.Tools;

public sealed class CompanionConversationBuffer
{
	private sealed class Turn(string userMessage, string assistantReply, DateTimeOffset createdAt)
	{
		public readonly string userMessage = userMessage;

		public readonly string assistantReply = assistantReply;

		public readonly DateTimeOffset createdAt = createdAt;

		public DateTimeOffset? AgentPromptConsumedAt;
	}

	public static readonly CompanionConversationBuffer Shared = new CompanionConversationBuffer();

	public const int MaximumRetainedTurns = 10;

	public const int MaximumTurnsPerLaunch = 10;

	public static readonly TimeSpan MaximumTurnAgeForFreshLaunch = TimeSpan.FromMinutes(3L);

	public static readonly TimeSpan MaximumTurnAgeForFollowUp = TimeSpan.FromMinutes(30L);

	private readonly object gate = new object();

	private readonly List<Turn> turns = new List<Turn>();

	private readonly Dictionary<string, DateTimeOffset> lastFollowUpSentAtByThreadId = new Dictionary<string, DateTimeOffset>();

	private readonly Func<DateTimeOffset> now;

	private static readonly string[] PhrasalReferents = new string[12]
	{
		"do it", "do that", "do this", "do them", "go ahead", "let's do it", "lets do it", "now do", "now that", "what about",
		"and that", "and this"
	};

	private static readonly HashSet<string> StandaloneReferents = new HashSet<string> { "it", "that", "this", "them", "those", "these" };

	public int Count
	{
		get
		{
			lock (gate)
			{
				return turns.Count;
			}
		}
	}

	public CompanionConversationBuffer(Func<DateTimeOffset>? clock = null)
	{
		now = clock ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.UtcNow));
	}

	public void Append(string? userMessage, string? assistantReply)
	{
		string text = userMessage?.Trim() ?? "";
		if (text.Length == 0)
		{
			return;
		}
		string text2 = assistantReply?.Trim() ?? "";
		lock (gate)
		{
			turns.Add(new Turn((text.Length > 250) ? text.Substring(0, 250) : text, (text2.Length > 450) ? text2.Substring(0, 450) : text2, now()));
			if (turns.Count > 10)
			{
				turns.RemoveRange(0, turns.Count - 10);
			}
		}
	}

	public void Clear()
	{
		lock (gate)
		{
			turns.Clear();
			lastFollowUpSentAtByThreadId.Clear();
		}
	}

	public IReadOnlyList<(string User, string Assistant)> ConsumeForFreshLaunch(string transcript)
	{
		if (!HasDeicticReferent(transcript))
		{
			return Array.Empty<(string, string)>();
		}
		lock (gate)
		{
			DateTimeOffset reference = now();
			List<Turn> list = turns.Where(delegate(Turn t)
			{
				DateTimeOffset? agentPromptConsumedAt = t.AgentPromptConsumedAt;
				return !agentPromptConsumedAt.HasValue && reference - t.createdAt <= MaximumTurnAgeForFreshLaunch;
			}).TakeLast(10).ToList();
			foreach (Turn item in list)
			{
				item.AgentPromptConsumedAt = reference;
			}
			return list.Select((Turn t) => (UserMessage: t.userMessage, AssistantReply: t.assistantReply)).ToList();
		}
	}

	public IReadOnlyList<(string User, string Assistant)> SinceLastMessage(string threadId)
	{
		lock (gate)
		{
			DateTimeOffset dateTimeOffset = now();
			DateTimeOffset value;
			DateTimeOffset? lastSentAt = (lastFollowUpSentAtByThreadId.TryGetValue(threadId, out value) ? new DateTimeOffset?(value) : ((DateTimeOffset?)null));
			lastFollowUpSentAtByThreadId[threadId] = dateTimeOffset;
			DateTimeOffset earliest = dateTimeOffset - MaximumTurnAgeForFollowUp;
			if (lastSentAt.HasValue)
			{
				DateTimeOffset valueOrDefault = lastSentAt.GetValueOrDefault();
				if (valueOrDefault > earliest)
				{
					earliest = valueOrDefault;
				}
			}
			return (from t in turns.Where(delegate(Turn t)
				{
					if (t.createdAt > earliest)
					{
						if (!lastSentAt.HasValue)
						{
							DateTimeOffset? agentPromptConsumedAt = t.AgentPromptConsumedAt;
							return !agentPromptConsumedAt.HasValue;
						}
						return true;
					}
					return false;
				}).TakeLast(10)
				select (UserMessage: t.userMessage, AssistantReply: t.assistantReply)).ToList();
		}
	}

	public static bool HasDeicticReferent(string transcript)
	{
		string text = transcript.ToLowerInvariant();
		if (PhrasalReferents.Any(text.Contains))
		{
			return true;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text2 = text + " ";
		foreach (char c in text2)
		{
			if (char.IsLetterOrDigit(c))
			{
				stringBuilder.Append(c);
				continue;
			}
			if (stringBuilder.Length > 0 && StandaloneReferents.Contains(stringBuilder.ToString()))
			{
				return true;
			}
			stringBuilder.Clear();
		}
		return false;
	}
}
