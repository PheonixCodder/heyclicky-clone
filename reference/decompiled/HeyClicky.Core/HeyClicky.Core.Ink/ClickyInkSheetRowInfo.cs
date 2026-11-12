using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public abstract record ClickyInkSheetRowInfo
{
	public sealed record Background : ClickyInkSheetRowInfo;

	public sealed record Row(IReadOnlyList<string> PlaceIDs, IReadOnlyDictionary<string, double> ItemFractions, IReadOnlyList<string> CollapsedAbove, IReadOnlyList<string> CollapsedBelow) : ClickyInkSheetRowInfo()
	{
		public Row(IReadOnlyList<string> placeIDs, IReadOnlyDictionary<string, double> itemFractions)
			: this(placeIDs, itemFractions, Array.Empty<string>(), Array.Empty<string>())
		{
		}

		public bool Equals(Row? other)
		{
			if ((object)other != null && PlaceIDs.SequenceEqual(other.PlaceIDs) && ItemFractions.Count == other.ItemFractions.Count && ItemFractions.All<KeyValuePair<string, double>>((KeyValuePair<string, double> kv) => other.ItemFractions.TryGetValue(kv.Key, out var value) && value == kv.Value) && CollapsedAbove.SequenceEqual(other.CollapsedAbove))
			{
				return CollapsedBelow.SequenceEqual(other.CollapsedBelow);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(PlaceIDs.Count, ItemFractions.Count, CollapsedAbove.Count, CollapsedBelow.Count);
		}
	}

	public sealed record Tail : ClickyInkSheetRowInfo;

	public sealed record PenMarker : ClickyInkSheetRowInfo;

	public sealed record FollowMarker : ClickyInkSheetRowInfo;
}
