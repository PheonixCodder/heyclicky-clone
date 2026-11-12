using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HeyClicky.Core.Documents.Spreadsheet;

public sealed class SpreadsheetGridLayout
{
	private readonly ISpreadsheetTextMeasurer measurer;

	private readonly double[] columnWidths;

	private readonly double[] rowHeights;

	private double[] columnOffsets = Array.Empty<double>();

	private double[] rowOffsets = Array.Empty<double>();

	public SpreadsheetSheet Sheet { get; }

	public int ColumnCount { get; }

	public int RowCount { get; }

	public int FrozenRowCount { get; }

	public int FrozenColumnCount { get; }

	public double ColumnHeaderHeight { get; } = 22.0;

	public double RowHeaderWidth { get; }

	public IReadOnlyList<double> ColumnWidths => columnWidths;

	public IReadOnlyList<double> RowHeights => rowHeights;

	public IReadOnlyList<double> ColumnOffsets => columnOffsets;

	public IReadOnlyList<double> RowOffsets => rowOffsets;

	public double TotalWidth => columnOffsets[^1];

	public double TotalHeight => rowOffsets[^1];

	public double FrozenWidth => columnOffsets[FrozenColumnCount];

	public double FrozenHeight => rowOffsets[FrozenRowCount];

	public SpreadsheetGridLayout(SpreadsheetSheet sheet, ISpreadsheetTextMeasurer measurer)
	{
		Sheet = sheet;
		this.measurer = measurer;
		ColumnCount = Math.Max(sheet.LastDataColumn + 1 + 4, 26);
		RowCount = Math.Max(sheet.LastDataRow + 1 + 60, 100);
		FrozenRowCount = Math.Min(sheet.FrozenRowCount, Math.Max(0, RowCount - 1));
		FrozenColumnCount = Math.Min(sheet.FrozenColumnCount, Math.Max(0, ColumnCount - 1));
		int length = RowCount.ToString(CultureInfo.InvariantCulture).Length;
		RowHeaderWidth = Math.Max(42, length * 8 + 18);
		columnWidths = new double[ColumnCount];
		for (int i = 0; i < ColumnCount; i++)
		{
			columnWidths[i] = (sheet.HiddenColumns.Contains(i) ? 0.0 : (sheet.CustomColumnWidths.TryGetValue(i, out var value) ? value : sheet.DefaultColumnWidth));
		}
		rowHeights = new double[RowCount];
		for (int j = 0; j < RowCount; j++)
		{
			rowHeights[j] = (sheet.HiddenRows.Contains(j) ? 0.0 : (sheet.CustomRowHeights.TryGetValue(j, out var value2) ? value2 : sheet.DefaultRowHeight));
		}
		FitWrappedRowsWithoutAuthoredHeights();
		RebuildColumnOffsets();
		RebuildRowOffsets();
	}

	public SpreadsheetRect CellRect(int row, int column)
	{
		return new SpreadsheetRect(columnOffsets[column], rowOffsets[row], columnWidths[column], rowHeights[row]);
	}

	public SpreadsheetRect Rect(SpreadsheetCellRange range)
	{
		int num = Math.Min(range.LastRow, RowCount - 1);
		int num2 = Math.Min(range.LastColumn, ColumnCount - 1);
		return new SpreadsheetRect(columnOffsets[range.FirstColumn], rowOffsets[range.FirstRow], columnOffsets[num2 + 1] - columnOffsets[range.FirstColumn], rowOffsets[num + 1] - rowOffsets[range.FirstRow]);
	}

	public int Column(double x)
	{
		return Index(columnOffsets, x);
	}

	public int Row(double y)
	{
		return Index(rowOffsets, y);
	}

	public void SetColumnWidth(double width, int column)
	{
		if (column >= 0 && column < columnWidths.Length)
		{
			columnWidths[column] = Math.Max(12.0, Math.Round(width, MidpointRounding.AwayFromZero));
			RebuildColumnOffsets();
		}
	}

	public void SetRowHeight(double height, int row)
	{
		if (row >= 0 && row < rowHeights.Length)
		{
			rowHeights[row] = Math.Max(10.0, Math.Round(height, MidpointRounding.AwayFromZero));
			RebuildRowOffsets();
		}
	}

	public double FittingWidth(int column)
	{
		double num = 0.0;
		int num2 = 0;
		foreach (int item in Sheet.CellsByRow.Keys.OrderBy((int r) => r))
		{
			if (num2 >= 3000)
			{
				break;
			}
			num2++;
			SpreadsheetCell spreadsheetCell = Sheet.Cell(item, column);
			if ((object)spreadsheetCell != null && spreadsheetCell.DisplayText.Length != 0)
			{
				SpreadsheetCellStyle style = Sheet.Style(spreadsheetCell.StyleIndex);
				double num3 = (from line in spreadsheetCell.DisplayText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
					select measurer.SingleLineWidth(line, style)).DefaultIfEmpty(0.0).Max();
				num = Math.Max(num, num3 + (double)style.IndentLevel * 9.0);
			}
		}
		return Math.Min(640.0, Math.Max(40.0, Math.Ceiling(num) + 8.0 + 6.0));
	}

	private void RebuildColumnOffsets()
	{
		double[] array = new double[columnWidths.Length + 1];
		for (int i = 0; i < columnWidths.Length; i++)
		{
			array[i + 1] = array[i] + columnWidths[i];
		}
		columnOffsets = array;
	}

	private void RebuildRowOffsets()
	{
		double[] array = new double[rowHeights.Length + 1];
		for (int i = 0; i < rowHeights.Length; i++)
		{
			array[i + 1] = array[i] + rowHeights[i];
		}
		rowOffsets = array;
	}

	private void FitWrappedRowsWithoutAuthoredHeights()
	{
		int num = 0;
		foreach (KeyValuePair<int, Dictionary<int, SpreadsheetCell>> item in Sheet.CellsByRow)
		{
			item.Deconstruct(out var key, out var value);
			int num2 = key;
			Dictionary<int, SpreadsheetCell> dictionary = value;
			if (Sheet.CustomRowHeights.ContainsKey(num2) || num2 < 0 || num2 >= rowHeights.Length || num >= 5000 || rowHeights[num2] <= 0.0)
			{
				continue;
			}
			num++;
			double num3 = rowHeights[num2];
			foreach (KeyValuePair<int, SpreadsheetCell> item2 in dictionary)
			{
				item2.Deconstruct(out key, out var value2);
				int num4 = key;
				SpreadsheetCell spreadsheetCell = value2;
				if (spreadsheetCell.DisplayText.Length == 0 || num4 < 0 || num4 >= columnWidths.Length)
				{
					continue;
				}
				SpreadsheetCellStyle style = Sheet.Style(spreadsheetCell.StyleIndex);
				if (style.WrapsText)
				{
					double num5 = columnWidths[num4] - 8.0;
					if (!(num5 <= 4.0))
					{
						double a = measurer.WrappedHeight(spreadsheetCell.DisplayText, style, num5);
						num3 = Math.Max(num3, Math.Min(Math.Ceiling(a) + 7.0, 420.0));
					}
				}
			}
			rowHeights[num2] = num3;
		}
	}

	private static int Index(double[] offsets, double position)
	{
		int num = offsets.Length - 2;
		if (num < 0)
		{
			return 0;
		}
		if (position <= 0.0)
		{
			return 0;
		}
		if (position >= offsets[num + 1])
		{
			return num;
		}
		int num2 = 0;
		int num3 = num;
		while (num2 < num3)
		{
			int num4 = (num2 + num3 + 1) / 2;
			if (offsets[num4] <= position)
			{
				num2 = num4;
			}
			else
			{
				num3 = num4 - 1;
			}
		}
		return num2;
	}
}
