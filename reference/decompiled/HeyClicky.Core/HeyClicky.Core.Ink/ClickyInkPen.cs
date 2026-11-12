using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Ink;

public static class ClickyInkPen
{
	public const double TitleCharactersPerSecond = 24.0;

	public const double BodyCharactersPerSecond = 48.0;

	public const double CellCharactersPerSecond = 80.0;

	public const double ObjectLandSeconds = 0.55;

	public const double RowSeconds = 0.1;

	public const double StrikeSeconds = 0.3;

	public const double FoldSeconds = 0.45;

	public const double GrowSeconds = 0.4;

	public const double PauseBetweenBlocks = 0.1;

	public const double TravelSeconds = 0.35;

	public const double TitleLeadSeconds = 0.3;

	public const double HeavyRewriteSimilarity = 0.3;

	public const int HeavyRewriteStrikeRuns = 3;

	public const int HeavyRewriteMinimumWords = 5;

	public const double HeavyRewriteStrikeCharactersPerSecond = 90.0;

	public const double HeavyRewriteStrikeMaximumSeconds = 0.8;

	public const double JumpTravelSeconds = 0.8;

	public const int JumpPageDistance = 2;

	public const int JumpBlockDistance = 4;

	public const double AfterDoneHasteMaximum = 2.5;

	public const double AfterDoneUnhurriedSeconds = 1.5;

	public const double AfterDoneSoftness = 1.0;

	public const double TickSeconds = 0.3;

	public const double NumberChangeSeconds = 0.45;

	public const double MaximumBacklogSeconds = 6.0;

	public const double HurryAfterBacklogSeconds = 4.0;

	public const double BacklogSecondsPerHaste = 6.0;

	public const double MaximumHaste = 2.5;

	public const double MaximumMotionHaste = 1.6;

	public const double UnhurriedVersionSeconds = 12.0;

	public const double LongVersionSoftness = 8.0;

	public const double SlowestStreamHaste = 0.6;

	public static double AfterDoneHaste(double left)
	{
		if (!(left > 1.5))
		{
			return 1.0;
		}
		double num = 1.5 + 1.0 * Math.Log(1.0 + (left - 1.5) / 1.0);
		return Math.Min(2.5, Math.Max(1.0, left / num));
	}

	public static double Haste(double backlog, double work)
	{
		double val = 1.0 + Math.Max(0.0, backlog - 4.0) / 6.0;
		double num = ((work <= 12.0) ? work : (12.0 + 8.0 * Math.Log(1.0 + (work - 12.0) / 8.0)));
		double val2 = ((num > 0.0) ? (work / num) : 1.0);
		return Math.Min(2.5, Math.Max(Math.Max(1.0, val), val2));
	}

	public static double Beats(string text)
	{
		string[] array = text.Characters();
		double num = 0.0;
		for (int i = 0; i < array.Length; i++)
		{
			num += Beat(array[i]) + Rest(array[i], (i + 1 < array.Length) ? array[i + 1] : null, (i > 0) ? array[i - 1] : null);
		}
		return num;
	}

	public static (int Characters, double Wet) Revealed(string text, double progress)
	{
		if (!(progress < 1.0))
		{
			return (Characters: text.CharacterCount(), Wet: 0.0);
		}
		if (!(progress > 0.0))
		{
			return (Characters: 0, Wet: 0.0);
		}
		string[] array = text.Characters();
		double num = progress * Beats(text);
		double num2 = 0.0;
		for (int i = 0; i < array.Length; i++)
		{
			double num3 = Beat(array[i]);
			if (num < num2 + num3)
			{
				return (Characters: i, Wet: Math.Max(0.0, Math.Min(1.0, (num - num2) / num3)));
			}
			num2 += num3 + Rest(array[i], (i + 1 < array.Length) ? array[i + 1] : null, (i > 0) ? array[i - 1] : null);
			if (num < num2)
			{
				return (Characters: i + 1, Wet: 0.0);
			}
		}
		return (Characters: array.Length, Wet: 0.0);
	}

	public static List<double> CumulativeBeats(string text)
	{
		string[] array = text.Characters();
		List<double> list = new List<double>(array.Length + 1) { 0.0 };
		for (int i = 0; i < array.Length; i++)
		{
			list.Add(list[i] + Beat(array[i]) + Rest(array[i], (i + 1 < array.Length) ? array[i + 1] : null, (i > 0) ? array[i - 1] : null));
		}
		return list;
	}

	private static double Beat(string character)
	{
		if (ClickyInkParser.Emphasis.IsMarker(character))
		{
			return 0.0;
		}
		if (!(character == " "))
		{
			return 1.0;
		}
		return 0.8;
	}

	private static double Rest(string character, string? next, string? previous)
	{
		bool flag = next == null || next == " " || next == "\n";
		if (next == null)
		{
			return 0.0;
		}
		if (character != null)
		{
			int length = character.Length;
			if (length == 1)
			{
				switch (character[0])
				{
				case ',':
				case ':':
				case ';':
					return flag ? 3.5 : 0.0;
				case '!':
				case '.':
				case '?':
				case '…':
					return (flag && previous != ".") ? 6 : 0;
				case '–':
				case '—':
					return 2.5;
				case '\n':
					return 4.0;
				}
			}
		}
		return 0.0;
	}
}
