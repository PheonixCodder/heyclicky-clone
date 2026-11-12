using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkActionFeedback
{
	public ClickyInkSentAction? Pending { get; set; }

	public IReadOnlyDictionary<string, ClickyInkSentAction> Answered { get; set; } = new Dictionary<string, ClickyInkSentAction>();

	public bool IsLocked => (object)Pending != null;

	public const double SteerPickupSeconds = 5.0;

	public const double PendingGivesUpSeconds = 900.0;

	public static ClickyInkActionFeedback Make(IReadOnlyList<ClickyInkSentAction> sent, DateTimeOffset? lastVersionAt, bool isRunning, DateTimeOffset? finishedAt, IReadOnlyList<DateTimeOffset>? editStartedAt = null, DateTimeOffset? now = null)
	{
		IReadOnlyList<DateTimeOffset> editStartedAt2 = editStartedAt ?? Array.Empty<DateTimeOffset>();
		DateTimeOffset dateTimeOffset = now ?? DateTimeOffset.Now;
		Dictionary<string, ClickyInkSentAction> dictionary = new Dictionary<string, ClickyInkSentAction>();
		List<ClickyInkSentAction> list = sent.OrderBy((ClickyInkSentAction a) => a.SentAt).ToList();
		foreach (ClickyInkSentAction item in list)
		{
			if (item.Kind != 0 && IsStillCurrent(item, editStartedAt2))
			{
				dictionary[item.ObjectKey] = item;
			}
		}
		ClickyInkSentAction pending = null;
		if (list.Count > 0)
		{
			ClickyInkSentAction clickyInkSentAction = list[list.Count - 1];
			bool num = (dateTimeOffset - clickyInkSentAction.SentAt).TotalSeconds > 900.0;
			double num2 = (clickyInkSentAction.WasSentWhileRunning ? 5.0 : 0.0);
			int num3;
			if (lastVersionAt.HasValue)
			{
				DateTimeOffset valueOrDefault = lastVersionAt.GetValueOrDefault();
				num3 = (((valueOrDefault - clickyInkSentAction.SentAt).TotalSeconds > num2) ? 1 : 0);
			}
			else
			{
				num3 = 0;
			}
			bool flag = (byte)num3 != 0;
			int num4;
			if (!isRunning && finishedAt.HasValue)
			{
				DateTimeOffset valueOrDefault2 = finishedAt.GetValueOrDefault();
				num4 = ((valueOrDefault2 > clickyInkSentAction.SentAt) ? 1 : 0);
			}
			else
			{
				num4 = 0;
			}
			bool flag2 = (byte)num4 != 0;
			if (!num && !flag && !flag2)
			{
				pending = clickyInkSentAction;
			}
		}
		return new ClickyInkActionFeedback
		{
			Pending = pending,
			Answered = dictionary
		};
	}

	public static bool IsStillCurrent(ClickyInkSentAction action, IReadOnlyList<DateTimeOffset> editStartedAt)
	{
		return editStartedAt.Count((DateTimeOffset d) => d > action.SentAt) <= 1;
	}

	public ClickyInkSentAction? AnswerFor(ClickyInkDocument.Block block)
	{
		if (!Answered.TryGetValue(ClickyInkObjectKey.Of(block), out ClickyInkSentAction value))
		{
			return null;
		}
		return value;
	}

	public bool IsPending(ClickyInkDocument.Block block)
	{
		return Pending?.ObjectKey == ClickyInkObjectKey.Of(block);
	}

	public bool Equals(ClickyInkActionFeedback? other)
	{
		ClickyInkSentAction value;
		if ((object)other != null && object.Equals(Pending, other.Pending) && Answered.Count == other.Answered.Count)
		{
			return Answered.All<KeyValuePair<string, ClickyInkSentAction>>((KeyValuePair<string, ClickyInkSentAction> kv) => other.Answered.TryGetValue(kv.Key, out value) && value.Equals(kv.Value));
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Pending, Answered.Count);
	}

	[CompilerGenerated]
	private ClickyInkActionFeedback(ClickyInkActionFeedback original)
	{
		Pending = original.Pending;
		Answered = original.Answered;
	}

	public ClickyInkActionFeedback()
	{
	}
}
