using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkSheetCamera
{
	public sealed record Place(double Top, double Height, IReadOnlyDictionary<string, double> ItemFractions);

	public const double TopBand = 0.1;

	public const double BottomBand = 0.68;

	public const double RunwayFraction = 0.32;

	public const double ShortestGlideSeconds = 0.8;

	public const double LongestGlideSeconds = 1.6;

	public const double GlideSecondsPerPoint = 0.0008333333333333334;

	public const double BriefestVisitSeconds = 0.6;

	public const double BriefVisitReturnFraction = 0.15;

	public const double AnticipationSeconds = 0.7;

	public const double AnticipateFromFraction = 0.8;

	public const double AnticipateFullFraction = 0.95;

	public const double AnticipateUntilFraction = 1.5;

	public const double AnticipationKeepsLineBelowFraction = 0.12;

	private static readonly Dictionary<string, List<ClickyInkLiveNote.PenFocus>> Cache = new Dictionary<string, List<ClickyInkLiveNote.PenFocus>>();

	private static (ClickyInkLiveNote Note, List<ClickyInkLiveNote.PenFocus> Focus)? last;

	private static readonly object Gate = new object();

	public static List<ClickyInkLiveNote.PenFocus> FocusOf(ClickyInkLiveNote note)
	{
		lock (Gate)
		{
			(ClickyInkLiveNote, List<ClickyInkLiveNote.PenFocus>)? tuple = last;
			if (tuple.HasValue)
			{
				(ClickyInkLiveNote, List<ClickyInkLiveNote.PenFocus>) valueOrDefault = tuple.GetValueOrDefault();
				if ((object)valueOrDefault.Item1 == note)
				{
					return valueOrDefault.Item2;
				}
			}
			string key = $"{string.Join(",", note.Pages.Select((ClickyInkLiveNote.Page p) => p.Id))}|{note.VersionCount}|{InkText.DoubleDescription(note.PenFreeAt)}|" + InkText.DoubleDescription(note.LastVersionAt.GetValueOrDefault());
			if (!Cache.TryGetValue(key, out List<ClickyInkLiveNote.PenFocus> value))
			{
				value = note.PenFocusList();
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

	public static double Runway(double contentHeight, double viewportHeight, double full = 1.0)
	{
		double t = (contentHeight - 0.7 * viewportHeight) / (0.3 * viewportHeight);
		return 0.32 * viewportHeight * ClickyInkClock.EaseInOut(t) * full;
	}

	public static double? PenLine(IReadOnlyList<ClickyInkLiveNote.PenFocus> focus, IReadOnlyDictionary<string, Place> places, double clock)
	{
		for (int num = focus.Count - 1; num >= 0; num--)
		{
			ClickyInkLiveNote.PenFocus entry = focus[num];
			if (!(entry.At > clock) && places.TryGetValue(entry.PlaceID, out Place value))
			{
				return value.Top + value.Height * Fraction(value, entry);
			}
		}
		return null;
	}

	private static double Fraction(Place place, ClickyInkLiveNote.PenFocus entry)
	{
		string itemID = entry.ItemID;
		if (itemID == null || !place.ItemFractions.TryGetValue(itemID, out var value))
		{
			return 1.0;
		}
		return value;
	}

	public static double Offset(IReadOnlyList<ClickyInkLiveNote.PenFocus> focus, IReadOnlyDictionary<string, Place> places, double clock, double viewportHeight, double maximumOffset)
	{
		if (!(maximumOffset > 0.0))
		{
			return 0.0;
		}
		double num = 0.0;
		double num2 = 0.0;
		(double, double)? tuple = null;
		for (int i = 0; i < focus.Count; i++)
		{
			ClickyInkLiveNote.PenFocus entry2 = focus[i];
			if (!(entry2.At - 0.7 <= clock))
			{
				break;
			}
			double? num3 = Target(num, entry2);
			if (!num3.HasValue)
			{
				continue;
			}
			double valueOrDefault = num3.GetValueOrDefault();
			num3 = Line(entry2);
			if (!num3.HasValue)
			{
				continue;
			}
			double valueOrDefault2 = num3.GetValueOrDefault();
			double num4 = valueOrDefault;
			if (i + 1 < focus.Count && focus[i + 1].PlaceID != entry2.PlaceID)
			{
				num3 = Target(num, focus[i + 1]);
				if (num3.HasValue)
				{
					double valueOrDefault3 = num3.GetValueOrDefault();
					double clock2 = focus[i + 1].At - entry2.At;
					double num5 = 1.0 - ClickyInkClock.Progress(clock2, 0.3, 0.6);
					double t = (Math.Abs(valueOrDefault3 - num) - 0.15 * viewportHeight) / (0.25 * viewportHeight);
					double num6 = 1.0 - ClickyInkClock.EaseInOut(t);
					num4 += (num - num4) * (num5 * num6);
				}
			}
			double num7;
			if (!entry2.IsLanding && tuple.HasValue)
			{
				(double, double) valueOrDefault4 = tuple.GetValueOrDefault();
				num7 = Anticipation(valueOrDefault2, valueOrDefault4.Item1, num, num4, viewportHeight, entry2.At - valueOrDefault4.Item2);
			}
			else
			{
				num7 = 0.0;
			}
			double num8 = num7;
			double num9 = Math.Max(entry2.At - num8, Math.Min(entry2.At, entry2.KnownSince));
			if (!(num9 <= clock))
			{
				break;
			}
			double num10 = Math.Abs(num4 - num2);
			double num11 = Math.Min(1.6, Math.Max(0.8, 0.8 + num10 * 0.0008333333333333334));
			double t2 = ClickyInkClock.Progress(clock, num9, num9 + num11);
			num2 += (num4 - num2) * SmootherStep(t2);
			num = num4;
			tuple = (valueOrDefault2, entry2.At);
		}
		return Math.Min(maximumOffset, Math.Max(0.0, num2));
		double? Line(ClickyInkLiveNote.PenFocus entry)
		{
			if (!places.TryGetValue(entry.PlaceID, out Place value2))
			{
				return null;
			}
			return value2.Top + value2.Height * Fraction(value2, entry);
		}
		double? Target(double settled, ClickyInkLiveNote.PenFocus entry)
		{
			if (!places.TryGetValue(entry.PlaceID, out Place value))
			{
				return null;
			}
			double num12 = value.Top + value.Height * Fraction(value, entry) - 0.68 * viewportHeight;
			double num13 = value.Top - 0.1 * viewportHeight;
			double num14 = settled;
			if (num14 > num13)
			{
				num14 = num13;
			}
			if (num14 < num12)
			{
				num14 = num12;
			}
			return Math.Min(maximumOffset, Math.Max(0.0, num14));
		}
	}

	public static double Anticipation(double newLine, double previousLine, double settled, double wanted, double viewportHeight, double room)
	{
		double num = wanted - settled;
		if (!(num > 0.5) || !(viewportHeight > 0.0) || !(room > 0.0))
		{
			return 0.0;
		}
		double num2 = (newLine - settled) / viewportHeight;
		double num3 = ClickyInkClock.EaseInOut((num2 - 0.8) / 0.1499999999999999) * (1.0 - ClickyInkClock.EaseInOut((num2 - 1.5) / 0.25 + 1.0));
		if (!(num3 > 0.0))
		{
			return 0.0;
		}
		double num4 = ((previousLine - settled) / viewportHeight - 0.12) * viewportHeight / num;
		if (!(num4 > 0.0))
		{
			return 0.0;
		}
		double num5 = Math.Min(1.6, Math.Max(0.8, 0.8 + num * 0.0008333333333333334));
		double val = 0.7 * num3;
		if (num4 < 1.0)
		{
			val = Math.Min(val, num5 * InverseSmootherStep(num4));
		}
		return Math.Min(val, room * 0.8);
	}

	public static double InverseSmootherStep(double value)
	{
		double num = 0.0;
		double num2 = 1.0;
		for (int i = 0; i < 30; i++)
		{
			double num3 = (num + num2) / 2.0;
			if (SmootherStep(num3) < value)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		return (num + num2) / 2.0;
	}

	public static double SmootherStep(double t)
	{
		double num = Math.Min(1.0, Math.Max(0.0, t));
		return num * num * num * (num * (6.0 * num - 15.0) + 10.0);
	}
}
