using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkRowMotion
{
	public abstract record State
	{
		public sealed record Moving : State;

		public sealed record Settled(double At) : State();

		public sealed record NotYet : State;
	}

	public sealed record Table(IReadOnlyDictionary<string, ClickyInkRowMotion> Places, ClickyInkRowMotion Switches, ClickyInkRowMotion RestingSwitches)
	{
		public string Key { get; set; } = "";
	}

	public IReadOnlyList<InkRange> Windows { get; }

	public const double LeadSeconds = 0.6;

	public const double TailSeconds = 1.2;

	public const double ObjectTailSeconds = 2.2;

	public const double DashboardTailSeconds = 8.0;

	private static readonly Dictionary<string, Table> Cache = new Dictionary<string, Table>();

	private static (ClickyInkLiveNote Note, Table Table)? last;

	private static readonly object Gate = new object();

	public ClickyInkRowMotion(IEnumerable<InkRange> spans, double lead = 0.6, double tail = 1.2)
	{
		IOrderedEnumerable<InkRange> orderedEnumerable = from s in spans
			where double.IsFinite(s.LowerBound) && double.IsFinite(s.UpperBound)
			select new InkRange(s.LowerBound - lead, s.UpperBound + tail) into s
			orderby s.LowerBound
			select s;
		List<InkRange> list = new List<InkRange>();
		foreach (InkRange item in orderedEnumerable)
		{
			if (list.Count > 0)
			{
				if (item.LowerBound <= list[list.Count - 1].UpperBound)
				{
					list[list.Count - 1] = new InkRange(list[list.Count - 1].LowerBound, Math.Max(list[list.Count - 1].UpperBound, item.UpperBound));
					continue;
				}
			}
			list.Add(item);
		}
		Windows = list;
	}

	public static ClickyInkRowMotion SwitchesAt(IEnumerable<double> times)
	{
		return new ClickyInkRowMotion(times.Select((double t) => new InkRange(t, t)), 0.0, 0.0);
	}

	public State StateAt(double clock)
	{
		int num = 0;
		int num2 = Windows.Count;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			if (Windows[num3].LowerBound <= clock)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		if (num == 0)
		{
			return new State.NotYet();
		}
		InkRange inkRange = Windows[num - 1];
		if (!(clock <= inkRange.UpperBound))
		{
			return new State.Settled(inkRange.UpperBound);
		}
		return new State.Moving();
	}

	public static Table TableFor(ClickyInkLiveNote note)
	{
		lock (Gate)
		{
			(ClickyInkLiveNote, Table)? tuple = last;
			if (tuple.HasValue)
			{
				(ClickyInkLiveNote, Table) valueOrDefault = tuple.GetValueOrDefault();
				if ((object)valueOrDefault.Item1 == note)
				{
					return valueOrDefault.Item2;
				}
			}
			string key = $"{string.Join(",", note.Pages.Select((ClickyInkLiveNote.Page p) => p.Id))}|{string.Join(",", note.Pages.Select((ClickyInkLiveNote.Page p) => p.Blocks.Count))}|{note.VersionCount}|{InkText.DoubleDescription(note.PenFreeAt)}|{InkText.DoubleDescription(note.LastVersionAt.GetValueOrDefault())}|{note.EditStarts.Count}|{InkText.DoubleDescription((note.VersionArrivals.Count > 0) ? note.VersionArrivals[0] : 0.0)}";
			if (!Cache.TryGetValue(key, out Table value))
			{
				value = MakeTable(note, ClickyInkPenSchedule.Of(note))with
				{
					Key = key
				};
				if (Cache.Count > 60)
				{
					Cache.Clear();
				}
				Cache[key] = value;
			}
			last = (note, value);
			return value;
		}
	}

	public static Table MakeTable(ClickyInkLiveNote note, ClickyInkPenSchedule schedule)
	{
		Dictionary<string, ClickyInkRowMotion> dictionary = new Dictionary<string, ClickyInkRowMotion>();
		ClickyInkLiveNote.Words title = note.Title;
		if ((object)title != null)
		{
			dictionary["title"] = new ClickyInkRowMotion(Spans(title));
		}
		foreach (ClickyInkLiveNote.Page page in note.Pages)
		{
			List<InkRange> list = Spans(page.Name).ToList();
			list.Add(new InkRange(page.LandsAt, page.LandsAt));
			foreach (ClickyInkLiveNote.Block block in page.Blocks)
			{
				list.Add(new InkRange(block.LandsAt, block.LandsAt));
				double? vanishesAt = block.VanishesAt;
				if (vanishesAt.HasValue)
				{
					double valueOrDefault = vanishesAt.GetValueOrDefault();
					list.Add(new InkRange(valueOrDefault, valueOrDefault));
				}
				dictionary[block.Id] = new ClickyInkRowMotion(Spans(block), 0.6, Tail(block));
			}
			dictionary[ClickyInkLiveNote.PageFocusID(page.Id)] = new ClickyInkRowMotion(list);
		}
		List<double> list2 = schedule.Busy.SelectMany((InkRange b) => new double[2]
		{
			b.LowerBound,
			b.UpperBound + 0.6
		}).ToList();
		list2.AddRange(schedule.Arrivals.SelectMany((double a) => new double[2]
		{
			a,
			a + 120.0
		}));
		return new Table(dictionary, SwitchesAt(note.EditStarts), SwitchesAt(list2));
	}

	public static double? SettledClock(ClickyInkLiveNote.Page page, double clock, Table table)
	{
		double num = double.NegativeInfinity;
		State state = table.Switches.StateAt(clock);
		if (!(state is State.Moving))
		{
			if (state is State.Settled settled)
			{
				num = settled.At;
			}
			foreach (ClickyInkLiveNote.Block block in page.Blocks)
			{
				if (!table.Places.TryGetValue(block.Id, out ClickyInkRowMotion value))
				{
					return null;
				}
				state = value.StateAt(clock);
				if (!(state is State.Moving))
				{
					if (state is State.Settled settled2)
					{
						num = Math.Max(num, settled2.At);
					}
					continue;
				}
				return null;
			}
			if (!double.IsFinite(num))
			{
				return null;
			}
			return num;
		}
		return null;
	}

	public static double Tail(ClickyInkLiveNote.Block block)
	{
		HashSet<ClickyInkDocument.Kind> hashSet = block.Stamps.Select<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>, ClickyInkDocument.Kind>((ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> s) => s.Value.Kind).ToHashSet();
		HashSet<ClickyInkDocument.Kind> other = new HashSet<ClickyInkDocument.Kind>(ClickyInkDashboardKinds.DashboardKinds)
		{
			ClickyInkDocument.Kind.Figure,
			ClickyInkDocument.Kind.Tiles,
			ClickyInkDocument.Kind.Drawing
		};
		if (hashSet.Overlaps(other))
		{
			return 8.0;
		}
		HashSet<ClickyInkDocument.Kind> other2 = new HashSet<ClickyInkDocument.Kind>
		{
			ClickyInkDocument.Kind.Heading,
			ClickyInkDocument.Kind.Paragraph,
			ClickyInkDocument.Kind.Quote,
			ClickyInkDocument.Kind.Note,
			ClickyInkDocument.Kind.List,
			ClickyInkDocument.Kind.Checklist,
			ClickyInkDocument.Kind.Numbered,
			ClickyInkDocument.Kind.Answer,
			ClickyInkDocument.Kind.Code,
			ClickyInkDocument.Kind.Drawing,
			ClickyInkDocument.Kind.Rule,
			ClickyInkDocument.Kind.Unknown
		};
		if (!hashSet.IsSubsetOf(other2))
		{
			return 2.2;
		}
		return 1.2;
	}

	public static List<InkRange> Spans(ClickyInkLiveNote.Block block)
	{
		List<InkRange> list = Spans(block.Words).ToList();
		List<double> list2 = new List<double> { block.LandsAt };
		double? goneAt = block.GoneAt;
		if (goneAt.HasValue)
		{
			double valueOrDefault = goneAt.GetValueOrDefault();
			list2.Add(valueOrDefault);
		}
		goneAt = block.VanishesAt;
		if (goneAt.HasValue)
		{
			double valueOrDefault2 = goneAt.GetValueOrDefault();
			list2.Add(valueOrDefault2);
		}
		list2.AddRange(block.Stamps.Select<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>, double>((ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> s) => s.At));
		list2.AddRange(block.PenArrivals);
		foreach (ClickyInkLiveNote.Item item in block.Items)
		{
			list2.Add(item.LandsAt);
			goneAt = item.GoneAt;
			if (goneAt.HasValue)
			{
				double valueOrDefault3 = goneAt.GetValueOrDefault();
				list2.Add(valueOrDefault3);
			}
			goneAt = item.VanishesAt;
			if (goneAt.HasValue)
			{
				double valueOrDefault4 = goneAt.GetValueOrDefault();
				list2.Add(valueOrDefault4);
			}
			list2.AddRange(item.Stamps.Select<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>, double>((ClickyInkLiveNote.Stamp<ClickyInkDocument.Item> s) => s.At));
			list.AddRange(Spans(item.Words));
			ClickyInkLiveNote.Words detail = item.Detail;
			if ((object)detail != null)
			{
				list.AddRange(Spans(detail));
			}
			foreach (ClickyInkLiveNote.Words cell in item.Cells)
			{
				list.AddRange(Spans(cell));
			}
		}
		list.AddRange(list2.Select((double m) => new InkRange(m, m)));
		return list;
	}

	public static IEnumerable<InkRange> Spans(ClickyInkLiveNote.Words words)
	{
		foreach (ClickyInkLiveNote.Segment segment in words.Segments)
		{
			if (segment.Written.HasValue)
			{
				yield return segment.Written.GetValueOrDefault();
			}
			if (segment.Struck.HasValue)
			{
				yield return segment.Struck.GetValueOrDefault();
			}
			if (segment.VanishesAt.HasValue)
			{
				double vanishesAt = segment.VanishesAt.GetValueOrDefault();
				yield return new InkRange(vanishesAt, vanishesAt);
			}
		}
	}

	public bool Equals(ClickyInkRowMotion? other)
	{
		if ((object)other != null)
		{
			return Windows.SequenceEqual(other.Windows);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Windows.Count;
	}

	[CompilerGenerated]
	private ClickyInkRowMotion(ClickyInkRowMotion original)
	{
		Windows = original.Windows;
	}
}
