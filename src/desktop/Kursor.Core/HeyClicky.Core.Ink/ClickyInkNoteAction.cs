using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkNoteAction(ClickyInkNoteAction.ActionKind Kind, string ObjectKey, string Message, string LaunchContext, string MarginWords, string ClickyLine, IReadOnlyList<string>? ReviewedItems = null)
{
	public enum ActionKind
	{
		Ask,
		Answer,
		Send,
		Review
	}

	public string AnalyticsAction { get; set; } = "";

	public string? AnalyticsMethod { get; set; }

	public IReadOnlyDictionary<string, int> AnalyticsCounts { get; set; } = new Dictionary<string, int>();

	public string AnalyticsObject
	{
		get
		{
			string text = ObjectKey.Split('|', 2)[0];
			if (text == null || text.Length <= 0)
			{
				return "unknown";
			}
			return text;
		}
	}

	public ClickyInkNoteAction ForAnalytics(string action, string? method = null, IReadOnlyDictionary<string, int>? counts = null)
	{
		return this with
		{
			AnalyticsAction = action,
			AnalyticsMethod = method,
			AnalyticsCounts = (counts ?? new Dictionary<string, int>())
		};
	}

	public bool Equals(ClickyInkNoteAction? other)
	{
		int value;
		if ((object)other != null && Kind == other.Kind && ObjectKey == other.ObjectKey && Message == other.Message && LaunchContext == other.LaunchContext && MarginWords == other.MarginWords && ClickyLine == other.ClickyLine && SameItems(ReviewedItems, other.ReviewedItems) && AnalyticsAction == other.AnalyticsAction && AnalyticsMethod == other.AnalyticsMethod && AnalyticsCounts.Count == other.AnalyticsCounts.Count)
		{
			return AnalyticsCounts.All<KeyValuePair<string, int>>((KeyValuePair<string, int> kv) => other.AnalyticsCounts.TryGetValue(kv.Key, out value) && value == kv.Value);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Kind, ObjectKey, Message);
	}

	internal static bool SameItems(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
	{
		if (a != null)
		{
			if (b != null)
			{
				return a.SequenceEqual(b);
			}
			return false;
		}
		return b == null;
	}
}
