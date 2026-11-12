using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Brain;

public sealed class OneBrainStats
{
	public sealed record Session
	{
		public string Id { get; set; } = "";

		public DateTimeOffset? StartedAt { get; set; }

		public DateTimeOffset? LastTurnAt { get; set; }

		public int ThreadItems { get; set; }

		public int ScreenshotsInThread { get; set; }

		public int LastInputTokens { get; set; }

		public int BaselineInputTokens { get; set; }

		public int NextCompactionCheckInputTokens { get; set; }

		public bool IsCompacting { get; set; }

		public int TurnCount { get; set; }
	}

	public sealed record Turn
	{
		public string Kind { get; set; } = "";

		public string Model { get; set; } = "";

		public DateTimeOffset? KeyUpAt { get; set; }

		public DateTimeOffset? SentAt { get; set; }

		public int? FirstTokenMilliseconds { get; set; }

		public DateTimeOffset? FirstHeardAt { get; set; }

		public int Rounds { get; set; }

		public int Input { get; set; }

		public int Cached { get; set; }

		public int Wrote { get; set; }

		public int Output { get; set; }

		public int Reasoning { get; set; }

		public double Cost { get; set; }

		public double? IdleMinutesBefore { get; set; }
	}

	public sealed record Event(DateTimeOffset At, string Text);

	private readonly object gate = new object();

	private Session session = new Session();

	private Turn? lastTurn;

	private readonly List<Event> events = new List<Event>();

	private DateTimeOffset? pendingKeyUpAt;

	public static OneBrainStats Shared { get; } = new OneBrainStats();

	public Func<bool> IsEnabledProvider { get; set; } = () => false;

	public bool IsEnabled => IsEnabledProvider();

	internal Func<DateTimeOffset> Now { get; set; } = () => DateTimeOffset.Now;

	public Session CurrentSession
	{
		get
		{
			lock (gate)
			{
				return session;
			}
		}
	}

	public Turn? LastTurn
	{
		get
		{
			lock (gate)
			{
				return lastTurn;
			}
		}
	}

	public IReadOnlyList<Event> Events
	{
		get
		{
			lock (gate)
			{
				return events.ToList();
			}
		}
	}

	public event EventHandler? Changed;

	public void UpdateSession(Func<Session, Session> update)
	{
		if (!IsEnabled)
		{
			return;
		}
		bool flag;
		lock (gate)
		{
			Session session = update(this.session);
			flag = session != this.session;
			if (flag)
			{
				this.session = session;
			}
		}
		if (flag)
		{
			Raise();
		}
	}

	public void Note(string text)
	{
		if (!IsEnabled)
		{
			return;
		}
		lock (gate)
		{
			events.Add(new Event(Now(), text));
			if (events.Count > 8)
			{
				events.RemoveRange(0, events.Count - 8);
			}
		}
		Raise();
	}

	public void NoteKeyUp()
	{
		if (!IsEnabled)
		{
			return;
		}
		lock (gate)
		{
			pendingKeyUpAt = Now();
		}
	}

	public void NoteTurnSent(bool spoken, DateTimeOffset? previousTurnAt)
	{
		if (!IsEnabled)
		{
			return;
		}
		lock (gate)
		{
			DateTimeOffset dateTimeOffset = Now();
			DateTimeOffset? obj;
			if (spoken)
			{
				DateTimeOffset? dateTimeOffset2 = pendingKeyUpAt;
				if (dateTimeOffset2.HasValue)
				{
					DateTimeOffset valueOrDefault = dateTimeOffset2.GetValueOrDefault();
					if (dateTimeOffset - valueOrDefault < TimeSpan.FromSeconds(15L))
					{
						obj = valueOrDefault;
						goto IL_006d;
					}
				}
			}
			obj = null;
			goto IL_006d;
			IL_00eb:
			object obj2;
			double? idleMinutesBefore;
			((Turn)obj2).IdleMinutesBefore = (double?)idleMinutesBefore;
			lastTurn = (Turn?)obj2;
			goto end_IL_0012;
			IL_006d:
			DateTimeOffset? keyUpAt = obj;
			pendingKeyUpAt = null;
			obj2 = new Turn
			{
				Kind = (spoken ? "spoken" : "app/click"),
				KeyUpAt = keyUpAt,
				SentAt = dateTimeOffset
			};
			if (previousTurnAt.HasValue)
			{
				DateTimeOffset valueOrDefault2 = previousTurnAt.GetValueOrDefault();
				if (valueOrDefault2 > DateTimeOffset.MinValue)
				{
					idleMinutesBefore = (dateTimeOffset - valueOrDefault2).TotalMinutes;
					goto IL_00eb;
				}
			}
			idleMinutesBefore = null;
			goto IL_00eb;
			end_IL_0012:;
		}
		Raise();
	}

	public void NoteRound(int round, JsonObject? usage, double? cost, string model, int? firstTokenMilliseconds)
	{
		if (!IsEnabled)
		{
			return;
		}
		lock (gate)
		{
			Turn turn = lastTurn;
			if ((object)turn == null)
			{
				return;
			}
			turn = turn with
			{
				Rounds = Math.Max(turn.Rounds, round + 1),
				Model = model,
				Cost = turn.Cost + cost.GetValueOrDefault()
			};
			if (round == 0)
			{
				turn = turn with
				{
					Input = Number("input"),
					Cached = Number("cached"),
					Wrote = Number("wrote"),
					Output = Number("output"),
					Reasoning = Number("reasoning"),
					FirstTokenMilliseconds = firstTokenMilliseconds
				};
			}
			lastTurn = turn;
		}
		Raise();
		int Number(string key)
		{
			if (!(usage?[key] is JsonValue jsonValue))
			{
				return 0;
			}
			if (!jsonValue.TryGetValue<double>(out var value))
			{
				if (!jsonValue.TryGetValue<int>(out var value2))
				{
					if (!jsonValue.TryGetValue<long>(out var value3))
					{
						return 0;
					}
					return (int)value3;
				}
				return value2;
			}
			return (int)value;
		}
	}

	public void NoteFirstLineHeard()
	{
		if (!IsEnabled)
		{
			return;
		}
		lock (gate)
		{
			Turn turn = lastTurn;
			if ((object)turn == null || turn.FirstHeardAt.HasValue)
			{
				return;
			}
			lastTurn = turn with
			{
				FirstHeardAt = Now()
			};
		}
		Raise();
	}

	public void ResetForNewSession()
	{
		lock (gate)
		{
			lastTurn = null;
			pendingKeyUpAt = null;
		}
		Raise();
	}

	private void Raise()
	{
		try
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Log.Warn("OneBrain", "stats listener threw: " + ex.Message);
		}
	}

	public (List<(string Text, string Style)> Lines, double Progress, bool PastThreshold, int ProgressAfterLine) Describe(DateTimeOffset now)
	{
		Session session;
		Turn turn;
		List<Event> list;
		lock (gate)
		{
			session = this.session;
			turn = lastTurn;
			list = events.TakeLast(5).ToList();
		}
		List<(string, string)> list2 = new List<(string, string)>();
		int lastInputTokens = session.LastInputTokens;
		int baselineInputTokens = session.BaselineInputTokens;
		int num = ((baselineInputTokens > 0) ? (baselineInputTokens + 45000) : 75000);
		list2.Add(("ONE BRAIN  ·  session " + ((session.Id.Length > 8) ? session.Id.Substring(0, 8) : session.Id) + "  ·  " + SessionAge(session, now), "bold"));
		list2.Add(("Sent to the model: " + K(lastInputTokens) + " tokens", "bold"));
		if (baselineInputTokens > 0)
		{
			list2.Add(("= base " + K(baselineInputTokens) + " (system prompt, tools, memory, first turn)", "dim"));
			list2.Add(("+ conversation " + K(Math.Max(0, lastInputTokens - baselineInputTokens)) + " (turns, replies, screenshots, notes)", "dim"));
		}
		if (baselineInputTokens == 0 && lastInputTokens == 0)
		{
			list2.Add(("Compacts once the conversation reaches " + K(45000) + " on top of the base (measured from the first turn)", "dim"));
		}
		else
		{
			list2.Add(($"Compacts at {K(num)} total{((baselineInputTokens == 0) ? " (base unknown, whole-input fallback)" : "")}  ·  {(session.IsCompacting ? "compacting…" : (K(Math.Max(0, num - lastInputTokens)) + " to go"))}", ""));
		}
		int count = list2.Count;
		double item = (double)Math.Min(lastInputTokens, num) / (double)Math.Max(num, 1);
		list2.Add(($"Thread {session.ThreadItems} items  ·  {session.ScreenshotsInThread} screenshots (~{K(session.ScreenshotsInThread * 1400)})", ""));
		if (session.NextCompactionCheckInputTokens > 0)
		{
			list2.Add(("Worker said too little to fold: next ask at " + K(session.NextCompactionCheckInputTokens) + " total", "faint"));
		}
		list2.Add(("—", "divider"));
		if ((object)turn != null)
		{
			int tokens = Math.Max(0, turn.Input - turn.Cached - turn.Wrote);
			int num2 = ((turn.Input > 0) ? ((int)((double)turn.Cached / (double)turn.Input * 100.0)) : 0);
			list2.Add(($"Last turn ({turn.Kind}, {((turn.Model.Length == 0) ? "…" : turn.Model)})  ·  {Age(turn.SentAt, now)} ago", "bold"));
			if (turn.Input > 0)
			{
				double? idleMinutesBefore = turn.IdleMinutesBefore;
				object obj;
				if (idleMinutesBefore.HasValue)
				{
					double valueOrDefault = idleMinutesBefore.GetValueOrDefault();
					obj = $"{valueOrDefault:0} min since the turn before";
				}
				else
				{
					obj = "first turn";
				}
				string value = (string)obj;
				list2.Add(((num2 >= 50) ? $"Cache HIT {num2}%  ({value})" : $"Cache MISS: {num2}% read, rewrote the prompt  ({value})", (num2 >= 50) ? "hit" : "miss"));
				list2.Add(($"Cache read {K(turn.Cached)}  ·  wrote {K(turn.Wrote)}  ·  fresh {K(tokens)}", ""));
				list2.Add(($"Reasoning {turn.Reasoning}  ·  output {turn.Output}  ·  rounds {turn.Rounds}  ·  ${turn.Cost.ToString("0.000", CultureInfo.InvariantCulture)}", ""));
			}
			list2.Add((TimingLine(turn), ""));
		}
		else
		{
			list2.Add(("No turn yet this session", "faint"));
		}
		if (list.Count > 0)
		{
			list2.Add(("—", "divider"));
			foreach (Event item2 in list)
			{
				list2.Add(($"{item2.At:HH:mm:ss}  {item2.Text}", "event"));
			}
		}
		return (Lines: list2, Progress: item, PastThreshold: lastInputTokens >= num, ProgressAfterLine: count);
	}

	private static string SessionAge(Session s, DateTimeOffset now)
	{
		DateTimeOffset? startedAt = s.StartedAt;
		if (startedAt.HasValue)
		{
			DateTimeOffset valueOrDefault = startedAt.GetValueOrDefault();
			if (!(valueOrDefault <= DateTimeOffset.MinValue))
			{
				return Age(valueOrDefault, now) + " old";
			}
		}
		if (s.Id.Length != 0)
		{
			return "resumed";
		}
		return "no turn yet";
	}

	private static string TimingLine(Turn turn)
	{
		List<string> list = new List<string>();
		DateTimeOffset? keyUpAt = turn.KeyUpAt;
		if (keyUpAt.HasValue)
		{
			DateTimeOffset valueOrDefault = keyUpAt.GetValueOrDefault();
			keyUpAt = turn.SentAt;
			if (keyUpAt.HasValue)
			{
				DateTimeOffset valueOrDefault2 = keyUpAt.GetValueOrDefault();
				list.Add("release→sent " + Seconds((valueOrDefault2 - valueOrDefault).TotalSeconds));
			}
		}
		int? firstTokenMilliseconds = turn.FirstTokenMilliseconds;
		if (firstTokenMilliseconds.HasValue)
		{
			int valueOrDefault3 = firstTokenMilliseconds.GetValueOrDefault();
			list.Add("1st token " + Seconds((double)valueOrDefault3 / 1000.0));
		}
		keyUpAt = turn.FirstHeardAt;
		if (keyUpAt.HasValue)
		{
			DateTimeOffset valueOrDefault4 = keyUpAt.GetValueOrDefault();
			keyUpAt = turn.SentAt;
			if (keyUpAt.HasValue)
			{
				DateTimeOffset valueOrDefault5 = keyUpAt.GetValueOrDefault();
				list.Add("heard " + Seconds((valueOrDefault4 - valueOrDefault5).TotalSeconds));
				keyUpAt = turn.KeyUpAt;
				if (keyUpAt.HasValue)
				{
					DateTimeOffset valueOrDefault6 = keyUpAt.GetValueOrDefault();
					list.Add("TOTAL " + Seconds((valueOrDefault4 - valueOrDefault6).TotalSeconds));
				}
			}
		}
		if (list.Count != 0)
		{
			return string.Join("  ·  ", list);
		}
		return "waiting…";
	}

	internal static string K(int tokens)
	{
		if (tokens < 1000)
		{
			return tokens.ToString(CultureInfo.InvariantCulture);
		}
		return ((double)tokens / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + "k";
	}

	private static string Seconds(double value)
	{
		return value.ToString("0.00", CultureInfo.InvariantCulture) + "s";
	}

	private static string Age(DateTimeOffset? date, DateTimeOffset now)
	{
		if (date.HasValue)
		{
			DateTimeOffset valueOrDefault = date.GetValueOrDefault();
			if (!(valueOrDefault <= DateTimeOffset.MinValue))
			{
				int num = (int)(now - valueOrDefault).TotalSeconds;
				if (num >= 60)
				{
					if (num < 3600)
					{
						return $"{num / 60}m";
					}
					return $"{num / 3600}h{num % 3600 / 60}m";
				}
				return $"{num}s";
			}
		}
		return "–";
	}
}
