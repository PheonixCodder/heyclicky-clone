using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public static class ResultCardLayouts
{
	public sealed record Arrangement(IReadOnlyList<ClickyAnswer.Block> Blocks, bool ShowsNote, bool IsCompact = false)
	{
		public bool Equals(Arrangement? other)
		{
			if ((object)other != null && Blocks.SequenceEqual(other.Blocks) && ShowsNote == other.ShowsNote)
			{
				return IsCompact == other.IsCompact;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Blocks.Count, ShowsNote, IsCompact);
		}
	}

	public enum MoveKind
	{
		None,
		Same,
		Better,
		Worse,
		Plain
	}

	public static IReadOnlyList<Arrangement> Layouts(ClickyAnswer answer)
	{
		IReadOnlyList<ClickyAnswer.Block> blocks = answer.Blocks;
		bool hasNote = answer.Note != null;
		List<Arrangement> layouts = new List<Arrangement>();
		TryBlocks(blocks);
		if (blocks.Count > 1)
		{
			ClickyAnswer.Block item = blocks.FirstOrDefault((ClickyAnswer.Block b) => (uint)(b - 3) <= 1u, blocks[0]);
			if (!blocks.Any((ClickyAnswer.Block b) => (uint)(b - 3) <= 1u))
			{
				item = blocks[0];
			}
			TryBlocks(new global::_003C_003Ez__ReadOnlySingleElementList<ClickyAnswer.Block>(item));
		}
		if (blocks.Count > 0)
		{
			layouts.Add(new Arrangement(Array.Empty<ClickyAnswer.Block>(), ShowsNote: false));
		}
		return layouts;
		void TryBlocks(IReadOnlyList<ClickyAnswer.Block> shown)
		{
			bool flag = shown.Contains(ClickyAnswer.Block.Ask) || shown.Contains(ClickyAnswer.Block.Numbers);
			if (hasNote)
			{
				layouts.Add(new Arrangement(shown, ShowsNote: true));
				if (flag)
				{
					layouts.Add(new Arrangement(shown, ShowsNote: true, IsCompact: true));
				}
			}
			layouts.Add(new Arrangement(shown, ShowsNote: false));
			if (flag)
			{
				layouts.Add(new Arrangement(shown, ShowsNote: false, IsCompact: true));
			}
		}
	}

	public static double? Numeric(string text)
	{
		string text2 = text.ToLowerInvariant();
		if (!double.TryParse(new string(text2.Where((char ch) => char.IsDigit(ch) || ch == '.' || ch == '-').ToArray()), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		if (text2.EndsWith('k'))
		{
			result *= 1000.0;
		}
		else if (text2.EndsWith('m'))
		{
			result *= 1000000.0;
		}
		return result;
	}

	public static double? Fraction(ClickyAnswer.Number number)
	{
		string of = number.Of;
		if (of != null)
		{
			double? num = Numeric(of);
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				if (!(valueOrDefault <= 0.0))
				{
					num = Numeric(number.Value);
					if (num.HasValue)
					{
						double valueOrDefault2 = num.GetValueOrDefault();
						return Math.Min(1.0, Math.Max(0.0, valueOrDefault2 / valueOrDefault));
					}
				}
			}
		}
		return null;
	}

	public static MoveKind Move(ClickyAnswer.Number number)
	{
		string was = number.Was;
		if (was == null)
		{
			return MoveKind.None;
		}
		if (was == number.Value)
		{
			return MoveKind.Same;
		}
		double? num = Numeric(was);
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			num = Numeric(number.Value);
			if (num.HasValue)
			{
				double valueOrDefault2 = num.GetValueOrDefault();
				if (valueOrDefault == valueOrDefault2)
				{
					return MoveKind.Same;
				}
				string better = number.Better;
				if (better == null)
				{
					return MoveKind.Plain;
				}
				if (better == "higher" != valueOrDefault2 > valueOrDefault)
				{
					return MoveKind.Worse;
				}
				return MoveKind.Better;
			}
		}
		return MoveKind.Plain;
	}

	public static string Moment(DateTimeOffset date, DateTimeOffset now)
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		string text = date.ToLocalTime().ToString("t", currentCulture);
		DateTime date2 = date.ToLocalTime().Date;
		DateTime date3 = now.ToLocalTime().Date;
		if (date2 == date3)
		{
			return "at " + text;
		}
		if (date2 == date3.AddDays(1.0))
		{
			return "tomorrow " + text;
		}
		if (date2 == date3.AddDays(-1.0))
		{
			return "yesterday " + text;
		}
		if (Math.Abs((date - now).TotalSeconds) < 518400.0)
		{
			return date.ToLocalTime().ToString("dddd", currentCulture) + " " + text;
		}
		return date.ToLocalTime().ToString("MMM d", currentCulture);
	}
}
