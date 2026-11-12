using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkTextContent : IEquatable<ClickyInkTextContent>
{
	public List<ClickyInkRun> Runs { get; } = new List<ClickyInkRun>();

	public int PenRun { get; set; } = -1;

	public ClickyInkPenAttribute? Pen { get; set; }

	public double PenClock { get; set; }

	public string PlainText => string.Concat(Runs.Select((ClickyInkRun r) => r.Text));

	public ClickyInkTextContent()
	{
	}

	public ClickyInkTextContent(IEnumerable<ClickyInkRun> runs)
	{
		Runs.AddRange(runs);
	}

	public static ClickyInkTextContent Plain(string text, Color? color = null)
	{
		return new ClickyInkTextContent(new _003C_003Ez__ReadOnlySingleElementList<ClickyInkRun>(new ClickyInkRun(text, color, IsBold: false, null, null, IsLink: false, null)));
	}

	public static ClickyInkTextContent Symbol(string name, Color? color = null)
	{
		return new ClickyInkTextContent(new _003C_003Ez__ReadOnlySingleElementList<ClickyInkRun>(new ClickyInkRun("", color, IsBold: false, null, null, IsLink: false, null, name)));
	}

	public ClickyInkTextContent Append(ClickyInkTextContent other)
	{
		ClickyInkTextContent clickyInkTextContent = new ClickyInkTextContent(Runs)
		{
			PenRun = PenRun,
			Pen = Pen,
			PenClock = PenClock
		};
		if (other.PenRun >= 0 && PenRun < 0)
		{
			clickyInkTextContent.PenRun = Runs.Count + other.PenRun;
			clickyInkTextContent.Pen = other.Pen;
			clickyInkTextContent.PenClock = other.PenClock;
		}
		clickyInkTextContent.Runs.AddRange(other.Runs);
		return clickyInkTextContent;
	}

	public bool Equals(ClickyInkTextContent? other)
	{
		if (other != null && Runs.SequenceEqual(other.Runs) && PenRun == other.PenRun)
		{
			ClickyInkPenAttribute? pen = Pen;
			ClickyInkPenAttribute? pen2 = other.Pen;
			if (pen.HasValue != pen2.HasValue)
			{
				return false;
			}
			if (!pen.HasValue)
			{
				return true;
			}
			return pen.GetValueOrDefault() == pen2.GetValueOrDefault();
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as ClickyInkTextContent);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Runs.Count, PenRun, Pen);
	}
}
