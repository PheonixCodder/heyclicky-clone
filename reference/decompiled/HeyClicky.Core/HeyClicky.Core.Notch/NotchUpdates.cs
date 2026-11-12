using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Notch;

public sealed class NotchUpdates
{
	public static readonly TimeSpan AutoDismiss = TimeSpan.FromSeconds(12L);

	public static readonly TimeSpan BeatBetweenCards = TimeSpan.FromSeconds(0.45);

	private readonly object gate = new object();

	private readonly Func<DateTimeOffset> clock;

	private readonly Timer? timer;

	private readonly List<NotchUpdate> waiting = new List<NotchUpdate>();

	private NotchUpdate? current;

	private TimeSpan? remaining;

	private DateTimeOffset lastTick;

	private DateTimeOffset? nextCardAt;

	private bool isShowing;

	private bool isHovered;

	private bool ticking;

	private string? shownTrackedKey;

	private DateTimeOffset? shownAt;

	private string? shownSubject;

	private readonly List<(string RailKey, string? JobId, JsonObject Properties)> pendingAnalytics = new List<(string, string, JsonObject)>();

	public NotchUpdate? Current
	{
		get
		{
			lock (gate)
			{
				return current;
			}
		}
	}

	public TimeSpan? RemainingTime
	{
		get
		{
			lock (gate)
			{
				return remaining;
			}
		}
	}

	public int WaitingCount
	{
		get
		{
			lock (gate)
			{
				return waiting.Count;
			}
		}
	}

	public Func<string, string?>? JobThreadId { get; set; }

	public event Action? CurrentChanged;

	public event Action? RemainingChanged;

	public NotchUpdates(Func<DateTimeOffset>? clock = null, bool autoTick = true)
	{
		this.clock = clock ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.UtcNow));
		lastTick = this.clock();
		if (autoTick)
		{
			timer = new Timer(delegate
			{
				Tick();
			}, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
		}
	}

	public void Present(string railKey, NotchUpdateKind kind)
	{
		bool flag;
		lock (gate)
		{
			Log.Info("NotchUpdates", $"present {railKey} kind={kind.RawValue()} current={current?.RailKey ?? "nil"} waiting={waiting.Count}");
			NotchUpdate item = new NotchUpdate(railKey, kind);
			waiting.RemoveAll((NotchUpdate w) => w.RailKey == railKey);
			if (current?.RailKey == railKey || (object)current == null)
			{
				SetCurrent(item);
				flag = true;
			}
			else
			{
				if (kind == NotchUpdateKind.Done)
				{
					waiting.Add(item);
				}
				else
				{
					int num = waiting.FindIndex((NotchUpdate w) => w.Kind == NotchUpdateKind.Done);
					waiting.Insert((num < 0) ? waiting.Count : num, item);
				}
				flag = false;
			}
		}
		if (flag)
		{
			Raise();
		}
	}

	public void DismissCurrent()
	{
		lock (gate)
		{
			if ((object)current == null)
			{
				return;
			}
			Log.Info("NotchUpdates", "dismissCurrent " + current.RailKey);
			DismissCurrentCore();
		}
		Raise();
	}

	private void DismissCurrentCore()
	{
		if (waiting.Count > 0)
		{
			nextCardAt = clock() + BeatBetweenCards;
		}
		SetCurrent(null);
	}

	public void Dismiss(string railKey)
	{
		bool flag = false;
		lock (gate)
		{
			waiting.RemoveAll((NotchUpdate w) => w.RailKey == railKey);
			if (current?.RailKey == railKey)
			{
				DismissCurrentCore();
				flag = true;
			}
		}
		if (flag)
		{
			Raise();
		}
	}

	public void Reset()
	{
		lock (gate)
		{
			Log.Info("NotchUpdates", "reset");
			waiting.Clear();
			nextCardAt = null;
			SetCurrent(null);
		}
		Raise();
	}

	public void NoteShowing(bool showing, bool hovered)
	{
		lock (gate)
		{
			isShowing = showing;
			isHovered = hovered;
			lastTick = clock();
		}
	}

	private void SetCurrent(NotchUpdate? update)
	{
		if ((((object)update == null) ? null : AnalyticsKey(update)) != shownTrackedKey)
		{
			shownTrackedKey = null;
			shownAt = null;
			shownSubject = null;
		}
		current = update;
		lastTick = clock();
		remaining = (((object)update != null && update.ClosesOnItsOwn) ? new TimeSpan?(AutoDismiss) : ((TimeSpan?)null));
		UpdateTimer();
	}

	private void UpdateTimer()
	{
		if (timer == null)
		{
			return;
		}
		TimeSpan? timeSpan = remaining;
		int num;
		if (!timeSpan.HasValue)
		{
			DateTimeOffset? dateTimeOffset = nextCardAt;
			num = (dateTimeOffset.HasValue ? 1 : 0);
		}
		else
		{
			num = 1;
		}
		bool flag = (byte)num != 0;
		if (flag != ticking)
		{
			ticking = flag;
			if (flag)
			{
				timer.Change(TimeSpan.FromMilliseconds(100L, 0L), TimeSpan.FromMilliseconds(100L, 0L));
			}
			else
			{
				timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
			}
		}
	}

	public void Tick()
	{
		bool flag = false;
		bool flag2 = false;
		lock (gate)
		{
			DateTimeOffset dateTimeOffset = clock();
			TimeSpan timeSpan = dateTimeOffset - lastTick;
			lastTick = dateTimeOffset;
			if (timeSpan < TimeSpan.Zero)
			{
				timeSpan = TimeSpan.Zero;
			}
			if ((object)current != null)
			{
				goto IL_00c1;
			}
			DateTimeOffset? dateTimeOffset2 = nextCardAt;
			if (!dateTimeOffset2.HasValue)
			{
				goto IL_00c1;
			}
			DateTimeOffset valueOrDefault = dateTimeOffset2.GetValueOrDefault();
			if (!(dateTimeOffset >= valueOrDefault))
			{
				goto IL_00c1;
			}
			nextCardAt = null;
			if (waiting.Count > 0)
			{
				NotchUpdate notchUpdate = waiting[0];
				waiting.RemoveAt(0);
				SetCurrent(notchUpdate);
				flag = true;
			}
			UpdateTimer();
			goto end_IL_000d;
			IL_00c1:
			if ((object)current != null)
			{
				TimeSpan? timeSpan2 = remaining;
				if (timeSpan2.HasValue)
				{
					TimeSpan valueOrDefault2 = timeSpan2.GetValueOrDefault();
					if (isShowing && !isHovered)
					{
						TimeSpan timeSpan3 = valueOrDefault2 - timeSpan;
						if (timeSpan3 <= TimeSpan.Zero)
						{
							TrackCore("auto_closed", null);
							DismissCurrentCore();
							flag = true;
						}
						else
						{
							remaining = timeSpan3;
							flag2 = true;
						}
					}
				}
			}
			end_IL_000d:;
		}
		FlushAnalytics();
		if (flag)
		{
			Raise();
		}
		else if (flag2)
		{
			this.RemainingChanged?.Invoke();
		}
	}

	public static string? JobIdFromRailKey(string railKey)
	{
		if (!railKey.StartsWith("job:", StringComparison.Ordinal))
		{
			return null;
		}
		return railKey.Substring(4, railKey.Length - 4);
	}

	private static string AnalyticsKey(NotchUpdate update)
	{
		return update.RailKey + "|" + update.Kind.AnalyticsName();
	}

	public void TrackShownIfNeeded(string subject, JsonObject properties)
	{
		lock (gate)
		{
			if ((object)current == null)
			{
				return;
			}
			string text = AnalyticsKey(current);
			if (text == shownTrackedKey)
			{
				return;
			}
			shownTrackedKey = text;
			shownAt = clock();
			shownSubject = subject;
			JsonObject jsonObject = properties.DeepClone().AsObject();
			jsonObject["waiting_count"] = waiting.Count;
			TrackCore("shown", jsonObject);
		}
		FlushAnalytics();
	}

	public void Track(string action, JsonObject? properties = null)
	{
		lock (gate)
		{
			TrackCore(action, properties);
		}
		FlushAnalytics();
	}

	private void TrackCore(string action, JsonObject? extra)
	{
		if ((object)current == null || (action != "shown" && shownTrackedKey == null))
		{
			return;
		}
		string text = JobIdFromRailKey(current.RailKey);
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = action,
			["kind"] = current.Kind.AnalyticsName(),
			["subject"] = shownSubject ?? ((text == null) ? "clicky" : "job")
		};
		if (action != "shown")
		{
			DateTimeOffset? dateTimeOffset = shownAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				jsonObject["visible_seconds"] = NotchChatAnalytics.Tenths(Math.Max(0.0, (clock() - valueOrDefault).TotalSeconds));
			}
		}
		if (extra != null)
		{
			foreach (KeyValuePair<string, JsonNode> item in extra)
			{
				item.Deconstruct(out var key, out var value);
				string propertyName = key;
				jsonObject[propertyName] = value?.DeepClone();
			}
		}
		pendingAnalytics.Add((current.RailKey, text, jsonObject));
	}

	private void FlushAnalytics()
	{
		(string, string, JsonObject)[] array;
		lock (gate)
		{
			if (pendingAnalytics.Count == 0)
			{
				return;
			}
			array = pendingAnalytics.ToArray();
			pendingAnalytics.Clear();
		}
		(string, string, JsonObject)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			var (text, text2, properties) = array2[i];
			ClickyAnalytics.TrackProductEvent("notch_update_card_action", (text2 == null) ? text : null, (text2 == null) ? null : JobThreadId?.Invoke(text2), properties);
		}
	}

	private void Raise()
	{
		try
		{
			this.CurrentChanged?.Invoke();
		}
		catch (Exception ex)
		{
			Log.Warn("NotchUpdates", "CurrentChanged handler threw: " + ex.Message);
		}
		this.RemainingChanged?.Invoke();
	}
}
