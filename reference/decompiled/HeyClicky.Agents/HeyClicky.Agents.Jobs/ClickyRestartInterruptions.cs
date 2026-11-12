using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Jobs;

public sealed class ClickyRestartInterruptions
{
	public sealed record Turn
	{
		public required string Slug { get; init; }

		public string? ThreadId { get; init; }

		public DateTimeOffset StartedAt { get; init; }

		public DateTimeOffset? InterruptedAt { get; init; }

		public bool? IsInChat { get; init; }

		[CompilerGenerated]
		[SetsRequiredMembers]
		private Turn(Turn original)
		{
			Slug = original.Slug;
			ThreadId = original.ThreadId;
			StartedAt = original.StartedAt;
			InterruptedAt = original.InterruptedAt;
			IsInChat = original.IsInChat;
		}

		public Turn()
		{
		}
	}

	private const string DefaultsKey = "clicky.clickys.runningTurns.v1";

	private static readonly TimeSpan KeptFor = TimeSpan.FromDays(7);

	private readonly object gate = new object();

	private Dictionary<string, Turn> turnsBySlug = new Dictionary<string, Turn>();

	public ClickyRestartInterruptions()
	{
		DateTimeOffset now = DateTimeOffset.Now;
		foreach (Turn item in AgentDefaults.Get<List<Turn>>("clicky.clickys.runningTurns.v1") ?? new List<Turn>())
		{
			Turn turn = ((!item.InterruptedAt.HasValue) ? item with
			{
				InterruptedAt = now
			} : item);
			if (!(now - (turn.InterruptedAt ?? now) >= KeptFor))
			{
				turnsBySlug[turn.Slug] = turn;
			}
		}
		Save();
	}

	public void NoteRunning(IReadOnlyList<(string Slug, string? ThreadId, DateTimeOffset StartedAt)> running)
	{
		lock (gate)
		{
			Dictionary<string, Turn> dictionary = turnsBySlug.Where<KeyValuePair<string, Turn>>((KeyValuePair<string, Turn> kv) => kv.Value.InterruptedAt.HasValue).ToDictionary((KeyValuePair<string, Turn> kv) => kv.Key, (KeyValuePair<string, Turn> kv) => kv.Value);
			foreach (var item in running)
			{
				if (turnsBySlug.TryGetValue(item.Slug, out Turn value) && !value.InterruptedAt.HasValue && value.ThreadId == item.ThreadId)
				{
					dictionary[item.Slug] = value;
					continue;
				}
				dictionary[item.Slug] = new Turn
				{
					Slug = item.Slug,
					ThreadId = item.ThreadId,
					StartedAt = item.StartedAt
				};
			}
			if (dictionary.Count != turnsBySlug.Count || !dictionary.All((KeyValuePair<string, Turn> kv) => turnsBySlug.TryGetValue(kv.Key, out Turn value2) && value2 == kv.Value))
			{
				turnsBySlug = dictionary;
				Save();
			}
		}
	}

	public Turn? Interruption(string slug)
	{
		lock (gate)
		{
			Turn value;
			return (turnsBySlug.TryGetValue(slug, out value) && value.InterruptedAt.HasValue) ? value : null;
		}
	}

	public void Clear(string slug)
	{
		lock (gate)
		{
			if (turnsBySlug.Remove(slug))
			{
				Save();
			}
		}
	}

	public IReadOnlyList<Turn> TakeInterruptionsForChat()
	{
		lock (gate)
		{
			List<Turn> list = turnsBySlug.Values.Where((Turn t) => t.InterruptedAt.HasValue && t.IsInChat != true).ToList();
			if (list.Count == 0)
			{
				return Array.Empty<Turn>();
			}
			foreach (Turn item in list)
			{
				turnsBySlug[item.Slug] = item with
				{
					IsInChat = true
				};
			}
			Save();
			return list.OrderBy((Turn t) => t.StartedAt).ToList();
		}
	}

	public void Reset()
	{
		lock (gate)
		{
			turnsBySlug = new Dictionary<string, Turn>();
		}
		AgentDefaults.Remove("clicky.clickys.runningTurns.v1");
	}

	private void Save()
	{
		lock (gate)
		{
			AgentDefaults.Set("clicky.clickys.runningTurns.v1", turnsBySlug.Values.ToList());
		}
	}
}
