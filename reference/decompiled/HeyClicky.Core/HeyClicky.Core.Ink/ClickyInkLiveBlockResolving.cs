using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Ink;

public static class ClickyInkLiveBlockResolving
{
	public static ClickyInkLiveNote.Block Resolved(this ClickyInkLiveNote.Block block, double clock)
	{
		ClickyInkLiveNote.Block block2 = block.Clone();
		ClickyInkDocument.Block block3 = block.SourceAt(clock);
		ClickyInkDocument.Block block4 = block.PreviousSource(clock);
		if ((object)block4 != null)
		{
			string text = block4["big"] ?? block4["value"];
			if (text != null && text != (block3["big"] ?? block3["value"]))
			{
				block3 = block3.Clone();
				block3.Attributes["_was"] = text;
			}
		}
		List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> stamps = block.Stamps;
		int num = stamps.FindLastIndex((ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> s) => s.At <= clock);
		if (num >= 0)
		{
			int first;
			for (first = num; first > 0 && Number(stamps[first - 1].Value) == Number(stamps[num].Value); first--)
			{
			}
			block2.NumberShownSince = stamps[first].At;
			double nextStamp = ((first + 1 < stamps.Count) ? stamps[first + 1].At : double.PositiveInfinity);
			List<double> list = (from s in block.Words.Segments.Where(delegate(ClickyInkLiveNote.Segment s)
				{
					InkRange? written = s.Written;
					if (written.HasValue)
					{
						InkRange valueOrDefault = written.GetValueOrDefault();
						if (valueOrDefault.LowerBound >= stamps[first].At - 0.001)
						{
							return valueOrDefault.LowerBound < nextStamp;
						}
					}
					return false;
				})
				select s.Written.Value.UpperBound).ToList();
			block2.NumberLineWrittenAt = ((list.Count > 0) ? list.Max() : (stamps[first].At + 0.45));
		}
		int num2 = 1;
		List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> list2 = new List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		Span<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> span = CollectionsMarshal.AsSpan(list2);
		int index = 0;
		span[index] = new ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>(EffectiveSince(stamps, clock), block3, null);
		block2.Stamps = list2;
		double value = double.PositiveInfinity;
		foreach (ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> item3 in stamps)
		{
			if (item3.At > clock)
			{
				value = item3.At;
				break;
			}
		}
		block2.Words = block.Words with
		{
			AsOf = clock,
			ReservedUntil = value
		};
		block2.Items = block.Items.Where((ClickyInkLiveNote.Item i) => i.IsPresent(clock)).Select(delegate(ClickyInkLiveNote.Item item)
		{
			ClickyInkLiveNote.Item item2 = item.Clone();
			List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>> list3 = item.Stamps.Where<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>>((ClickyInkLiveNote.Stamp<ClickyInkDocument.Item> s) => s.At <= clock).ToList();
			List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>> list4;
			if (list3.Count != 0)
			{
				list4 = list3;
			}
			else
			{
				int num3 = 1;
				list4 = new List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>>(num3);
				CollectionsMarshal.SetCount(list4, num3);
				Span<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>> span2 = CollectionsMarshal.AsSpan(list4);
				int index2 = 0;
				span2[index2] = item.Stamps[0];
			}
			item2.Stamps = list4;
			double passEnds = double.PositiveInfinity;
			foreach (ClickyInkLiveNote.Stamp<ClickyInkDocument.Item> stamp in item.Stamps)
			{
				if (stamp.At > clock)
				{
					passEnds = stamp.At;
					break;
				}
			}
			item2.Words = item.Words with
			{
				AsOf = clock,
				ReservedUntil = passEnds
			};
			item2.Detail = (((object)item.Detail == null) ? null : item.Detail with
			{
				AsOf = clock,
				ReservedUntil = passEnds
			});
			item2.Cells = item.Cells.Select((ClickyInkLiveNote.Words cell) => cell with
			{
				AsOf = clock,
				ReservedUntil = passEnds
			}).ToList();
			return item2;
		}).ToList();
		return block2;
		static string? Number(ClickyInkDocument.Block b)
		{
			return b["big"] ?? b["value"];
		}
	}

	public static double EffectiveSince<TValue>(IReadOnlyList<ClickyInkLiveNote.Stamp<TValue>> stamps, double clock)
	{
		for (int num = stamps.Count - 1; num >= 0; num--)
		{
			if (stamps[num].At <= clock)
			{
				return stamps[num].At;
			}
		}
		return stamps[0].At;
	}

	public static double ShownSince(this ClickyInkLiveNote.Block block)
	{
		if (block.Stamps.Count <= 0)
		{
			return block.LandsAt;
		}
		List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> stamps = block.Stamps;
		return stamps[stamps.Count - 1].At;
	}

	public static ClickyInkDocument.Block? PreviousSource(this ClickyInkLiveNote.Block block, double clock)
	{
		List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> stamps = block.Stamps;
		int num = stamps.FindLastIndex((ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> s) => s.At <= clock);
		if (num > 0)
		{
			int? edit = stamps[num].Edit;
			if (edit.HasValue)
			{
				int valueOrDefault = edit.GetValueOrDefault();
				int num2 = num - 1;
				while (num2 > 0 && stamps[num2].Edit == valueOrDefault)
				{
					num2--;
				}
				return stamps[num2].Value;
			}
		}
		return null;
	}
}
