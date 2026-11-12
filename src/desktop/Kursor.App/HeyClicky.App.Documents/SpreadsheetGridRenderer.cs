using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using HeyClicky.Core.Documents.Spreadsheet;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Text;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Documents;

public sealed class SpreadsheetGridRenderer
{
	private readonly SpreadsheetTextAttributes attributes = SpreadsheetTextAttributes.Shared;

	private readonly Dictionary<int, List<int>> mergeIndicesByRow = new Dictionary<int, List<int>>();

	private readonly Dictionary<SpreadsheetCellAddress, (string Text, double Width, CanvasTextLayout Layout)> singleLineTextCache = new Dictionary<SpreadsheetCellAddress, (string, double, CanvasTextLayout)>();

	private readonly Dictionary<string, (double Height, CanvasTextLayout Layout)> wrappedCache = new Dictionary<string, (double, CanvasTextLayout)>();

	public SpreadsheetGridLayout Layout { get; }

	public SpreadsheetSheet Sheet => Layout.Sheet;

	public SpreadsheetSelection? Selection { get; set; }

	public SpreadsheetGridRenderer(SpreadsheetGridLayout layout)
	{
		Layout = layout;
		for (int i = 0; i < layout.Sheet.MergedRanges.Count; i++)
		{
			SpreadsheetCellRange spreadsheetCellRange = layout.Sheet.MergedRanges[i];
			for (int j = spreadsheetCellRange.FirstRow; j <= Math.Min(spreadsheetCellRange.LastRow, spreadsheetCellRange.FirstRow + 5000); j++)
			{
				if (!mergeIndicesByRow.TryGetValue(j, out List<int> value))
				{
					value = (mergeIndicesByRow[j] = new List<int>());
				}
				value.Add(i);
			}
		}
	}

	public SpreadsheetCellRange? MergedRange(int row, int column)
	{
		if (!mergeIndicesByRow.TryGetValue(row, out List<int> value))
		{
			return null;
		}
		foreach (int item in value)
		{
			if (Sheet.MergedRanges[item].Contains(row, column))
			{
				return Sheet.MergedRanges[item];
			}
		}
		return null;
	}

	public SpreadsheetCellRange ExpandedForMerges(SpreadsheetCellRange range)
	{
		if (Sheet.MergedRanges.Count == 0)
		{
			return range;
		}
		SpreadsheetCellRange spreadsheetCellRange = range;
		bool flag = true;
		while (flag)
		{
			flag = false;
			foreach (SpreadsheetCellRange mergedRange in Sheet.MergedRanges)
			{
				if (mergedRange.Intersects(spreadsheetCellRange))
				{
					SpreadsheetCellRange spreadsheetCellRange2 = spreadsheetCellRange.Union(mergedRange);
					if (spreadsheetCellRange2 != spreadsheetCellRange)
					{
						spreadsheetCellRange = spreadsheetCellRange2;
						flag = true;
					}
				}
			}
		}
		return spreadsheetCellRange;
	}

	public void InvalidateTextMeasurements(int? column = null)
	{
		if (column.HasValue)
		{
			int c = column.GetValueOrDefault();
			foreach (SpreadsheetCellAddress item in singleLineTextCache.Keys.Where((SpreadsheetCellAddress k) => k.Column == c).ToList())
			{
				singleLineTextCache[item].Layout.Dispose();
				singleLineTextCache.Remove(item);
			}
		}
		else
		{
			foreach (var value in singleLineTextCache.Values)
			{
				value.Layout.Dispose();
			}
			singleLineTextCache.Clear();
		}
		foreach (var value2 in wrappedCache.Values)
		{
			value2.Layout.Dispose();
		}
		wrappedCache.Clear();
	}

	private static Rect R(SpreadsheetRect r)
	{
		return new Rect(r.X, r.Y, Math.Max(0.0, r.Width), Math.Max(0.0, r.Height));
	}

	private static Rect R(double x, double y, double w, double h)
	{
		return new Rect(x, y, Math.Max(0.0, w), Math.Max(0.0, h));
	}

	public void Draw(CanvasDrawingSession ds, SpreadsheetPaneGeometry geometry, SpreadsheetRect dirtyRect, SpreadsheetRect paneBounds)
	{
		SpreadsheetRect other = new SpreadsheetRect(geometry.RowHeaderWidth, geometry.ColumnHeaderHeight, Math.Max(0.0, paneBounds.Width - geometry.RowHeaderWidth), Math.Max(0.0, paneBounds.Height - geometry.ColumnHeaderHeight));
		SpreadsheetRect spreadsheetRect = dirtyRect.Intersection(other);
		if (!spreadsheetRect.IsEmpty && !geometry.RowsEmpty && !geometry.ColumnsEmpty)
		{
			using (ds.CreateLayer(1f, R(spreadsheetRect)))
			{
				DrawCells(ds, geometry, spreadsheetRect);
			}
		}
		else if (!spreadsheetRect.IsEmpty)
		{
			ds.FillRectangle(R(spreadsheetRect), SpreadsheetGridPalette.Paper);
		}
		if (geometry.ColumnHeaderHeight > 0.0)
		{
			SpreadsheetRect spreadsheetRect2 = new SpreadsheetRect(geometry.RowHeaderWidth, 0.0, other.Width, geometry.ColumnHeaderHeight);
			SpreadsheetRect spreadsheetRect3 = dirtyRect.Intersection(spreadsheetRect2);
			if (!spreadsheetRect3.IsEmpty)
			{
				using (ds.CreateLayer(1f, R(spreadsheetRect3)))
				{
					DrawColumnHeaders(ds, geometry, spreadsheetRect2, spreadsheetRect3);
				}
			}
		}
		if (geometry.RowHeaderWidth > 0.0)
		{
			SpreadsheetRect spreadsheetRect4 = new SpreadsheetRect(0.0, geometry.ColumnHeaderHeight, geometry.RowHeaderWidth, other.Height);
			SpreadsheetRect spreadsheetRect5 = dirtyRect.Intersection(spreadsheetRect4);
			if (!spreadsheetRect5.IsEmpty)
			{
				using (ds.CreateLayer(1f, R(spreadsheetRect5)))
				{
					DrawRowHeaders(ds, geometry, spreadsheetRect4, spreadsheetRect5);
				}
			}
		}
		if (geometry.ColumnHeaderHeight > 0.0 && geometry.RowHeaderWidth > 0.0)
		{
			SpreadsheetRect r = new SpreadsheetRect(0.0, 0.0, geometry.RowHeaderWidth, geometry.ColumnHeaderHeight);
			if (r.Intersects(dirtyRect))
			{
				ds.FillRectangle(R(r), SpreadsheetGridPalette.HeaderFill);
				ds.FillRectangle(R(r.MaxX - 1.0, 0.0, 1.0, r.Height), SpreadsheetGridPalette.HeaderLine);
				ds.FillRectangle(R(0.0, r.MaxY - 1.0, r.Width, 1.0), SpreadsheetGridPalette.HeaderLine);
			}
		}
	}

	private static (int Start, int End) VisibleRange(int domainStart, int domainEnd, double minimum, double maximum, Func<double, int> offsets)
	{
		if (domainEnd <= domainStart)
		{
			return (Start: domainStart, End: domainEnd);
		}
		int num = Math.Max(domainStart, offsets(minimum));
		int num2 = Math.Min(domainEnd - 1, offsets(maximum));
		if (num > num2)
		{
			return (Start: num, End: num);
		}
		return (Start: num, End: num2 + 1);
	}

	private void DrawCells(CanvasDrawingSession ds, SpreadsheetPaneGeometry geometry, SpreadsheetRect dirtyRect)
	{
		double ox = geometry.OffsetX;
		double oy = geometry.OffsetY;
		(int Start, int End) tuple = VisibleRange(geometry.RowStart, geometry.RowEnd, dirtyRect.MinY - oy, dirtyRect.MaxY - oy, Layout.Row);
		int item = tuple.Start;
		int item2 = tuple.End;
		(int Start, int End) tuple2 = VisibleRange(geometry.ColumnStart, geometry.ColumnEnd, dirtyRect.MinX - ox, dirtyRect.MaxX - ox, Layout.Column);
		int item3 = tuple2.Start;
		int item4 = tuple2.End;
		bool flag = item2 <= item;
		bool flag2 = item4 <= item3;
		ds.FillRectangle(R(dirtyRect), SpreadsheetGridPalette.Paper);
		if (Sheet.ShowsGridLines && !flag && !flag2)
		{
			using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(ds);
			for (int i = item3; i < item4; i++)
			{
				if (!(Layout.ColumnWidths[i] <= 0.0))
				{
					float num = (float)(Layout.ColumnOffsets[i + 1] + ox - 0.5);
					canvasPathBuilder.BeginFigure(num, (float)dirtyRect.MinY);
					canvasPathBuilder.AddLine(num, (float)dirtyRect.MaxY);
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				}
			}
			for (int j = item; j < item2; j++)
			{
				if (!(Layout.RowHeights[j] <= 0.0))
				{
					float num2 = (float)(Layout.RowOffsets[j + 1] + oy - 0.5);
					canvasPathBuilder.BeginFigure((float)dirtyRect.MinX, num2);
					canvasPathBuilder.AddLine((float)dirtyRect.MaxX, num2);
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				}
			}
			using CanvasGeometry geometry2 = CanvasGeometry.CreatePath(canvasPathBuilder);
			ds.DrawGeometry(geometry2, SpreadsheetGridPalette.GridLine, 1f);
		}
		for (int k = item; k < item2; k++)
		{
			if (!Sheet.CellsByRow.TryGetValue(k, out Dictionary<int, SpreadsheetCell> value) || Layout.RowHeights[k] <= 0.0)
			{
				continue;
			}
			for (int l = item3; l < item4; l++)
			{
				if (!value.TryGetValue(l, out var value2))
				{
					continue;
				}
				SpreadsheetColor? fillColor = Sheet.Style(value2.StyleIndex).FillColor;
				if (fillColor.HasValue)
				{
					SpreadsheetColor valueOrDefault = fillColor.GetValueOrDefault();
					if (!MergedRange(k, l).HasValue)
					{
						ds.FillRectangle(R(PaneRect(k, l)), SpreadsheetTextAttributes.ColorOf(valueOrDefault));
					}
				}
			}
		}
		SpreadsheetCellRange visibleBlock = new SpreadsheetCellRange(item, item3, Math.Max(item, item2 - 1), Math.Max(item3, item4 - 1));
		List<SpreadsheetCellRange> list = ((flag || flag2) ? new List<SpreadsheetCellRange>() : Sheet.MergedRanges.Where((SpreadsheetCellRange m) => m.Intersects(visibleBlock) && m.FirstRow >= geometry.RowStart && m.FirstRow < geometry.RowEnd && m.FirstColumn >= geometry.ColumnStart && m.FirstColumn < geometry.ColumnEnd).ToList());
		foreach (SpreadsheetCellRange item5 in list)
		{
			SpreadsheetCell spreadsheetCell = Sheet.Cell(item5.FirstRow, item5.FirstColumn);
			Color color;
			if ((object)spreadsheetCell != null)
			{
				SpreadsheetColor? fillColor = Sheet.Style(spreadsheetCell.StyleIndex).FillColor;
				if (fillColor.HasValue)
				{
					SpreadsheetColor valueOrDefault2 = fillColor.GetValueOrDefault();
					color = SpreadsheetTextAttributes.ColorOf(valueOrDefault2);
					goto IL_03e7;
				}
			}
			color = SpreadsheetGridPalette.Paper;
			goto IL_03e7;
			IL_03e7:
			Color color2 = color;
			SpreadsheetRect spreadsheetRect = PaneRange(item5);
			if (Sheet.ShowsGridLines)
			{
				spreadsheetRect = spreadsheetRect with
				{
					Width = spreadsheetRect.Width - 1.0,
					Height = spreadsheetRect.Height - 1.0
				};
			}
			ds.FillRectangle(R(spreadsheetRect), color2);
		}
		int num3 = Math.Max(geometry.ColumnStart, item3 - 12);
		for (int n = item; n < item2; n++)
		{
			if (!Sheet.CellsByRow.TryGetValue(n, out Dictionary<int, SpreadsheetCell> value3) || Layout.RowHeights[n] <= 0.0)
			{
				continue;
			}
			for (int num4 = num3; num4 < item4; num4++)
			{
				if (value3.TryGetValue(num4, out var value4) && value4.DisplayText.Length != 0 && !MergedRange(n, num4).HasValue)
				{
					DrawText(ds, value4, new SpreadsheetCellAddress(n, num4), PaneRect(n, num4), value3, geometry.ColumnEnd, ox, oy, allowsSpill: true);
				}
			}
		}
		foreach (SpreadsheetCellRange item6 in list)
		{
			SpreadsheetCell spreadsheetCell2 = Sheet.Cell(item6.FirstRow, item6.FirstColumn);
			if ((object)spreadsheetCell2 != null && spreadsheetCell2.DisplayText.Length != 0)
			{
				DrawText(ds, spreadsheetCell2, new SpreadsheetCellAddress(item6.FirstRow, item6.FirstColumn), PaneRange(item6), null, geometry.ColumnEnd, ox, oy, allowsSpill: false);
			}
		}
		for (int num5 = item; num5 < item2; num5++)
		{
			if (!Sheet.CellsByRow.TryGetValue(num5, out Dictionary<int, SpreadsheetCell> value5))
			{
				continue;
			}
			for (int num6 = item3; num6 < item4; num6++)
			{
				if (!value5.TryGetValue(num6, out var value6))
				{
					continue;
				}
				SpreadsheetCellStyle style = Sheet.Style(value6.StyleIndex);
				if (style.HasBorders)
				{
					SpreadsheetCellRange? spreadsheetCellRange = MergedRange(num5, num6);
					SpreadsheetRect spreadsheetRect2;
					if (spreadsheetCellRange.HasValue)
					{
						SpreadsheetCellRange valueOrDefault3 = spreadsheetCellRange.GetValueOrDefault();
						spreadsheetRect2 = PaneRange(valueOrDefault3);
					}
					else
					{
						spreadsheetRect2 = PaneRect(num5, num6);
					}
					SpreadsheetRect rect = spreadsheetRect2;
					DrawBorders(ds, style, rect);
				}
			}
		}
		SpreadsheetSelection? selection = Selection;
		if (selection.HasValue)
		{
			SpreadsheetSelection valueOrDefault4 = selection.GetValueOrDefault();
			DrawSelection(ds, valueOrDefault4, geometry);
		}
		SpreadsheetRect PaneRange(SpreadsheetCellRange range)
		{
			return Layout.Rect(range).Offset(ox, oy);
		}
		SpreadsheetRect PaneRect(int row, int column)
		{
			return Layout.CellRect(row, column).Offset(ox, oy);
		}
	}

	private void DrawText(CanvasDrawingSession ds, SpreadsheetCell cell, SpreadsheetCellAddress address, SpreadsheetRect cellRect, Dictionary<int, SpreadsheetCell>? rowCells, int columnLimit, double ox, double oy, bool allowsSpill)
	{
		if (cellRect.Width <= 0.0 || cellRect.Height <= 0.0)
		{
			return;
		}
		SpreadsheetCellStyle style = Sheet.Style(cell.StyleIndex);
		bool flag = Sheet.Hyperlinks.ContainsKey(address);
		double num = 4.0;
		double num2 = (double)style.IndentLevel * 9.0;
		SpreadsheetColor? textColor = style.TextColor;
		Color obj;
		if (!textColor.HasValue)
		{
			obj = (flag ? SpreadsheetTextAttributes.HyperlinkColor : SpreadsheetTextAttributes.DefaultTextColor);
		}
		else
		{
			SpreadsheetColor valueOrDefault = textColor.GetValueOrDefault();
			obj = SpreadsheetTextAttributes.ColorOf(valueOrDefault);
		}
		Color color = obj;
		SpreadsheetHorizontalAlignment spreadsheetHorizontalAlignment = style.HorizontalAlignment;
		if (spreadsheetHorizontalAlignment == SpreadsheetHorizontalAlignment.General)
		{
			SpreadsheetHorizontalAlignment spreadsheetHorizontalAlignment2;
			switch (cell.Kind)
			{
			case SpreadsheetCellKind.Number:
				spreadsheetHorizontalAlignment2 = SpreadsheetHorizontalAlignment.Right;
				break;
			case SpreadsheetCellKind.Boolean:
			case SpreadsheetCellKind.Error:
				spreadsheetHorizontalAlignment2 = SpreadsheetHorizontalAlignment.Center;
				break;
			default:
				spreadsheetHorizontalAlignment2 = SpreadsheetHorizontalAlignment.Left;
				break;
			}
			spreadsheetHorizontalAlignment = spreadsheetHorizontalAlignment2;
		}
		if (style.WrapsText)
		{
			CanvasHorizontalAlignment alignment = spreadsheetHorizontalAlignment switch
			{
				SpreadsheetHorizontalAlignment.Center => CanvasHorizontalAlignment.Center, 
				SpreadsheetHorizontalAlignment.Right => CanvasHorizontalAlignment.Right, 
				_ => CanvasHorizontalAlignment.Left, 
			};
			double num3 = Math.Max(1.0, cellRect.Width - num * 2.0 - num2);
			string key = $"{address.Row}:{address.Column}:{(int)num3}:{flag}";
			if (!wrappedCache.TryGetValue(key, out (double, CanvasTextLayout) value))
			{
				CanvasTextLayout canvasTextLayout = attributes.WrappedLayout(ds, cell.DisplayText, style, num3, alignment, flag);
				value = (Math.Ceiling((double)canvasTextLayout.LineMetrics.Length * SpreadsheetTextAttributes.WrappedLinePitch(style.FontSize)), canvasTextLayout);
				wrappedCache[key] = value;
			}
			double item = value.Item1;
			double num4 = style.VerticalAlignment switch
			{
				SpreadsheetVerticalAlignment.Top => cellRect.MinY + 2.0, 
				SpreadsheetVerticalAlignment.Center => cellRect.MinY + Math.Max(2.0, (cellRect.Height - item) / 2.0), 
				_ => Math.Max(cellRect.MinY + 2.0, cellRect.MaxY - 3.0 - item), 
			};
			using (ds.CreateLayer(1f, R(cellRect.Inset(1.0, 1.0))))
			{
				ds.DrawTextLayout(value.Item2, (float)(cellRect.MinX + num + num2), (float)num4, color);
				return;
			}
		}
		if (!singleLineTextCache.TryGetValue(address, out (string, double, CanvasTextLayout) value2))
		{
			string text = (cell.DisplayText.Contains('\n') ? cell.DisplayText.Replace("\n", " ") : cell.DisplayText);
			CanvasTextLayout canvasTextLayout2 = attributes.SingleLineLayout(ds, text, attributes.CellFormat(style), style.IsUnderlined || flag, style.IsStruckThrough);
			value2 = (text, Math.Ceiling(canvasTextLayout2.LayoutBoundsIncludingTrailingWhitespace.Width), canvasTextLayout2);
			singleLineTextCache[address] = value2;
		}
		SpreadsheetRect spreadsheetRect = cellRect;
		if (allowsSpill && spreadsheetHorizontalAlignment == SpreadsheetHorizontalAlignment.Left && value2.Item2 + num * 2.0 + num2 > cellRect.Width)
		{
			double num5 = value2.Item2 + num * 2.0 + num2 - cellRect.Width;
			int i = address.Column + 1;
			double maxX;
			SpreadsheetCell value3;
			SpreadsheetRect spreadsheetRect2;
			Color color2;
			Color color3;
			for (maxX = cellRect.MaxX; num5 > 0.0 && i < columnLimit && (rowCells == null || !rowCells.TryGetValue(i, out value3) || value3.DisplayText.Length <= 0) && !MergedRange(address.Row, i).HasValue; color3 = color2, ds.FillRectangle(R(spreadsheetRect2.MinX - 1.0, spreadsheetRect2.MinY, spreadsheetRect2.Width, spreadsheetRect2.Height - (double)(Sheet.ShowsGridLines ? 1 : 0)), color3), maxX = spreadsheetRect2.MaxX, num5 -= spreadsheetRect2.Width, i++)
			{
				spreadsheetRect2 = Layout.CellRect(address.Row, i).Offset(ox, oy);
				if (rowCells != null && rowCells.TryGetValue(i, out SpreadsheetCell value4))
				{
					textColor = Sheet.Style(value4.StyleIndex).FillColor;
					if (textColor.HasValue)
					{
						SpreadsheetColor valueOrDefault2 = textColor.GetValueOrDefault();
						color2 = SpreadsheetTextAttributes.ColorOf(valueOrDefault2);
						continue;
					}
				}
				color2 = SpreadsheetGridPalette.Paper;
			}
			spreadsheetRect = spreadsheetRect with
			{
				Width = maxX - cellRect.MinX
			};
		}
		double num6 = SpreadsheetTextAttributes.LineHeight(style.FontSize);
		double value5 = style.VerticalAlignment switch
		{
			SpreadsheetVerticalAlignment.Top => cellRect.MinY + 3.0, 
			SpreadsheetVerticalAlignment.Center => cellRect.MinY + (cellRect.Height - num6) / 2.0, 
			_ => cellRect.MaxY - 4.0 - num6, 
		};
		double num7 = spreadsheetHorizontalAlignment switch
		{
			SpreadsheetHorizontalAlignment.Right => cellRect.MaxX - num - value2.Item2 - num2, 
			SpreadsheetHorizontalAlignment.Center => cellRect.MidX - value2.Item2 / 2.0, 
			_ => cellRect.MinX + num + num2, 
		};
		double num8 = Math.Round(value5, MidpointRounding.AwayFromZero) + SpreadsheetTextAttributes.Ascender(style.FontSize) - attributes.Baseline(attributes.CellFormat(style));
		using (ds.CreateLayer(1f, R(spreadsheetRect.Inset(1.0, 0.0))))
		{
			ds.DrawTextLayout(value2.Item3, (float)num7, (float)num8, color);
		}
	}

	private static void DrawBorders(CanvasDrawingSession ds, SpreadsheetCellStyle style, SpreadsheetRect rect)
	{
		Stroke(ds, style.TopBorder, rect.MinX - 0.5, rect.MinY - 0.5, rect.MaxX - 0.5, rect.MinY - 0.5);
		Stroke(ds, style.BottomBorder, rect.MinX - 0.5, rect.MaxY - 0.5, rect.MaxX - 0.5, rect.MaxY - 0.5);
		Stroke(ds, style.LeftBorder, rect.MinX - 0.5, rect.MinY - 0.5, rect.MinX - 0.5, rect.MaxY - 0.5);
		Stroke(ds, style.RightBorder, rect.MaxX - 0.5, rect.MinY - 0.5, rect.MaxX - 0.5, rect.MaxY - 0.5);
		static void Stroke(CanvasDrawingSession ds, SpreadsheetBorderEdge? edge, double x1, double y1, double x2, double y2)
		{
			if (edge.HasValue)
			{
				SpreadsheetBorderEdge valueOrDefault = edge.GetValueOrDefault();
				Color color = SpreadsheetTextAttributes.ColorOf(valueOrDefault.Color);
				if (valueOrDefault.IsDashed)
				{
					using (CanvasStrokeStyle strokeStyle = new CanvasStrokeStyle
					{
						CustomDashStyle = new float[2]
						{
							(float)(3.0 / valueOrDefault.Width),
							(float)(2.0 / valueOrDefault.Width)
						},
						DashCap = CanvasCapStyle.Flat
					})
					{
						ds.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, color, (float)valueOrDefault.Width, strokeStyle);
						return;
					}
				}
				ds.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, color, (float)valueOrDefault.Width);
			}
		}
	}

	private void DrawSelection(CanvasDrawingSession ds, SpreadsheetSelection selection, SpreadsheetPaneGeometry geometry)
	{
		double offsetX = geometry.OffsetX;
		double offsetY = geometry.OffsetY;
		SpreadsheetRect r = Layout.Rect(selection.Range).Offset(offsetX, offsetY);
		SpreadsheetCellRange spreadsheetCellRange = MergedRange(selection.Anchor.Row, selection.Anchor.Column) ?? new SpreadsheetCellRange(selection.Anchor);
		SpreadsheetRect r2 = Layout.Rect(spreadsheetCellRange).Offset(offsetX, offsetY);
		if (!selection.Range.IsSingleCell && selection.Range != spreadsheetCellRange)
		{
			using (CanvasGeometry canvasGeometry = CanvasGeometry.CreateRectangle(ds, R(r)))
			{
				using CanvasGeometry otherGeometry = CanvasGeometry.CreateRectangle(ds, R(r2));
				using CanvasGeometry geometry2 = canvasGeometry.CombineWith(otherGeometry, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
				ds.FillGeometry(geometry2, SpreadsheetGridPalette.SelectionFill);
			}
			SpreadsheetRect r3 = r2.Inset(0.5, 0.5).Offset(-0.5, -0.5);
			ds.DrawRectangle(R(r3), SpreadsheetGridPalette.SelectionBorder, 1f);
		}
		ds.DrawRectangle(R(r.Offset(-0.5, -0.5)), SpreadsheetGridPalette.SelectionBorder, 2f);
		SpreadsheetRect r4 = new SpreadsheetRect(r.MaxX - 4.0, r.MaxY - 4.0, 6.0, 6.0);
		ds.FillRectangle(R(r4.Inset(-1.0, -1.0)), Colors.White);
		ds.FillRectangle(R(r4), SpreadsheetGridPalette.SelectionBorder);
	}

	private static CanvasTextFormat HeaderFont(bool selected)
	{
		return SpreadsheetTextAttributes.Shared.Format(10.5, selected ? FontWeights.SemiBold : FontWeights.Medium, italic: false);
	}

	private void DrawLabel(CanvasDrawingSession ds, string label, SpreadsheetRect rect, bool selected, Color color, bool onlyIfFits)
	{
		CanvasTextFormat canvasTextFormat = HeaderFont(selected);
		using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(ds, label, canvasTextFormat, 1000f, 100f);
		double width = canvasTextLayout.LayoutBoundsIncludingTrailingWhitespace.Width;
		double num = SpreadsheetTextAttributes.LabelHeight(10.5);
		if (!onlyIfFits || !(rect.Height < num - 2.0))
		{
			double num2 = rect.MidY - num / 2.0 + SpreadsheetTextAttributes.Ascender(10.5) - attributes.Baseline(canvasTextFormat);
			ds.DrawTextLayout(canvasTextLayout, (float)(rect.MidX - width / 2.0), (float)num2, color);
		}
	}

	private void DrawColumnHeaders(CanvasDrawingSession ds, SpreadsheetPaneGeometry geometry, SpreadsheetRect strip, SpreadsheetRect dirtyRect)
	{
		ds.FillRectangle(R(dirtyRect), SpreadsheetGridPalette.HeaderFill);
		double offsetX = geometry.OffsetX;
		(int Start, int End) tuple = VisibleRange(geometry.ColumnStart, geometry.ColumnEnd, dirtyRect.MinX - offsetX, dirtyRect.MaxX - offsetX, Layout.Column);
		int item = tuple.Start;
		int item2 = tuple.End;
		SpreadsheetCellRange? spreadsheetCellRange = Selection?.Range;
		int num;
		if (spreadsheetCellRange.HasValue)
		{
			SpreadsheetCellRange valueOrDefault = spreadsheetCellRange.GetValueOrDefault();
			if (valueOrDefault.FirstRow == 0)
			{
				num = ((valueOrDefault.LastRow >= Layout.RowCount - 1) ? 1 : 0);
				goto IL_00c7;
			}
		}
		num = 0;
		goto IL_00c7;
		IL_00c7:
		bool flag = (byte)num != 0;
		for (int i = item; i < item2; i++)
		{
			if (Layout.ColumnWidths[i] <= 0.0)
			{
				continue;
			}
			SpreadsheetRect spreadsheetRect = new SpreadsheetRect(Layout.ColumnOffsets[i] + offsetX, strip.MinY, Layout.ColumnWidths[i], strip.Height);
			int num2;
			if (spreadsheetCellRange.HasValue)
			{
				SpreadsheetCellRange valueOrDefault2 = spreadsheetCellRange.GetValueOrDefault();
				if (i >= valueOrDefault2.FirstColumn)
				{
					num2 = ((i <= valueOrDefault2.LastColumn) ? 1 : 0);
					goto IL_015a;
				}
			}
			num2 = 0;
			goto IL_015a;
			IL_015a:
			bool flag2 = (byte)num2 != 0;
			if (flag2)
			{
				ds.FillRectangle(R(spreadsheetRect), flag ? SpreadsheetGridPalette.HeaderFullySelectedFill : SpreadsheetGridPalette.HeaderSelectedFill);
			}
			DrawLabel(ds, SpreadsheetCellAddress.ColumnLetters(i), spreadsheetRect, flag2, (!flag2) ? SpreadsheetGridPalette.HeaderText : (flag ? Colors.White : SpreadsheetGridPalette.HeaderSelectedText), onlyIfFits: false);
			ds.FillRectangle(R(spreadsheetRect.MaxX - 1.0, spreadsheetRect.MinY, 1.0, spreadsheetRect.Height), SpreadsheetGridPalette.HeaderLine);
		}
		ds.FillRectangle(R(dirtyRect.MinX, strip.MaxY - 1.0, dirtyRect.Width, 1.0), SpreadsheetGridPalette.HeaderLine);
		if (!spreadsheetCellRange.HasValue)
		{
			return;
		}
		SpreadsheetCellRange valueOrDefault3 = spreadsheetCellRange.GetValueOrDefault();
		if (item2 > item)
		{
			int num3 = Math.Max(valueOrDefault3.FirstColumn, item);
			int num4 = Math.Min(valueOrDefault3.LastColumn, item2 - 1);
			if (num3 <= num4)
			{
				ds.FillRectangle(R(Layout.ColumnOffsets[num3] + offsetX, strip.MaxY - 2.0, Layout.ColumnOffsets[num4 + 1] - Layout.ColumnOffsets[num3], 2.0), SpreadsheetGridPalette.SelectionBorder);
			}
		}
	}

	private void DrawRowHeaders(CanvasDrawingSession ds, SpreadsheetPaneGeometry geometry, SpreadsheetRect strip, SpreadsheetRect dirtyRect)
	{
		ds.FillRectangle(R(dirtyRect), SpreadsheetGridPalette.HeaderFill);
		double offsetY = geometry.OffsetY;
		(int Start, int End) tuple = VisibleRange(geometry.RowStart, geometry.RowEnd, dirtyRect.MinY - offsetY, dirtyRect.MaxY - offsetY, Layout.Row);
		int item = tuple.Start;
		int item2 = tuple.End;
		SpreadsheetCellRange? spreadsheetCellRange = Selection?.Range;
		int num;
		if (spreadsheetCellRange.HasValue)
		{
			SpreadsheetCellRange valueOrDefault = spreadsheetCellRange.GetValueOrDefault();
			if (valueOrDefault.FirstColumn == 0)
			{
				num = ((valueOrDefault.LastColumn >= Layout.ColumnCount - 1) ? 1 : 0);
				goto IL_00c7;
			}
		}
		num = 0;
		goto IL_00c7;
		IL_00c7:
		bool flag = (byte)num != 0;
		for (int i = item; i < item2; i++)
		{
			if (Layout.RowHeights[i] <= 0.0)
			{
				continue;
			}
			SpreadsheetRect spreadsheetRect = new SpreadsheetRect(strip.MinX, Layout.RowOffsets[i] + offsetY, strip.Width, Layout.RowHeights[i]);
			int num2;
			if (spreadsheetCellRange.HasValue)
			{
				SpreadsheetCellRange valueOrDefault2 = spreadsheetCellRange.GetValueOrDefault();
				if (i >= valueOrDefault2.FirstRow)
				{
					num2 = ((i <= valueOrDefault2.LastRow) ? 1 : 0);
					goto IL_015a;
				}
			}
			num2 = 0;
			goto IL_015a;
			IL_015a:
			bool flag2 = (byte)num2 != 0;
			if (flag2)
			{
				ds.FillRectangle(R(spreadsheetRect), flag ? SpreadsheetGridPalette.HeaderFullySelectedFill : SpreadsheetGridPalette.HeaderSelectedFill);
			}
			DrawLabel(ds, (i + 1).ToString(CultureInfo.InvariantCulture), spreadsheetRect, flag2, (!flag2) ? SpreadsheetGridPalette.HeaderText : (flag ? Colors.White : SpreadsheetGridPalette.HeaderSelectedText), onlyIfFits: true);
			ds.FillRectangle(R(spreadsheetRect.MinX, spreadsheetRect.MaxY - 1.0, spreadsheetRect.Width, 1.0), SpreadsheetGridPalette.HeaderLine);
		}
		ds.FillRectangle(R(strip.MaxX - 1.0, dirtyRect.MinY, 1.0, dirtyRect.Height), SpreadsheetGridPalette.HeaderLine);
		if (!spreadsheetCellRange.HasValue)
		{
			return;
		}
		SpreadsheetCellRange valueOrDefault3 = spreadsheetCellRange.GetValueOrDefault();
		if (item2 > item)
		{
			int num3 = Math.Max(valueOrDefault3.FirstRow, item);
			int num4 = Math.Min(valueOrDefault3.LastRow, item2 - 1);
			if (num3 <= num4)
			{
				ds.FillRectangle(R(strip.MaxX - 2.0, Layout.RowOffsets[num3] + offsetY, 2.0, Layout.RowOffsets[num4 + 1] - Layout.RowOffsets[num3]), SpreadsheetGridPalette.SelectionBorder);
			}
		}
	}

	public void Dispose()
	{
		InvalidateTextMeasurements(null);
	}
}
