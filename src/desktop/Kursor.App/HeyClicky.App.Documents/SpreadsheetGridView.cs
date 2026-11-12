using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using HeyClicky.App.UI;
using HeyClicky.Core.Documents.Spreadsheet;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace HeyClicky.App.Documents;

public sealed class SpreadsheetGridView : UserControl
{
	private abstract record DragMode;

	private sealed record CellsDrag : DragMode;

	private sealed record ColumnsDrag(int AnchorColumn) : DragMode();

	private sealed record RowsDrag(int AnchorRow) : DragMode();

	private sealed record ResizeColumnDrag(int Column, double StartX, double StartWidth) : DragMode();

	private sealed record ResizeRowDrag(int Row, double StartY, double StartHeight) : DragMode();

	private abstract record Hit;

	private sealed record CornerHit : Hit;

	private sealed record ColumnHeaderHit(int Column, double ContentX) : Hit();

	private sealed record RowHeaderHit(int Row, double ContentY) : Hit();

	private sealed record CellHit(SpreadsheetCellAddress Address) : Hit();

	private SpreadsheetGridLayout? layout;

	private SpreadsheetGridRenderer? renderer;

	private readonly Grid root = new Grid
	{
		Background = Ui.Brush(Colors.White)
	};

	private readonly CanvasControl canvas = new CanvasControl
	{
		IsHitTestVisible = false,
		ClearColor = Colors.White
	};

	private readonly ScrollViewer scrollView = new ScrollViewer
	{
		HorizontalScrollMode = ScrollMode.Enabled,
		VerticalScrollMode = ScrollMode.Enabled,
		HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
		VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
		ZoomMode = ZoomMode.Disabled,
		IsTabStop = false,
		HorizontalAlignment = HorizontalAlignment.Stretch,
		VerticalAlignment = VerticalAlignment.Stretch
	};

	private readonly Border bodySpacer = new Border
	{
		Background = Ui.Transparent,
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top
	};

	private double originX;

	private double originY;

	private SpreadsheetCellAddress anchor = new SpreadsheetCellAddress(0, 0);

	private SpreadsheetCellAddress extent = new SpreadsheetCellAddress(0, 0);

	private SpreadsheetSelectionSummary? lastSummary;

	private DragMode? dragMode;

	private DateTime lastPressAt;

	private Point lastPressPoint;

	private int clickCount;

	private static readonly uint DoubleClickMilliseconds = GetDoubleClickTime();

	public SpreadsheetSheet? Sheet { get; private set; }

	private double FixedTop
	{
		get
		{
			if (layout != null)
			{
				return layout.ColumnHeaderHeight + layout.FrozenHeight;
			}
			return 0.0;
		}
	}

	private double FixedLeft
	{
		get
		{
			if (layout != null)
			{
				return layout.RowHeaderWidth + layout.FrozenWidth;
			}
			return 0.0;
		}
	}

	private double ViewportWidth => Math.Max(0.0, base.ActualWidth - FixedLeft);

	private double ViewportHeight => Math.Max(0.0, base.ActualHeight - FixedTop);

	public event Action<SpreadsheetSelectionSummary>? SelectionChanged;

	public event Action<string>? OpenHyperlink;

	public SpreadsheetGridView()
	{
		base.IsTabStop = true;
		base.UseSystemFocusVisuals = false;
		scrollView.Content = bodySpacer;
		root.Children.Add(canvas);
		root.Children.Add(scrollView);
		base.Content = root;
		canvas.Draw += delegate(CanvasControl _, CanvasDrawEventArgs e)
		{
			DrawPanes(e.DrawingSession);
		};
		scrollView.ViewChanging += delegate(object? _, ScrollViewerViewChangingEventArgs e)
		{
			originX = e.NextView.HorizontalOffset;
			originY = e.NextView.VerticalOffset;
			canvas.Invalidate();
		};
		scrollView.ViewChanged += delegate
		{
			originX = scrollView.HorizontalOffset;
			originY = scrollView.VerticalOffset;
			canvas.Invalidate();
		};
		base.SizeChanged += delegate
		{
			ApplyGeometry();
		};
		root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), handledEventsToo: true);
		root.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), handledEventsToo: true);
		root.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnPointerReleased), handledEventsToo: true);
		root.AddHandler(UIElement.PointerCaptureLostEvent, (PointerEventHandler)delegate
		{
			dragMode = null;
		}, handledEventsToo: true);
		root.PointerExited += delegate
		{
			if ((object)dragMode == null)
			{
				base.ProtectedCursor = null;
			}
		};
		root.PointerWheelChanged += OnWheelOutsideBody;
		base.KeyDown += OnKeyDown;
		base.Unloaded += delegate
		{
			renderer?.Dispose();
		};
	}

	public void Show(SpreadsheetSheet newSheet)
	{
		if (newSheet != Sheet)
		{
			Sheet = newSheet;
			renderer?.Dispose();
			layout = new SpreadsheetGridLayout(newSheet, SpreadsheetTextAttributes.Shared);
			renderer = new SpreadsheetGridRenderer(layout);
			anchor = new SpreadsheetCellAddress(0, 0);
			extent = anchor;
			ApplyGeometry();
			scrollView.ChangeView(0.0, 0.0, null, disableAnimation: true);
			originX = (originY = 0.0);
			UpdateSelection(scrollToActiveCell: false);
		}
	}

	public void Select(SpreadsheetCellRange range)
	{
		anchor = new SpreadsheetCellAddress(range.FirstRow, range.FirstColumn);
		extent = new SpreadsheetCellAddress(range.LastRow, range.LastColumn);
		UpdateSelection(scrollToActiveCell: false);
	}

	private void ApplyGeometry()
	{
		if (layout != null)
		{
			scrollView.Margin = new Thickness(FixedLeft, FixedTop, 0.0, 0.0);
			bodySpacer.Width = Math.Max(layout.TotalWidth - layout.FrozenWidth, 1.0);
			bodySpacer.Height = Math.Max(layout.TotalHeight - layout.FrozenHeight, 1.0);
			canvas.Invalidate();
		}
	}

	private void DrawPanes(CanvasDrawingSession ds)
	{
		Matrix3x2 oldTransform;
		if (layout != null && renderer != null)
		{
			double columnHeaderHeight = layout.ColumnHeaderHeight;
			double rowHeaderWidth = layout.RowHeaderWidth;
			double fixedTop = FixedTop;
			double fixedLeft = FixedLeft;
			double actualWidth = base.ActualWidth;
			double actualHeight = base.ActualHeight;
			int frozenRowCount = layout.FrozenRowCount;
			int frozenColumnCount = layout.FrozenColumnCount;
			oldTransform = ds.Transform;
			double num = Math.Max(0.0, actualWidth - fixedLeft);
			double num2 = Math.Max(0.0, actualHeight - fixedTop);
			SpreadsheetRect spreadsheetRect = new SpreadsheetRect(originX, originY, num, num2);
			Pane(fixedLeft, fixedTop, num, num2, new SpreadsheetPaneGeometry(frozenRowCount, layout.RowCount, frozenColumnCount, layout.ColumnCount, 0.0 - layout.FrozenWidth, 0.0 - layout.FrozenHeight, 0.0, 0.0), spreadsheetRect, spreadsheetRect, dividerBelow: false, dividerRight: false);
			SpreadsheetRect spreadsheetRect2 = new SpreadsheetRect(0.0, 0.0, num, fixedTop);
			Pane(fixedLeft, 0.0, num, fixedTop, new SpreadsheetPaneGeometry(0, frozenRowCount, frozenColumnCount, layout.ColumnCount, 0.0 - layout.FrozenWidth - originX, columnHeaderHeight, columnHeaderHeight, 0.0), spreadsheetRect2, spreadsheetRect2, frozenRowCount > 0, dividerRight: false);
			SpreadsheetRect spreadsheetRect3 = new SpreadsheetRect(0.0, 0.0, fixedLeft, num2);
			Pane(0.0, fixedTop, fixedLeft, num2, new SpreadsheetPaneGeometry(frozenRowCount, layout.RowCount, 0, frozenColumnCount, rowHeaderWidth, 0.0 - layout.FrozenHeight - originY, 0.0, rowHeaderWidth), spreadsheetRect3, spreadsheetRect3, dividerBelow: false, frozenColumnCount > 0);
			SpreadsheetRect spreadsheetRect4 = new SpreadsheetRect(0.0, 0.0, fixedLeft, fixedTop);
			Pane(0.0, 0.0, fixedLeft, fixedTop, new SpreadsheetPaneGeometry(0, frozenRowCount, 0, frozenColumnCount, rowHeaderWidth, columnHeaderHeight, columnHeaderHeight, rowHeaderWidth), spreadsheetRect4, spreadsheetRect4, frozenRowCount > 0, frozenColumnCount > 0);
		}
		void Pane(double x, double y, double w, double h, SpreadsheetPaneGeometry geometry, SpreadsheetRect dirty, SpreadsheetRect bounds, bool dividerBelow, bool dividerRight)
		{
			if (w <= 0.0 || h <= 0.0)
			{
				return;
			}
			using (ds.CreateLayer(1f, new Rect(x, y, w, h)))
			{
				ds.Transform = Matrix3x2.CreateTranslation((float)(x - bounds.X), (float)(y - bounds.Y)) * oldTransform;
				renderer.Draw(ds, geometry, dirty, bounds);
				ds.Transform = Matrix3x2.CreateTranslation((float)x, (float)y) * oldTransform;
				if (dividerBelow)
				{
					ds.FillRectangle(new Rect(0.0, h - 2.0, w, 2.0), SpreadsheetGridPalette.FrozenDivider);
				}
				if (dividerRight)
				{
					ds.FillRectangle(new Rect(w - 2.0, 0.0, 2.0, h), SpreadsheetGridPalette.FrozenDivider);
				}
				ds.Transform = oldTransform;
			}
		}
	}

	private Point ContentPoint(Point point)
	{
		if (layout == null)
		{
			return default(Point);
		}
		double num = point.X - layout.RowHeaderWidth;
		double num2 = point.Y - layout.ColumnHeaderHeight;
		double x = ((num < layout.FrozenWidth) ? num : (num + originX));
		double y = ((num2 < layout.FrozenHeight) ? num2 : (num2 + originY));
		return new Point(x, y);
	}

	private Hit HitAt(Point point)
	{
		if (layout == null)
		{
			return new CornerHit();
		}
		Point point2 = ContentPoint(point);
		bool flag = point.Y < layout.ColumnHeaderHeight;
		bool flag2 = point.X < layout.RowHeaderWidth;
		if (flag && flag2)
		{
			return new CornerHit();
		}
		if (flag)
		{
			return new ColumnHeaderHit(layout.Column(point2.X), point2.X);
		}
		if (flag2)
		{
			return new RowHeaderHit(layout.Row(point2.Y), point2.Y);
		}
		return new CellHit(new SpreadsheetCellAddress(layout.Row(point2.Y), layout.Column(point2.X)));
	}

	private int? ColumnResizeTarget(Point point)
	{
		if (layout != null && HitAt(point) is ColumnHeaderHit columnHeaderHit)
		{
			columnHeaderHit.Deconstruct(out var Column, out var ContentX);
			if (Math.Abs(ContentX - layout.ColumnOffsets[Column + 1]) <= 4.0)
			{
				return Column;
			}
			if (Column > 0 && Math.Abs(ContentX - layout.ColumnOffsets[Column]) <= 4.0)
			{
				int num = Column - 1;
				while (num > 0 && layout.ColumnWidths[num] == 0.0)
				{
					num--;
				}
				return num;
			}
			return null;
		}
		return null;
	}

	private int? RowResizeTarget(Point point)
	{
		if (layout != null && HitAt(point) is RowHeaderHit rowHeaderHit)
		{
			rowHeaderHit.Deconstruct(out var Row, out var ContentY);
			if (Math.Abs(ContentY - layout.RowOffsets[Row + 1]) <= 3.0)
			{
				return Row;
			}
			if (Row > 0 && Math.Abs(ContentY - layout.RowOffsets[Row]) <= 3.0)
			{
				return Row - 1;
			}
			return null;
		}
		return null;
	}

	[DllImport("user32.dll")]
	private static extern uint GetDoubleClickTime();

	private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (layout == null || IsInScrollBar(e.OriginalSource as DependencyObject))
		{
			return;
		}
		PointerPoint currentPoint = e.GetCurrentPoint(this);
		if (!currentPoint.Properties.IsLeftButtonPressed)
		{
			return;
		}
		Focus(FocusState.Pointer);
		Point position = currentPoint.Position;
		DateTime utcNow = DateTime.UtcNow;
		clickCount = ((!((utcNow - lastPressAt).TotalMilliseconds <= (double)DoubleClickMilliseconds) || !(Math.Abs(position.X - lastPressPoint.X) <= 4.0) || !(Math.Abs(position.Y - lastPressPoint.Y) <= 4.0)) ? 1 : (clickCount + 1));
		lastPressAt = utcNow;
		lastPressPoint = position;
		bool flag = (e.KeyModifiers & VirtualKeyModifiers.Shift) != 0;
		root.CapturePointer(e.Pointer);
		int? num = ColumnResizeTarget(position);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (clickCount >= 2)
			{
				layout.SetColumnWidth(layout.FittingWidth(valueOrDefault), valueOrDefault);
				renderer?.InvalidateTextMeasurements(valueOrDefault);
				ApplyGeometry();
				dragMode = null;
			}
			else
			{
				dragMode = new ResizeColumnDrag(valueOrDefault, position.X, layout.ColumnWidths[valueOrDefault]);
			}
			return;
		}
		num = RowResizeTarget(position);
		if (num.HasValue)
		{
			int valueOrDefault2 = num.GetValueOrDefault();
			dragMode = new ResizeRowDrag(valueOrDefault2, position.Y, layout.RowHeights[valueOrDefault2]);
			return;
		}
		Hit hit = HitAt(position);
		if (!(hit is CornerHit))
		{
			if (hit is ColumnHeaderHit columnHeaderHit)
			{
				columnHeaderHit.Deconstruct(out var Column, out var _);
				int num2 = (flag ? anchor.Column : Column);
				anchor = new SpreadsheetCellAddress(0, num2);
				extent = new SpreadsheetCellAddress(layout.RowCount - 1, Column);
				dragMode = new ColumnsDrag(num2);
				UpdateSelection(scrollToActiveCell: false);
			}
			else if (hit is RowHeaderHit rowHeaderHit)
			{
				rowHeaderHit.Deconstruct(out var Row, out var _);
				int num3 = (flag ? anchor.Row : Row);
				anchor = new SpreadsheetCellAddress(num3, 0);
				extent = new SpreadsheetCellAddress(Row, layout.ColumnCount - 1);
				dragMode = new RowsDrag(num3);
				UpdateSelection(scrollToActiveCell: false);
			}
			else if (hit is CellHit cellHit)
			{
				cellHit.Deconstruct(out var Address);
				if (clickCount >= 2)
				{
					SpreadsheetSheet? sheet = Sheet;
					if (sheet != null && sheet.Hyperlinks.TryGetValue(TopLeft(Address), out string value))
					{
						OpenLink(value);
						return;
					}
				}
				if (flag)
				{
					extent = Address;
				}
				else
				{
					anchor = TopLeft(Address);
					extent = Address;
				}
				dragMode = new CellsDrag();
				UpdateSelection(scrollToActiveCell: false);
			}
		}
		else
		{
			SelectAll();
			dragMode = null;
		}
		e.Handled = true;
	}

	private bool IsInScrollBar(DependencyObject? element)
	{
		DependencyObject dependencyObject = element;
		while ((object)dependencyObject != null && (object)dependencyObject != scrollView)
		{
			if (dependencyObject is ScrollBar)
			{
				return true;
			}
			dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
		}
		return false;
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (layout == null)
		{
			return;
		}
		Point position = e.GetCurrentPoint(this).Position;
		if ((object)this.dragMode == null)
		{
			base.ProtectedCursor = (ColumnResizeTarget(position).HasValue ? InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast) : (RowResizeTarget(position).HasValue ? InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth) : null));
			return;
		}
		DragMode dragMode = this.dragMode;
		if (dragMode is ResizeColumnDrag resizeColumnDrag)
		{
			resizeColumnDrag.Deconstruct(out var Column, out var StartX, out var StartWidth);
			layout.SetColumnWidth(StartWidth + position.X - StartX, Column);
			renderer?.InvalidateTextMeasurements(Column);
			ApplyGeometry();
			return;
		}
		if (dragMode is ResizeRowDrag resizeRowDrag)
		{
			resizeRowDrag.Deconstruct(out var Row, out var StartY, out var StartHeight);
			layout.SetRowHeight(StartHeight + position.Y - StartY, Row);
			renderer?.InvalidateTextMeasurements(null);
			ApplyGeometry();
			return;
		}
		if (!(dragMode is CellsDrag))
		{
			if (dragMode is ColumnsDrag columnsDrag)
			{
				columnsDrag.Deconstruct(out var AnchorColumn);
				Point point = ContentPoint(position);
				anchor = new SpreadsheetCellAddress(0, AnchorColumn);
				extent = new SpreadsheetCellAddress(layout.RowCount - 1, layout.Column(point.X));
			}
			else if (dragMode is RowsDrag rowsDrag)
			{
				rowsDrag.Deconstruct(out var AnchorRow);
				Point point2 = ContentPoint(position);
				anchor = new SpreadsheetCellAddress(AnchorRow, 0);
				extent = new SpreadsheetCellAddress(layout.Row(point2.Y), layout.ColumnCount - 1);
			}
		}
		else
		{
			Point point3 = ContentPoint(position);
			extent = new SpreadsheetCellAddress(layout.Row(point3.Y), layout.Column(point3.X));
		}
		Autoscroll(position);
		UpdateSelection(scrollToActiveCell: false);
	}

	private void Autoscroll(Point point)
	{
		double num = 0.0;
		double num2 = 0.0;
		if (point.X > base.ActualWidth)
		{
			num = point.X - base.ActualWidth;
		}
		else if (point.X < FixedLeft && originX > 0.0)
		{
			num = point.X - FixedLeft;
		}
		if (point.Y > base.ActualHeight)
		{
			num2 = point.Y - base.ActualHeight;
		}
		else if (point.Y < FixedTop && originY > 0.0)
		{
			num2 = point.Y - FixedTop;
		}
		if (num != 0.0 || num2 != 0.0)
		{
			scrollView.ChangeView(Math.Max(0.0, originX + num), Math.Max(0.0, originY + num2), null, disableAnimation: true);
		}
	}

	private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		dragMode = null;
		root.ReleasePointerCapture(e.Pointer);
	}

	private void OnWheelOutsideBody(object sender, PointerRoutedEventArgs e)
	{
		if (layout == null)
		{
			return;
		}
		PointerPoint currentPoint = e.GetCurrentPoint(this);
		if (!(currentPoint.Position.X >= FixedLeft) || !(currentPoint.Position.Y >= FixedTop))
		{
			double num = (double)currentPoint.Properties.MouseWheelDelta / 120.0 * 48.0;
			if (currentPoint.Properties.IsHorizontalMouseWheel)
			{
				scrollView.ChangeView(Math.Max(0.0, originX + num), null, null);
			}
			else if ((e.KeyModifiers & VirtualKeyModifiers.Shift) != 0)
			{
				scrollView.ChangeView(Math.Max(0.0, originX - num), null, null);
			}
			else
			{
				scrollView.ChangeView(null, Math.Max(0.0, originY - num), null);
			}
			e.Handled = true;
		}
	}

	private void OpenLink(string link)
	{
		if (!link.StartsWith('#'))
		{
			this.OpenHyperlink?.Invoke(link);
		}
	}

	private static bool IsDown(VirtualKey key)
	{
		return (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (layout == null)
		{
			return;
		}
		bool flag = IsDown(VirtualKey.Shift);
		bool flag2 = IsDown(VirtualKey.Control);
		if (flag2 && e.Key == VirtualKey.C)
		{
			Copy();
			e.Handled = true;
			return;
		}
		if (flag2 && e.Key == VirtualKey.A)
		{
			SelectAll();
			e.Handled = true;
			return;
		}
		SpreadsheetCellAddress address = (flag ? extent : anchor);
		int num = Math.Max(1, (int)(ViewportHeight / Math.Max(layout.Sheet.DefaultRowHeight, 12.0)) - 1);
		switch (e.Key)
		{
		default:
			return;
		case VirtualKey.Left:
			address = Moved(address, 0, -1, flag2);
			break;
		case VirtualKey.Right:
			address = Moved(address, 0, 1, flag2);
			break;
		case VirtualKey.Down:
			address = Moved(address, 1, 0, flag2);
			break;
		case VirtualKey.Up:
			address = Moved(address, -1, 0, flag2);
			break;
		case VirtualKey.Tab:
			anchor = Moved(anchor, 0, (!flag) ? 1 : (-1), jumpsToEdge: false);
			extent = anchor;
			UpdateSelection(scrollToActiveCell: true);
			e.Handled = true;
			return;
		case VirtualKey.Enter:
			anchor = Moved(anchor, (!flag) ? 1 : (-1), 0, jumpsToEdge: false);
			extent = anchor;
			UpdateSelection(scrollToActiveCell: true);
			e.Handled = true;
			return;
		case VirtualKey.PageUp:
			address = Moved(address, -num, 0, jumpsToEdge: false);
			break;
		case VirtualKey.PageDown:
			address = Moved(address, num, 0, jumpsToEdge: false);
			break;
		case VirtualKey.Home:
			address = new SpreadsheetCellAddress((!flag2) ? address.Row : 0, 0);
			break;
		case VirtualKey.End:
			address = new SpreadsheetCellAddress(flag2 ? Math.Max(0, layout.Sheet.LastDataRow) : address.Row, Math.Max(0, layout.Sheet.LastDataColumn));
			break;
		}
		if (flag)
		{
			extent = address;
		}
		else
		{
			anchor = TopLeft(address);
			extent = address;
		}
		UpdateSelection(scrollToActiveCell: true);
		e.Handled = true;
	}

	private SpreadsheetCellAddress Moved(SpreadsheetCellAddress address, int rowDelta, int columnDelta, bool jumpsToEdge)
	{
		if (layout == null)
		{
			return address;
		}
		if (jumpsToEdge)
		{
			int num = Math.Max(0, layout.Sheet.LastDataRow);
			int num2 = Math.Max(0, layout.Sheet.LastDataColumn);
			return new SpreadsheetCellAddress((rowDelta > 0) ? num : ((rowDelta >= 0) ? address.Row : 0), (columnDelta > 0) ? num2 : ((columnDelta >= 0) ? address.Column : 0));
		}
		int num3 = address.Row;
		int num4 = address.Column;
		SpreadsheetCellRange? spreadsheetCellRange = renderer?.MergedRange(num3, num4);
		if (spreadsheetCellRange.HasValue)
		{
			SpreadsheetCellRange valueOrDefault = spreadsheetCellRange.GetValueOrDefault();
			if (rowDelta > 0)
			{
				num3 = valueOrDefault.LastRow;
			}
			if (columnDelta > 0)
			{
				num4 = valueOrDefault.LastColumn;
			}
			if (rowDelta < 0)
			{
				num3 = valueOrDefault.FirstRow;
			}
			if (columnDelta < 0)
			{
				num4 = valueOrDefault.FirstColumn;
			}
		}
		num3 = Math.Min(Math.Max(0, num3 + rowDelta), layout.RowCount - 1);
		num4 = Math.Min(Math.Max(0, num4 + columnDelta), layout.ColumnCount - 1);
		int num5 = ((rowDelta >= 0) ? 1 : (-1));
		int num6 = ((columnDelta >= 0) ? 1 : (-1));
		for (; layout.RowHeights[num3] == 0.0 && num3 + num5 >= 0 && num3 + num5 < layout.RowCount; num3 += num5)
		{
		}
		for (; layout.ColumnWidths[num4] == 0.0 && num4 + num6 >= 0 && num4 + num6 < layout.ColumnCount; num4 += num6)
		{
		}
		return new SpreadsheetCellAddress(num3, num4);
	}

	private SpreadsheetCellAddress TopLeft(SpreadsheetCellAddress address)
	{
		SpreadsheetCellRange? spreadsheetCellRange = renderer?.MergedRange(address.Row, address.Column);
		if (spreadsheetCellRange.HasValue)
		{
			SpreadsheetCellRange valueOrDefault = spreadsheetCellRange.GetValueOrDefault();
			return new SpreadsheetCellAddress(valueOrDefault.FirstRow, valueOrDefault.FirstColumn);
		}
		return address;
	}

	public void SelectAll()
	{
		if (layout != null)
		{
			anchor = new SpreadsheetCellAddress(0, 0);
			extent = new SpreadsheetCellAddress(layout.RowCount - 1, layout.ColumnCount - 1);
			UpdateSelection(scrollToActiveCell: false);
		}
	}

	public string? SelectionText()
	{
		SpreadsheetSheet sheet = Sheet;
		if (sheet != null)
		{
			SpreadsheetCellRange? spreadsheetCellRange = renderer?.Selection?.Range;
			if (spreadsheetCellRange.HasValue)
			{
				SpreadsheetCellRange valueOrDefault = spreadsheetCellRange.GetValueOrDefault();
				int num = Math.Min(valueOrDefault.LastRow, sheet.LastDataRow);
				int num2 = Math.Min(valueOrDefault.LastColumn, sheet.LastDataColumn);
				if (num < valueOrDefault.FirstRow || num2 < valueOrDefault.FirstColumn)
				{
					return null;
				}
				if ((long)(num - valueOrDefault.FirstRow + 1) * (long)(num2 - valueOrDefault.FirstColumn + 1) > 500000)
				{
					return null;
				}
				List<string> list = new List<string>();
				for (int i = valueOrDefault.FirstRow; i <= num; i++)
				{
					List<string> list2 = new List<string>();
					for (int j = valueOrDefault.FirstColumn; j <= num2; j++)
					{
						string text = sheet.Cell(i, j)?.DisplayText ?? "";
						if (text.Contains('\t') || text.Contains('\n') || text.Contains('"'))
						{
							text = "\"" + text.Replace("\"", "\"\"") + "\"";
						}
						list2.Add(text);
					}
					list.Add(string.Join('\t', list2));
				}
				return string.Join('\n', list);
			}
		}
		return null;
	}

	public void Copy()
	{
		string text = SelectionText();
		if (text != null)
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(text);
			Clipboard.SetContent(dataPackage);
		}
	}

	private void UpdateSelection(bool scrollToActiveCell)
	{
		if (renderer == null || layout == null)
		{
			return;
		}
		SpreadsheetSheet sheet = Sheet;
		if (sheet != null)
		{
			SpreadsheetCellRange range = renderer.ExpandedForMerges(SpreadsheetCellRange.Spanning(anchor, extent));
			renderer.Selection = new SpreadsheetSelection(anchor, extent, range);
			canvas.Invalidate();
			if (scrollToActiveCell)
			{
				ScrollToVisible(extent);
			}
			SpreadsheetSelectionSummary spreadsheetSelectionSummary = MakeSummary(sheet, range);
			if (spreadsheetSelectionSummary != lastSummary)
			{
				lastSummary = spreadsheetSelectionSummary;
				this.SelectionChanged?.Invoke(spreadsheetSelectionSummary);
			}
		}
	}

	private void ScrollToVisible(SpreadsheetCellAddress address)
	{
		if (layout == null)
		{
			return;
		}
		SpreadsheetRect spreadsheetRect = layout.CellRect(address.Row, address.Column);
		double num = originX;
		double num2 = originY;
		double viewportWidth = ViewportWidth;
		double viewportHeight = ViewportHeight;
		if (address.Column >= layout.FrozenColumnCount)
		{
			double num3 = spreadsheetRect.MinX - layout.FrozenWidth;
			double num4 = num3 + spreadsheetRect.Width;
			if (num3 < num)
			{
				num = num3;
			}
			else if (num4 > num + viewportWidth)
			{
				num = Math.Min(num3, num4 - viewportWidth);
			}
		}
		if (address.Row >= layout.FrozenRowCount)
		{
			double num5 = spreadsheetRect.MinY - layout.FrozenHeight;
			double num6 = num5 + spreadsheetRect.Height;
			if (num5 < num2)
			{
				num2 = num5;
			}
			else if (num6 > num2 + viewportHeight)
			{
				num2 = Math.Min(num5, num6 - viewportHeight);
			}
		}
		if (num != originX || num2 != originY)
		{
			scrollView.ChangeView(Math.Max(0.0, num), Math.Max(0.0, num2), null, disableAnimation: true);
		}
	}

	private SpreadsheetSelectionSummary MakeSummary(SpreadsheetSheet sheet, SpreadsheetCellRange range)
	{
		SpreadsheetCell? spreadsheetCell = sheet.Cell(anchor.Row, anchor.Column);
		string reference = anchor.Reference;
		string formulaBarText = spreadsheetCell?.FormulaBarText ?? "";
		string value;
		string hyperlink = ((sheet.Hyperlinks.TryGetValue(anchor, out value) && !value.StartsWith('#')) ? value : null);
		SpreadsheetSelectionSummary spreadsheetSelectionSummary = new SpreadsheetSelectionSummary(reference, formulaBarText, hyperlink);
		if (!range.IsSingleCell)
		{
			SpreadsheetGridRenderer? spreadsheetGridRenderer = renderer;
			if (spreadsheetGridRenderer == null || !(spreadsheetGridRenderer.MergedRange(anchor.Row, anchor.Column) == range))
			{
				int num = Math.Min(range.LastRow, sheet.LastDataRow);
				int num2 = Math.Min(range.LastColumn, sheet.LastDataColumn);
				if (num < range.FirstRow || num2 < range.FirstColumn)
				{
					return spreadsheetSelectionSummary;
				}
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				double num6 = 0.0;
				for (int i = range.FirstRow; i <= num; i++)
				{
					if (!sheet.CellsByRow.TryGetValue(i, out Dictionary<int, SpreadsheetCell> value2))
					{
						continue;
					}
					foreach (var (num8, spreadsheetCell3) in value2)
					{
						if (num8 < range.FirstColumn || num8 > num2)
						{
							continue;
						}
						num5++;
						if (num5 > 400000)
						{
							break;
						}
						if (spreadsheetCell3.DisplayText.Length == 0)
						{
							continue;
						}
						num3++;
						if (spreadsheetCell3.Kind == SpreadsheetCellKind.Number)
						{
							double? numericValue = spreadsheetCell3.NumericValue;
							if (numericValue.HasValue)
							{
								double valueOrDefault = numericValue.GetValueOrDefault();
								num4++;
								num6 += valueOrDefault;
							}
						}
					}
				}
				if (num4 >= 1)
				{
					double value3 = num6 / (double)num4;
					return spreadsheetSelectionSummary with
					{
						Statistics = $"Sum: {StatisticText(num6)}   Average: {StatisticText(value3)}   Count: {num3}"
					};
				}
				if (num3 > 0)
				{
					return spreadsheetSelectionSummary with
					{
						Statistics = $"Count: {num3}"
					};
				}
				return spreadsheetSelectionSummary;
			}
		}
		return spreadsheetSelectionSummary;
	}

	private static string StatisticText(double value)
	{
		if (!double.IsFinite(value))
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}
		int num = ((Math.Abs(value) >= 1000.0) ? 2 : 4);
		return Math.Round(value, num, MidpointRounding.ToEven).ToString("#,##0." + new string('#', num), CultureInfo.CurrentCulture);
	}
}
