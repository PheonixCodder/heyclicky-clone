using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Documents.Markdown;

public sealed record MarkdownViewerTableLayout(IReadOnlyList<double> ColumnWidths, bool ScrollsHorizontally)
{
	public const double CellHorizontalPadding = 12.0;

	public const double CellVerticalPadding = 8.0;

	public const double MinimumColumnWidth = 120.0;

	public const double MaximumNaturalColumnWidth = 360.0;

	private static readonly Dictionary<string, MarkdownViewerTableLayout> Memo = new Dictionary<string, MarkdownViewerTableLayout>();

	private static readonly object MemoGate = new object();

	public static MarkdownViewerTableLayout Compute(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, double availableWidth, Func<string, double> measure)
	{
		string key = availableWidth.ToString("R", CultureInfo.InvariantCulture) + "\u001f" + string.Join("\u001e", headers) + "\u001d" + string.Join("\u001d", rows.Select((IReadOnlyList<string> r) => string.Join("\u001e", r)));
		lock (MemoGate)
		{
			if (Memo.TryGetValue(key, out MarkdownViewerTableLayout value))
			{
				return value;
			}
		}
		MarkdownViewerTableLayout markdownViewerTableLayout = ComputeUncached(headers, rows, availableWidth, measure);
		lock (MemoGate)
		{
			if (Memo.Count >= 200)
			{
				Memo.Clear();
			}
			Memo[key] = markdownViewerTableLayout;
			return markdownViewerTableLayout;
		}
	}

	public static string MeasuredText(string markdownCellText)
	{
		return markdownCellText.Replace("**", "").Replace("`", "").Replace("_", "");
	}

	private static MarkdownViewerTableLayout ComputeUncached(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, double availableWidth, Func<string, double> measure)
	{
		int count = headers.Count;
		if (count == 0)
		{
			return new MarkdownViewerTableLayout(Array.Empty<double>(), ScrollsHorizontally: false);
		}
		double[] naturalWidths = new double[count];
		for (int i = 0; i < count; i++)
		{
			double num = Measured(headers[i]);
			foreach (IReadOnlyList<string> row in rows)
			{
				if (i < row.Count)
				{
					num = Math.Max(num, Measured(row[i]));
				}
			}
			double val = num + 24.0;
			naturalWidths[i] = Math.Min(360.0, Math.Max(120.0, val));
		}
		double num2 = naturalWidths.Sum();
		if (availableWidth <= 0.0 || num2 <= 0.0)
		{
			return new MarkdownViewerTableLayout(naturalWidths, ScrollsHorizontally: true);
		}
		double scale = availableWidth / num2;
		if (scale >= 1.0)
		{
			return new MarkdownViewerTableLayout(naturalWidths.Select((double w) => w * scale).ToArray(), ScrollsHorizontally: false);
		}
		if (120.0 * (double)count <= availableWidth + 0.5)
		{
			double low = 120.0;
			double num3 = naturalWidths.Max();
			for (int j = 0; j < 24; j++)
			{
				double num4 = (low + num3) / 2.0;
				if (Total(num4) <= availableWidth)
				{
					low = num4;
				}
				else
				{
					num3 = num4;
				}
			}
			double[] widths = naturalWidths.Select((double w) => Math.Min(w, Math.Max(low, 120.0))).ToArray();
			List<int> list = (from index in Enumerable.Range(0, count)
				where naturalWidths[index] > widths[index]
				select index).ToList();
			double num5 = availableWidth - widths.Sum();
			if (num5 > 0.0 && list.Count > 0)
			{
				foreach (int item in list)
				{
					widths[item] += num5 / (double)list.Count;
				}
			}
			return new MarkdownViewerTableLayout(widths, ScrollsHorizontally: false);
		}
		return new MarkdownViewerTableLayout(naturalWidths, ScrollsHorizontally: true);
		double Measured(string cell)
		{
			return Math.Ceiling(measure(MeasuredText(cell)));
		}
		double Total(double cap)
		{
			return naturalWidths.Sum((double w) => Math.Min(w, Math.Max(cap, 120.0)));
		}
	}

	public static bool IsBestRow(IReadOnlyList<string> cells)
	{
		if (cells.Count > 0)
		{
			return cells[0].TrimStart(' ', '\t').StartsWith('✓');
		}
		return false;
	}

	[CompilerGenerated]
	private MarkdownViewerTableLayout(MarkdownViewerTableLayout original)
	{
		ColumnWidths = original.ColumnWidths;
		ScrollsHorizontally = original.ScrollsHorizontally;
	}
}
