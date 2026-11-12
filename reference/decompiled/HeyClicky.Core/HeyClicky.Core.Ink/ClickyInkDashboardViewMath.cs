using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkDashboardViewMath
{
	private static readonly string[] ProgressKeys = new string[9] { "value", "progress", "percent", "done", "of", "total", "goal", "max", "target" };

	public const double TallyGroupWidth = 26.0;

	public const double TallyGroupGap = 9.0;

	public const double TileHorizontalPadding = 12.0;

	public const double TileStackedChangeHeight = 17.0;

	public static string? DeltaWordsWithoutAChange(string? given)
	{
		string text = given?.TrimWs();
		if (string.IsNullOrEmpty(text) || text.Any(char.IsDigit))
		{
			return null;
		}
		string lower = text.Lowered();
		if (!new string[7] { "up", "down", "fell", "rose", "flat", "same", "no change" }.Any((string w) => lower.Contains(w, StringComparison.Ordinal)))
		{
			return text;
		}
		return null;
	}

	public static (string Text, double Value)? ResolvedDelta(string? given, (string Text, double Value)? computed)
	{
		string text = given?.TrimWs();
		if (!string.IsNullOrEmpty(text))
		{
			string text2 = text.Lowered();
			bool flag = text.HasPrefix("-") || text.HasPrefix("−") || text2.Contains("down", StringComparison.Ordinal) || text2.Contains("fell", StringComparison.Ordinal);
			bool flag2;
			switch (text2)
			{
			case "0":
			case "flat":
			case "same":
			case "no change":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			bool flag3 = flag2;
			return (text, (!flag3) ? ((!flag) ? 1 : (-1)) : 0);
		}
		if (computed.HasValue)
		{
			(string, double) valueOrDefault = computed.GetValueOrDefault();
			if (valueOrDefault.Item2 != 0.0)
			{
				return valueOrDefault;
			}
		}
		return null;
	}

	public static string DeltaArrow(double value)
	{
		if (!(value > 0.0))
		{
			if (!(value < 0.0))
			{
				return "=";
			}
			return "↓";
		}
		return "↑";
	}

	public static string DeltaWords(string text)
	{
		return text.TrimSet("+-−");
	}

	public static bool? DeltaIsGoodNews(double delta, ClickyInkDocument.Block block)
	{
		if (delta == 0.0)
		{
			return null;
		}
		bool flag;
		switch (block["good"]?.Lowered() ?? "")
		{
		case "down":
		case "lower":
		case "less":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		bool flag2 = flag;
		return delta > 0.0 != flag2;
	}

	public static string ProgressValueKey(ClickyInkDocument.Block block)
	{
		return new string[4] { "value", "progress", "percent", "done" }.FirstOrDefault((string k) => block[k] != null) ?? "value";
	}

	public static double? ProgressPreviousFraction(ClickyInkDocument.Block block)
	{
		if (!ClickyInkChange.ChangedAt(block).HasValue)
		{
			return null;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>(block.Attributes);
		bool flag = false;
		string[] progressKeys = ProgressKeys;
		foreach (string key in progressKeys)
		{
			string text = ClickyInkChange.Previous(block, key);
			if (text != null)
			{
				dictionary[key] = text;
				flag = true;
			}
		}
		if (!flag)
		{
			return null;
		}
		double? fraction = new ClickyInkProgress(dictionary).Fraction;
		if (fraction.HasValue)
		{
			double valueOrDefault = fraction.GetValueOrDefault();
			return Math.Min(1.0, Math.Max(0.0, valueOrDefault));
		}
		return null;
	}

	public static string? ProgressPreviousShown(ClickyInkDocument.Block block)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(block.Attributes);
		string[] progressKeys = ProgressKeys;
		foreach (string key in progressKeys)
		{
			string text = ClickyInkChange.Previous(block, key);
			if (text != null)
			{
				dictionary[key] = text;
			}
		}
		return new ClickyInkProgress(dictionary).Shown;
	}

	public static int TallyCapacity(double width, int rows)
	{
		return Math.Max(5, (int)((width + 9.0) / 35.0) * 5 * rows);
	}

	public static int TallyGroupsPerRow(double width)
	{
		return Math.Max(1, (int)((width + 9.0) / 35.0));
	}

	public static int TileColumns(int count, double width, bool isWindowPreview)
	{
		if (isWindowPreview)
		{
			if (count <= 4)
			{
				return Math.Max(count, 1);
			}
			return (count + 1) / 2;
		}
		int num = Math.Max(1, Math.Min((int)(width / 176.0), 4));
		int val = (count + num - 1) / num;
		return Math.Max(1, (count + Math.Max(val, 1) - 1) / Math.Max(val, 1));
	}

	public static double TileHeight(ClickyInkDocument.Block block, bool stacksChange, bool compact = false)
	{
		double num = block.Kind switch
		{
			ClickyInkDocument.Kind.Spark => 34, 
			ClickyInkDocument.Kind.Tally => 42, 
			ClickyInkDocument.Kind.Progress => 20, 
			ClickyInkDocument.Kind.Streak => 18, 
			ClickyInkDocument.Kind.Countdown => 16, 
			ClickyInkDocument.Kind.Figure => (block["values"] != null) ? 30 : 0, 
			ClickyInkDocument.Kind.Paragraph => 20, 
			_ => 0, 
		};
		string text = block["note"];
		double num2 = ((text != null && text.Length > 0) ? (compact ? 17 : 20) : 0);
		double num3 = (stacksChange ? 17.0 : 0.0);
		return 58.0 + num + num2 + num3 + (double)(compact ? 6 : 16);
	}

	public static (string Number, double NumberSize, string Change)? TileChangeMeasure(ClickyInkDocument.Block block)
	{
		string text = block["delta"]?.TrimWs();
		if (text == null || text.Length <= 0)
		{
			return null;
		}
		string item;
		double item2;
		switch (block.Kind)
		{
		case ClickyInkDocument.Kind.Figure:
			item = block["value"] ?? block["big"] ?? "";
			item2 = 28.0;
			break;
		case ClickyInkDocument.Kind.Spark:
		{
			string? last = new ClickyInkSparkReading(block).Last;
			string text2 = block["unit"];
			item = last + ((text2 != null) ? (" " + text2) : "");
			item2 = 26.0;
			break;
		}
		default:
			return null;
		}
		return (item, item2, "↑ " + DeltaWords(text));
	}

	public static bool TileStacksChange(double numberWidth, int numberCharacters, double changeWidth, double width)
	{
		return numberWidth - 0.8 * (double)numberCharacters + 6.0 + changeWidth > width - 24.0;
	}

	public static string StateWord(string word)
	{
		if (word.CharacterCount() > 3)
		{
			return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant());
		}
		return word.ToUpperInvariant();
	}

	public static ClickyInkRoutineControl? RunStampRoutine(ClickyInkDocument.Block block, IReadOnlyList<ClickyInkRoutineControl> routines)
	{
		string words = (block.Text + " " + block["extra"]).Lowered();
		return routines.FirstOrDefault((ClickyInkRoutineControl r) => r.Title.Length > 0 && words.Contains(r.Title.Lowered(), StringComparison.Ordinal)) ?? routines.FirstOrDefault();
	}

	public static List<int> RoutineIntervalOptions(int interval)
	{
		int[] source = new int[4] { 60, 240, 1440, 10080 };
		if (!source.Contains(interval))
		{
			return (from m in source.Append(interval)
				orderby m
				select m).ToList();
		}
		return source.ToList();
	}

	public static string RunCountWords(int runCount)
	{
		return $"ran {runCount} {((runCount == 1) ? "time" : "times")}";
	}

	public static string RoutineStatusWords(ClickyInkRoutineControl routine, DateTimeOffset? now = null)
	{
		if (routine.IsPaused)
		{
			if (!routine.IsPausedAfterFailures)
			{
				return "paused";
			}
			return "paused after 3 failed runs";
		}
		if (routine.IsRunning)
		{
			return "running now";
		}
		return "next: " + ClickyInkRoutineControl.NextRunWords(routine.NextRunAt, now) + " · " + ClickyInkRoutineControl.CadenceWords(routine.IntervalMinutes).ToLowerInvariant();
	}

	public static string? PipelinePercent(double from, double to)
	{
		if (!(from > 0.0))
		{
			return null;
		}
		return InkText.InkClamping(Math.Round(to / from * 100.0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture) + "%";
	}

	public static double PipelineBarWidth(double stageWidth, double value, double top, double appear)
	{
		return Math.Max(4.0, (stageWidth - 10.0) * (value / top) * appear);
	}
}
