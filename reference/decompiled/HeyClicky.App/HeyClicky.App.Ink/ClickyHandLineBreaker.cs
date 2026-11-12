using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;

namespace HeyClicky.App.Ink;

public static class ClickyHandLineBreaker
{
	private static readonly Dictionary<PaperFont, (double Natural, double Ascent)> metricsCache = new Dictionary<PaperFont, (double, double)>();

	private static CanvasTextLayout Layout(string text, PaperFont font, double width, bool wraps)
	{
		using CanvasTextFormat canvasTextFormat = font.TextFormat();
		canvasTextFormat.WordWrapping = ((!wraps) ? CanvasWordWrapping.NoWrap : CanvasWordWrapping.Wrap);
		return new CanvasTextLayout(CanvasDevice.GetSharedDevice(), text, canvasTextFormat, (float)Math.Min(width, 1000000.0), 1000000f);
	}

	public static double NaturalLineHeight(PaperFont font)
	{
		return Metrics(font).Natural;
	}

	public static double Ascent(PaperFont font)
	{
		return Metrics(font).Ascent;
	}

	private static (double Natural, double Ascent) Metrics(PaperFont font)
	{
		lock (metricsCache)
		{
			if (metricsCache.TryGetValue(font, out (double, double) value))
			{
				return value;
			}
			using CanvasTextLayout canvasTextLayout = Layout("Hg", font, 1000000.0, wraps: false);
			CanvasLineMetrics canvasLineMetrics = canvasTextLayout.LineMetrics[0];
			(float, float) tuple = (canvasLineMetrics.Height, canvasLineMetrics.Baseline);
			Dictionary<PaperFont, (double Natural, double Ascent)> dictionary = metricsCache;
			(float, float) tuple2 = tuple;
			dictionary[font] = (tuple2.Item1, tuple2.Item2);
			tuple2 = tuple;
			return (Natural: tuple2.Item1, Ascent: tuple2.Item2);
		}
	}

	public static IReadOnlyList<string> Lines(string text, PaperFont font, double width, int? lineLimit)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new global::_003C_003Ez__ReadOnlySingleElementList<string>("");
		}
		double width2 = Math.Max(width, 1.0);
		List<string> list = new List<string>();
		using CanvasTextLayout canvasTextLayout = Layout(text, font, width2, wraps: true);
		int num = 0;
		CanvasLineMetrics[] lineMetrics = canvasTextLayout.LineMetrics;
		for (int i = 0; i < lineMetrics.Length; i++)
		{
			CanvasLineMetrics canvasLineMetrics = lineMetrics[i];
			if (num >= text.Length)
			{
				break;
			}
			if (lineLimit.HasValue)
			{
				int valueOrDefault = lineLimit.GetValueOrDefault();
				if (list.Count == valueOrDefault - 1)
				{
					int num2 = num;
					string line = text.Substring(num2, text.Length - num2).Trim();
					list.Add(Truncated(line, font, width2));
					break;
				}
			}
			int num3 = Math.Min(canvasLineMetrics.CharacterCount, text.Length - num);
			if (num3 <= 0)
			{
				break;
			}
			list.Add(text.Substring(num, num3).Trim());
			num += num3;
		}
		List<string> list2;
		if (list.Count != 0)
		{
			list2 = list;
		}
		else
		{
			int i = 1;
			list2 = new List<string>(i);
			CollectionsMarshal.SetCount(list2, i);
			Span<string> span = CollectionsMarshal.AsSpan(list2);
			int num2 = 0;
			span[num2] = "";
		}
		return list2;
	}

	public static double Width(string line, PaperFont font)
	{
		if (string.IsNullOrEmpty(line))
		{
			return 0.0;
		}
		using CanvasTextLayout canvasTextLayout = Layout(line, font, 1000000.0, wraps: false);
		return canvasTextLayout.LayoutBoundsIncludingTrailingWhitespace.Width;
	}

	private static string Truncated(string line, PaperFont font, double width)
	{
		if (Width(line, font) <= width)
		{
			return line;
		}
		string text = line;
		while (text.Length > 0)
		{
			TextElementEnumerator textElementEnumerator = StringInfo.GetTextElementEnumerator(text);
			int length = 0;
			while (textElementEnumerator.MoveNext())
			{
				length = textElementEnumerator.ElementIndex;
			}
			text = text.Substring(0, length);
			string text2 = text.TrimEnd(' ', '\t').TrimStart(' ', '\t') + "…";
			if (Width(text2, font) <= width)
			{
				return text2;
			}
		}
		return "…";
	}
}
