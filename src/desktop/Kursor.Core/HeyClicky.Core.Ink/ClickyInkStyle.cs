using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkStyle
{
	public Color Ink { get; set; }

	public Color Quiet { get; set; }

	public Color Fresh { get; set; } = ClickyBlue;

	public Color Highlighter { get; set; } = Color.FromArgb((byte)Math.Round(140.25), byte.MaxValue, 226, 122);

	public int? LatestEdit { get; set; }

	public ClickyInkPenSchedule? PenSchedule { get; set; }

	public bool? IsWorking { get; set; }

	public static readonly Color ClickyBlue = Color.FromArgb(byte.MaxValue, 45, 91, 227);

	public ClickyInkStyle(Color ink, Color quiet, Color? fresh = null, int? latestEdit = null)
	{
		Ink = ink;
		Quiet = quiet;
		Fresh = fresh ?? ClickyBlue;
		LatestEdit = latestEdit;
	}

	public ClickyInkStyle Resting(ClickyInkLiveNote note, bool? isWorking = null)
	{
		return this with
		{
			PenSchedule = ClickyInkPenSchedule.Of(note),
			IsWorking = isWorking
		};
	}

	public double? RestingSince(double clock)
	{
		return PenSchedule?.RestingSince(clock, IsWorking);
	}

	public bool IsPenResting(double clock)
	{
		return RestingSince(clock).HasValue;
	}

	public static bool LooksTheSame(ClickyInkStyle lhs, ClickyInkStyle rhs)
	{
		if (lhs.Ink == rhs.Ink && lhs.Quiet == rhs.Quiet && lhs.Fresh == rhs.Fresh && lhs.Highlighter == rhs.Highlighter && lhs.LatestEdit == rhs.LatestEdit && lhs.IsWorking == rhs.IsWorking && (lhs.PenSchedule?.Busy ?? Array.Empty<InkRange>()).SequenceEqual(rhs.PenSchedule?.Busy ?? Array.Empty<InkRange>()) && (lhs.PenSchedule?.Arrivals ?? Array.Empty<double>()).SequenceEqual(rhs.PenSchedule?.Arrivals ?? Array.Empty<double>()))
		{
			return (object)lhs.PenSchedule == null == ((object)rhs.PenSchedule == null);
		}
		return false;
	}

	[CompilerGenerated]
	private ClickyInkStyle(ClickyInkStyle original)
	{
		Ink = original.Ink;
		Quiet = original.Quiet;
		Fresh = original.Fresh;
		Highlighter = original.Highlighter;
		LatestEdit = original.LatestEdit;
		PenSchedule = original.PenSchedule;
		IsWorking = original.IsWorking;
	}
}
