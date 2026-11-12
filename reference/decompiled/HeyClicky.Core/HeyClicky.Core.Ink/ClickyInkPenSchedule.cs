using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkPenSchedule(IReadOnlyList<InkRange> Busy, IReadOnlyList<double> Arrivals)
{
	private static readonly Dictionary<string, ClickyInkPenSchedule> Cache = new Dictionary<string, ClickyInkPenSchedule>();

	private static (ClickyInkLiveNote Note, ClickyInkPenSchedule Schedule)? last;

	private static readonly object Gate = new object();

	public static ClickyInkPenSchedule Of(ClickyInkLiveNote note)
	{
		lock (Gate)
		{
			(ClickyInkLiveNote, ClickyInkPenSchedule)? tuple = last;
			if (tuple.HasValue)
			{
				(ClickyInkLiveNote, ClickyInkPenSchedule) valueOrDefault = tuple.GetValueOrDefault();
				if ((object)valueOrDefault.Item1 == note)
				{
					return valueOrDefault.Item2;
				}
			}
			ClickyInkPenSchedule clickyInkPenSchedule = Keyed(note);
			last = (note, clickyInkPenSchedule);
			return clickyInkPenSchedule;
		}
	}

	private static ClickyInkPenSchedule Keyed(ClickyInkLiveNote note)
	{
		string key = $"{string.Join(",", note.Pages.Select((ClickyInkLiveNote.Page p) => p.Id))}|{note.VersionCount}|{InkText.DoubleDescription(note.PenFreeAt)}";
		if (Cache.TryGetValue(key, out ClickyInkPenSchedule value))
		{
			return value;
		}
		List<InkRange> list = new List<InkRange>();
		foreach (ClickyInkLiveNote.PenFocus item in from f in note.PenFocusList()
			orderby f.At
			select f)
		{
			if (list.Count > 0)
			{
				if (item.At <= list[list.Count - 1].UpperBound + 0.6)
				{
					list[list.Count - 1] = new InkRange(list[list.Count - 1].LowerBound, Math.Max(list[list.Count - 1].UpperBound, item.Until));
					continue;
				}
			}
			list.Add(new InkRange(item.At, Math.Max(item.At, item.Until)));
		}
		ClickyInkPenSchedule clickyInkPenSchedule = new ClickyInkPenSchedule(list, note.VersionArrivals.ToList());
		if (Cache.Count > 60)
		{
			Cache.Clear();
		}
		Cache[key] = clickyInkPenSchedule;
		return clickyInkPenSchedule;
	}

	public bool IsWriting(double clock)
	{
		int num = LastStartedBy(clock);
		if (num > 0)
		{
			return clock <= Busy[num - 1].UpperBound;
		}
		return false;
	}

	private int LastStartedBy(double clock)
	{
		int num = 0;
		int num2 = Busy.Count;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			if (Busy[num3].LowerBound <= clock)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	public double? IdleSince(double clock)
	{
		int num = LastStartedBy(clock);
		if (num == 0)
		{
			return null;
		}
		double num2 = Busy[num - 1].UpperBound + 0.6;
		if (!(clock >= num2))
		{
			return null;
		}
		return num2;
	}

	public bool LooksWorking(double clock)
	{
		double? num = null;
		foreach (double arrival in Arrivals)
		{
			if (arrival <= clock)
			{
				num = arrival;
			}
		}
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			return clock < valueOrDefault + 120.0;
		}
		return false;
	}

	public double? RestingSince(double clock, bool? isWorking)
	{
		double? num = IdleSince(clock);
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			if (!(isWorking ?? LooksWorking(clock)))
			{
				return null;
			}
			return valueOrDefault;
		}
		return null;
	}

	public bool Equals(ClickyInkPenSchedule? other)
	{
		if ((object)other != null && Busy.SequenceEqual(other.Busy))
		{
			return Arrivals.SequenceEqual(other.Arrivals);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Busy.Count, Arrivals.Count);
	}

	[CompilerGenerated]
	private ClickyInkPenSchedule(ClickyInkPenSchedule original)
	{
		Busy = original.Busy;
		Arrivals = original.Arrivals;
	}
}
