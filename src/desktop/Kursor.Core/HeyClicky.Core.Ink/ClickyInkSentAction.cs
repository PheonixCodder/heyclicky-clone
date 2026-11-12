using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkSentAction(ClickyInkNoteAction.ActionKind Kind, string ObjectKey, string MarginWords, DateTimeOffset SentAt, bool WasSentWhileRunning, IReadOnlyList<string>? ReviewedItems)
{
	public ClickyInkSentAction(ClickyInkNoteAction action, DateTimeOffset sentAt, bool wasSentWhileRunning)
		: this(action.Kind, action.ObjectKey, action.MarginWords, sentAt, wasSentWhileRunning, action.ReviewedItems)
	{
	}

	public bool Equals(ClickyInkSentAction? other)
	{
		if ((object)other != null && Kind == other.Kind && ObjectKey == other.ObjectKey && MarginWords == other.MarginWords && SentAt == other.SentAt && WasSentWhileRunning == other.WasSentWhileRunning)
		{
			return ClickyInkNoteAction.SameItems(ReviewedItems, other.ReviewedItems);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Kind, ObjectKey, SentAt);
	}
}
